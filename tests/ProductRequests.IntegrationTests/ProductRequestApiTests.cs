using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProductRequests.Infrastructure.Persistence;
using ProductRequests.Infrastructure.Seeding;

namespace ProductRequests.IntegrationTests;

[Collection(MySqlDatabaseFixtureSet.Name)]
[SuppressMessage("Design", "CA1001", Justification = "xUnit IAsyncLifetime disposes owned resources.")]
public sealed class ProductRequestApiTests(MySqlFixture database) : IAsyncLifetime
{
    private ProductRequestsApiFactory _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _factory = new ProductRequestsApiFactory(database.ConnectionString);
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await using AsyncServiceScope scope = _factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ProductRequestsDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<DemoUserSeeder>().SeedAsync();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task ClientCreatesRequestAndBodyCannotSpoofIdentity()
    {
        await AuthenticateAsync("client@example.com");
        var body = new
        {
            productName = $"Laptop-{Guid.NewGuid()}",
            description = "Business laptop",
            quantity = 2,
            currency = "usd",
            clientId = Guid.NewGuid()
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/product-requests", body);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        ProductRequestPayload created = (await response.Content.ReadFromJsonAsync<ProductRequestPayload>())!;
        Assert.Equal("USD", created.Currency);
        Assert.Equal("Open", created.Status);
        Assert.NotNull(response.Headers.Location);

        Guid expectedClientId = await GetUserIdAsync("CLIENT@EXAMPLE.COM");
        await using AsyncServiceScope scope = _factory.Services.CreateAsyncScope();
        ProductRequestsDbContext context = scope.ServiceProvider.GetRequiredService<ProductRequestsDbContext>();
        Assert.Equal(expectedClientId, (await context.ProductRequests.FindAsync(created.Id))!.ClientId);
    }

    [Fact]
    public async Task ProviderCannotCreateRequest()
    {
        await AuthenticateAsync("provider1@example.com");

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/product-requests", ValidRequest());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData(0, "USD")]
    [InlineData(1, "US")]
    public async Task InvalidQuantityOrCurrencyReturnsBadRequest(int quantity, string currency)
    {
        await AuthenticateAsync("client@example.com");

        HttpResponseMessage response = await _client.PostAsJsonAsync(
            "/api/product-requests",
            new { productName = "Laptop", description = "Business", quantity, currency });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ClientOnlyListsOwnRequestsAndOtherClientIsDenied()
    {
        await AuthenticateAsync("client@example.com");
        ProductRequestPayload first = await CreateRequestAsync();
        await AuthenticateAsync("client2@example.com");
        ProductRequestPayload second = await CreateRequestAsync();

        PagedPayload mine = (await _client.GetFromJsonAsync<PagedPayload>(
            "/api/product-requests/mine?page=1&pageSize=100"))!;
        HttpResponseMessage foreign = await _client.GetAsync($"/api/product-requests/{first.Id}");

        Assert.Contains(mine.Items, item => item.Id == second.Id);
        Assert.DoesNotContain(mine.Items, item => item.Id == first.Id);
        Assert.Equal(HttpStatusCode.Forbidden, foreign.StatusCode);
    }

    [Fact]
    public async Task ProviderListsOpenRequestsWithBoundedPagination()
    {
        await AuthenticateAsync("client@example.com");
        ProductRequestPayload created = await CreateRequestAsync();
        await AuthenticateAsync("provider1@example.com");

        PagedPayload page = (await _client.GetFromJsonAsync<PagedPayload>(
            "/api/product-requests/open?page=1&pageSize=1"))!;
        HttpResponseMessage tooLarge = await _client.GetAsync(
            "/api/product-requests/open?page=1&pageSize=101");
        HttpResponseMessage details = await _client.GetAsync($"/api/product-requests/{created.Id}");

        Assert.Single(page.Items);
        Assert.All(page.Items, item => Assert.Equal("Open", item.Status));
        Assert.Equal(HttpStatusCode.BadRequest, tooLarge.StatusCode);
        Assert.Equal(HttpStatusCode.OK, details.StatusCode);
    }

    private async Task<ProductRequestPayload> CreateRequestAsync()
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/product-requests", ValidRequest());
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProductRequestPayload>())!;
    }

    private async Task AuthenticateAsync(string email)
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new { email, password = "Passw0rd!" });
        response.EnsureSuccessStatusCode();
        using JsonDocument payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        string token = payload.RootElement.GetProperty("accessToken").GetString()!;
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private async Task<Guid> GetUserIdAsync(string normalizedEmail)
    {
        await using AsyncServiceScope scope = _factory.Services.CreateAsyncScope();
        ProductRequestsDbContext context = scope.ServiceProvider.GetRequiredService<ProductRequestsDbContext>();
        return await context.Users.Where(user => user.NormalizedEmail == normalizedEmail)
            .Select(user => user.Id)
            .SingleAsync();
    }

    private static object ValidRequest() => new
    {
        productName = $"Laptop-{Guid.NewGuid()}",
        description = "Business laptop",
        quantity = 1,
        currency = "USD"
    };

    private sealed record ProductRequestPayload(Guid Id, string Currency, string Status);
    private sealed record PagedPayload(ProductRequestPayload[] Items, int Page, int PageSize, int TotalItems, int TotalPages);
}

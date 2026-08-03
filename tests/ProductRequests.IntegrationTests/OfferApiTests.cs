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
using DomainProductRequest = ProductRequests.Domain.ProductRequests.ProductRequest;

namespace ProductRequests.IntegrationTests;

[Collection(MySqlDatabaseFixtureSet.Name)]
[SuppressMessage("Design", "CA1001", Justification = "xUnit IAsyncLifetime disposes owned resources.")]
public sealed class OfferApiTests(MySqlFixture database) : IAsyncLifetime
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
    public async Task ProviderCreatesOfferWithPersistedHistoryAndClientCannotCreate()
    {
        Guid requestId = await CreateRequestAsync("client@example.com");
        await AuthenticateAsync("provider1@example.com");

        HttpResponseMessage response = await _client.PostAsJsonAsync(
            $"/api/product-requests/{requestId}/offers",
            ValidOffer(12500));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        OfferPayload offer = (await response.Content.ReadFromJsonAsync<OfferPayload>())!;
        Assert.Equal("PendingClientDecision", offer.Status);
        Assert.Equal("USD", offer.Currency);
        await using (AsyncServiceScope scope = _factory.Services.CreateAsyncScope())
        {
            ProductRequestsDbContext context = scope.ServiceProvider.GetRequiredService<ProductRequestsDbContext>();
            Assert.Equal(1, await context.OfferHistories.CountAsync(item => item.OfferId == offer.Id));
        }

        await AuthenticateAsync("client@example.com");
        HttpResponseMessage clientResponse = await _client.PostAsJsonAsync(
            $"/api/product-requests/{requestId}/offers",
            ValidOffer(12000));
        Assert.Equal(HttpStatusCode.Forbidden, clientResponse.StatusCode);
    }

    [Theory]
    [InlineData(0, "USD", 5, HttpStatusCode.BadRequest)]
    [InlineData(100, "USD", 0, HttpStatusCode.BadRequest)]
    [InlineData(100, "EUR", 5, HttpStatusCode.Conflict)]
    public async Task InvalidOfferIsRejected(
        decimal amount,
        string currency,
        int deliveryDays,
        HttpStatusCode expected)
    {
        Guid requestId = await CreateRequestAsync("client@example.com");
        await AuthenticateAsync("provider1@example.com");

        HttpResponseMessage response = await _client.PostAsJsonAsync(
            $"/api/product-requests/{requestId}/offers",
            new { amount, currency, deliveryDays, notes = "Test" });

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task DuplicateProviderOfferReturnsConflict()
    {
        Guid requestId = await CreateRequestAsync("client@example.com");
        await AuthenticateAsync("provider1@example.com");
        (await _client.PostAsJsonAsync($"/api/product-requests/{requestId}/offers", ValidOffer(100)))
            .EnsureSuccessStatusCode();

        HttpResponseMessage duplicate = await _client.PostAsJsonAsync(
            $"/api/product-requests/{requestId}/offers",
            ValidOffer(90));

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        using JsonDocument problem = JsonDocument.Parse(await duplicate.Content.ReadAsStringAsync());
        Assert.Equal("DUPLICATE_PROVIDER_OFFER", problem.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task OfferQueriesHideCompetitors()
    {
        Guid requestId = await CreateRequestAsync("client@example.com");
        OfferPayload provider1Offer = await CreateOfferAsync(requestId, "provider1@example.com", 100);
        OfferPayload provider2Offer = await CreateOfferAsync(requestId, "provider2@example.com", 90);

        await AuthenticateAsync("client@example.com");
        OfferPayload[] all = (await _client.GetFromJsonAsync<OfferPayload[]>(
            $"/api/product-requests/{requestId}/offers"))!;
        Assert.Contains(all, item => item.Id == provider1Offer.Id);
        Assert.Contains(all, item => item.Id == provider2Offer.Id);

        await AuthenticateAsync("client2@example.com");
        Assert.Equal(HttpStatusCode.Forbidden,
            (await _client.GetAsync($"/api/product-requests/{requestId}/offers")).StatusCode);

        await AuthenticateAsync("provider1@example.com");
        Assert.Equal(HttpStatusCode.OK,
            (await _client.GetAsync($"/api/offers/{provider1Offer.Id}")).StatusCode);
        PagedOfferPayload mine = (await _client.GetFromJsonAsync<PagedOfferPayload>(
            "/api/offers/mine?page=1&pageSize=100"))!;
        Assert.Contains(mine.Items, item => item.Id == provider1Offer.Id);

        await AuthenticateAsync("provider2@example.com");
        Assert.Equal(HttpStatusCode.Forbidden,
            (await _client.GetAsync($"/api/offers/{provider1Offer.Id}")).StatusCode);
    }

    [Fact]
    public async Task AwardedRequestRejectsNewOffers()
    {
        Guid requestId = await CreateRequestAsync("client@example.com");
        OfferPayload existing = await CreateOfferAsync(requestId, "provider1@example.com", 100);
        await using (AsyncServiceScope scope = _factory.Services.CreateAsyncScope())
        {
            ProductRequestsDbContext context = scope.ServiceProvider.GetRequiredService<ProductRequestsDbContext>();
            DomainProductRequest request = await context.ProductRequests
                .Include(item => item.Offers)
                .ThenInclude(item => item.Histories)
                .SingleAsync(item => item.Id == requestId);
            Guid clientId = await context.Users.Where(user => user.NormalizedEmail == "CLIENT@EXAMPLE.COM")
                .Select(user => user.Id)
                .SingleAsync();
            request.AcceptInitialOffer(existing.Id, clientId, DateTimeOffset.UtcNow);
            await context.SaveChangesAsync();
        }

        await AuthenticateAsync("provider2@example.com");
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            $"/api/product-requests/{requestId}/offers",
            ValidOffer(90));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private async Task<Guid> CreateRequestAsync(string clientEmail)
    {
        await AuthenticateAsync(clientEmail);
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            "/api/product-requests",
            new
            {
                productName = $"Laptop-{Guid.NewGuid()}",
                description = "Business",
                quantity = 1,
                currency = "USD"
            });
        response.EnsureSuccessStatusCode();
        using JsonDocument payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return payload.RootElement.GetProperty("id").GetGuid();
    }

    private async Task<OfferPayload> CreateOfferAsync(Guid requestId, string providerEmail, decimal amount)
    {
        await AuthenticateAsync(providerEmail);
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            $"/api/product-requests/{requestId}/offers",
            ValidOffer(amount));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OfferPayload>())!;
    }

    private async Task AuthenticateAsync(string email)
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new { email, password = "Passw0rd!" });
        response.EnsureSuccessStatusCode();
        using JsonDocument payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            payload.RootElement.GetProperty("accessToken").GetString());
    }

    private static object ValidOffer(decimal amount) => new
    {
        amount,
        currency = "USD",
        deliveryDays = 7,
        notes = "Includes delivery"
    };

    private sealed record OfferPayload(Guid Id, string Currency, string Status);
    private sealed record PagedOfferPayload(OfferPayload[] Items, int TotalItems);
}

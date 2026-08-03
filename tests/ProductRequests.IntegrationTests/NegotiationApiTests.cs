using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProductRequests.Domain.Common;
using ProductRequests.Domain.Offers;
using ProductRequests.Domain.ProductRequests;
using ProductRequests.Infrastructure.Persistence;
using ProductRequests.Infrastructure.Seeding;

namespace ProductRequests.IntegrationTests;

[Collection(MySqlDatabaseFixtureSet.Name)]
[SuppressMessage("Design", "CA1001", Justification = "xUnit IAsyncLifetime disposes owned resources.")]
public sealed class NegotiationApiTests(MySqlFixture database) : IAsyncLifetime
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
    public async Task ClientAcceptsInitialOfferAtomically()
    {
        NegotiationSetup setup = await CreateSetupAsync();
        await AuthenticateAsync("client@example.com");

        HttpResponseMessage response = await _client.PostAsync(
            $"/api/offers/{setup.Offer1Id}/accept",
            null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        OfferDecisionPayload result = (await response.Content.ReadFromJsonAsync<OfferDecisionPayload>())!;
        Assert.Equal("Accepted", result.OfferStatus);
        Assert.Equal("Awarded", result.ProductRequestStatus);
        Assert.Equal(100m, result.AgreedAmount);
        Assert.Equal(setup.Offer1Id, result.AcceptedOfferId);

        await using AsyncServiceScope scope = _factory.Services.CreateAsyncScope();
        ProductRequestsDbContext context = scope.ServiceProvider.GetRequiredService<ProductRequestsDbContext>();
        ProductRequest request = await context.ProductRequests.Include(item => item.Offers)
            .ThenInclude(item => item.Histories)
            .SingleAsync(item => item.Id == setup.RequestId);
        Assert.Equal(ProductRequestStatus.Awarded, request.Status);
        Assert.Equal(OfferStatus.Accepted, request.Offers.Single(item => item.Id == setup.Offer1Id).Status);
        Assert.Equal(OfferStatus.NotSelected, request.Offers.Single(item => item.Id == setup.Offer2Id).Status);
        Assert.Contains(request.Offers.SelectMany(item => item.Histories),
            item => item.Action == OfferHistoryAction.RequestAwarded);
        Assert.Contains(request.Offers.SelectMany(item => item.Histories),
            item => item.Action == OfferHistoryAction.OfferMarkedAsNotSelected);
    }

    [Fact]
    public async Task InvalidActorMissingOfferAndSecondAttemptAreRejected()
    {
        NegotiationSetup setup = await CreateSetupAsync();

        await AuthenticateAsync("client2@example.com");
        Assert.Equal(HttpStatusCode.Forbidden,
            (await _client.PostAsync($"/api/offers/{setup.Offer1Id}/accept", null)).StatusCode);

        await AuthenticateAsync("provider1@example.com");
        Assert.Equal(HttpStatusCode.Forbidden,
            (await _client.PostAsync($"/api/offers/{setup.Offer1Id}/accept", null)).StatusCode);

        await AuthenticateAsync("client@example.com");
        Assert.Equal(HttpStatusCode.NotFound,
            (await _client.PostAsync($"/api/offers/{Guid.NewGuid()}/accept", null)).StatusCode);
        (await _client.PostAsync($"/api/offers/{setup.Offer1Id}/accept", null)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Conflict,
            (await _client.PostAsync($"/api/offers/{setup.Offer1Id}/accept", null)).StatusCode);
    }

    [Fact]
    public async Task ConcurrentAcceptsProduceSingleWinner()
    {
        NegotiationSetup setup = await CreateSetupAsync();
        await AuthenticateAsync("client@example.com");

        Task<HttpResponseMessage> first = _client.PostAsync($"/api/offers/{setup.Offer1Id}/accept", null);
        Task<HttpResponseMessage> second = _client.PostAsync($"/api/offers/{setup.Offer2Id}/accept", null);
        HttpResponseMessage[] responses = await Task.WhenAll(first, second);

        Assert.Single(responses, item => item.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, item => item.StatusCode == HttpStatusCode.Conflict);
        await using AsyncServiceScope scope = _factory.Services.CreateAsyncScope();
        ProductRequestsDbContext context = scope.ServiceProvider.GetRequiredService<ProductRequestsDbContext>();
        Assert.Equal(1, await context.Offers.CountAsync(item =>
            item.ProductRequestId == setup.RequestId && item.Status == OfferStatus.Accepted));
    }

    [Fact]
    public async Task ClientRejectsInitialOfferWithoutClosingRequest()
    {
        NegotiationSetup setup = await CreateSetupAsync();
        await AuthenticateAsync("client@example.com");

        HttpResponseMessage response = await _client.PostAsJsonAsync(
            $"/api/offers/{setup.Offer1Id}/reject",
            new { reason = "Price exceeds budget" });

        response.EnsureSuccessStatusCode();
        OfferDecisionPayload result = (await response.Content.ReadFromJsonAsync<OfferDecisionPayload>())!;
        Assert.Equal("Rejected", result.OfferStatus);
        Assert.Equal("Open", result.ProductRequestStatus);
        await using AsyncServiceScope scope = _factory.Services.CreateAsyncScope();
        ProductRequestsDbContext context = scope.ServiceProvider.GetRequiredService<ProductRequestsDbContext>();
        ProductRequest request = await context.ProductRequests.Include(item => item.Offers)
            .ThenInclude(item => item.Histories)
            .SingleAsync(item => item.Id == setup.RequestId);
        Assert.Equal(OfferStatus.PendingClientDecision,
            request.Offers.Single(item => item.Id == setup.Offer2Id).Status);
        Assert.Contains(request.Offers.Single(item => item.Id == setup.Offer1Id).Histories,
            item => item.Action == OfferHistoryAction.OfferRejectedByClient &&
                    item.Comment == "Price exceeds budget");
    }

    [Fact]
    public async Task RejectInitialEnforcesActorStateAndConcurrency()
    {
        NegotiationSetup setup = await CreateSetupAsync();
        await AuthenticateAsync("client2@example.com");
        Assert.Equal(HttpStatusCode.Forbidden,
            (await _client.PostAsJsonAsync(
                $"/api/offers/{setup.Offer1Id}/reject",
                new { reason = "No" })).StatusCode);
        await AuthenticateAsync("provider1@example.com");
        Assert.Equal(HttpStatusCode.Forbidden,
            (await _client.PostAsJsonAsync(
                $"/api/offers/{setup.Offer1Id}/reject",
                new { reason = "No" })).StatusCode);

        await AuthenticateAsync("client@example.com");
        Task<HttpResponseMessage> first = _client.PostAsJsonAsync(
            $"/api/offers/{setup.Offer1Id}/reject",
            new { reason = "First" });
        Task<HttpResponseMessage> second = _client.PostAsJsonAsync(
            $"/api/offers/{setup.Offer1Id}/reject",
            new { reason = "Second" });
        HttpResponseMessage[] responses = await Task.WhenAll(first, second);
        Assert.Single(responses, item => item.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, item => item.StatusCode == HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task InitialRejectDoesNotHandlePendingProviderDecision()
    {
        NegotiationSetup setup = await CreateSetupAsync();
        await using (AsyncServiceScope scope = _factory.Services.CreateAsyncScope())
        {
            ProductRequestsDbContext context = scope.ServiceProvider.GetRequiredService<ProductRequestsDbContext>();
            ProductRequest request = await context.ProductRequests.Include(item => item.Offers)
                .ThenInclude(item => item.Histories)
                .SingleAsync(item => item.Id == setup.RequestId);
            Guid clientId = await context.Users.Where(user => user.NormalizedEmail == "CLIENT@EXAMPLE.COM")
                .Select(user => user.Id)
                .SingleAsync();
            request.SubmitCounterOffer(
                setup.Offer1Id,
                clientId,
                new Money(80, "USD"),
                DateTimeOffset.UtcNow);
            await context.SaveChangesAsync();
        }

        await AuthenticateAsync("client@example.com");
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            $"/api/offers/{setup.Offer1Id}/reject",
            new { reason = "Wrong endpoint" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ClientSubmitsSingleCounterOffer()
    {
        NegotiationSetup setup = await CreateSetupAsync();
        await AuthenticateAsync("client@example.com");

        HttpResponseMessage response = await _client.PostAsJsonAsync(
            $"/api/offers/{setup.Offer1Id}/counter-offer",
            new { amount = 80, currency = "USD", comment = "Budget adjustment" });

        response.EnsureSuccessStatusCode();
        OfferDecisionPayload result = (await response.Content.ReadFromJsonAsync<OfferDecisionPayload>())!;
        Assert.Equal("PendingProviderDecision", result.OfferStatus);
        Assert.Equal("Open", result.ProductRequestStatus);
        await using AsyncServiceScope scope = _factory.Services.CreateAsyncScope();
        ProductRequestsDbContext context = scope.ServiceProvider.GetRequiredService<ProductRequestsDbContext>();
        Offer offer = await context.Offers.Include(item => item.Histories)
            .SingleAsync(item => item.Id == setup.Offer1Id);
        Assert.Equal(80m, offer.CounterAmount);
        Assert.Contains(offer.Histories,
            item => item.Action == OfferHistoryAction.CounterOfferSubmittedByClient &&
                    item.Comment == "Budget adjustment");

        HttpResponseMessage second = await _client.PostAsJsonAsync(
            $"/api/offers/{setup.Offer1Id}/counter-offer",
            new { amount = 75, currency = "USD", comment = "Second" });
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task CounterOfferValidatesActorMoneyAndConcurrency()
    {
        NegotiationSetup setup = await CreateSetupAsync();
        await AuthenticateAsync("client2@example.com");
        Assert.Equal(HttpStatusCode.Forbidden,
            (await _client.PostAsJsonAsync(
                $"/api/offers/{setup.Offer1Id}/counter-offer",
                new { amount = 80, currency = "USD", comment = "No" })).StatusCode);
        await AuthenticateAsync("provider1@example.com");
        Assert.Equal(HttpStatusCode.Forbidden,
            (await _client.PostAsJsonAsync(
                $"/api/offers/{setup.Offer1Id}/counter-offer",
                new { amount = 80, currency = "USD", comment = "No" })).StatusCode);

        await AuthenticateAsync("client@example.com");
        Assert.Equal(HttpStatusCode.BadRequest,
            (await _client.PostAsJsonAsync(
                $"/api/offers/{setup.Offer1Id}/counter-offer",
                new { amount = 0, currency = "USD", comment = "Invalid" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,
            (await _client.PostAsJsonAsync(
                $"/api/offers/{setup.Offer1Id}/counter-offer",
                new { amount = 80, currency = "EUR", comment = "Invalid" })).StatusCode);

        Task<HttpResponseMessage> first = _client.PostAsJsonAsync(
            $"/api/offers/{setup.Offer1Id}/counter-offer",
            new { amount = 80, currency = "USD", comment = "First" });
        Task<HttpResponseMessage> second = _client.PostAsJsonAsync(
            $"/api/offers/{setup.Offer1Id}/counter-offer",
            new { amount = 75, currency = "USD", comment = "Second" });
        HttpResponseMessage[] responses = await Task.WhenAll(first, second);
        Assert.Single(responses, item => item.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, item => item.StatusCode == HttpStatusCode.Conflict);
    }

    private async Task<NegotiationSetup> CreateSetupAsync()
    {
        await AuthenticateAsync("client@example.com");
        HttpResponseMessage requestResponse = await _client.PostAsJsonAsync(
            "/api/product-requests",
            new
            {
                productName = $"Laptop-{Guid.NewGuid()}",
                description = "Business",
                quantity = 1,
                currency = "USD"
            });
        requestResponse.EnsureSuccessStatusCode();
        using JsonDocument requestPayload = JsonDocument.Parse(await requestResponse.Content.ReadAsStringAsync());
        Guid requestId = requestPayload.RootElement.GetProperty("id").GetGuid();
        Guid offer1Id = await CreateOfferAsync(requestId, "provider1@example.com", 100);
        Guid offer2Id = await CreateOfferAsync(requestId, "provider2@example.com", 90);
        return new NegotiationSetup(requestId, offer1Id, offer2Id);
    }

    private async Task<Guid> CreateOfferAsync(Guid requestId, string email, decimal amount)
    {
        await AuthenticateAsync(email);
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            $"/api/product-requests/{requestId}/offers",
            new { amount, currency = "USD", deliveryDays = 5, notes = "Test" });
        response.EnsureSuccessStatusCode();
        using JsonDocument payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return payload.RootElement.GetProperty("id").GetGuid();
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

    private sealed record NegotiationSetup(Guid RequestId, Guid Offer1Id, Guid Offer2Id);
    private sealed record OfferDecisionPayload(
        Guid OfferId,
        string OfferStatus,
        string ProductRequestStatus,
        decimal? AgreedAmount,
        Guid? AcceptedOfferId);
}

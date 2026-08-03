using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using ProductRequests.Api.Authorization;
using ProductRequests.Application.Abstractions;
using ProductRequests.Application.Authorization;
using ProductRequests.Application.Exceptions;
using ProductRequests.Domain.Common;
using ProductRequests.Domain.ProductRequests;
using ProductRequests.Domain.Users;
using Offer = ProductRequests.Domain.Offers.Offer;

namespace ProductRequests.IntegrationTests;

[Collection(MySqlDatabaseFixtureSet.Name)]
public sealed class AuthorizationTests(MySqlFixture database)
{
    [Theory]
    [InlineData("Client", PolicyNames.Client, true)]
    [InlineData("Provider", PolicyNames.Client, false)]
    [InlineData("Provider", PolicyNames.Provider, true)]
    [InlineData("Client", PolicyNames.Provider, false)]
    public async Task RolePoliciesAreEnforced(string role, string policy, bool expected)
    {
        await using var factory = new ProductRequestsApiFactory(database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        IAuthorizationService authorization = factory.Services.GetRequiredService<IAuthorizationService>();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("role", role), new Claim("sub", Guid.NewGuid().ToString())],
            "Test",
            "name",
            "role"));

        AuthorizationResult result = await authorization.AuthorizeAsync(principal, null, policy);

        Assert.Equal(expected, result.Succeeded);
    }

    [Fact]
    public void ClientAndProviderOwnershipAreEnforced()
    {
        Guid clientId = Guid.NewGuid();
        Guid providerId = Guid.NewGuid();
        ProductRequest request = ProductRequest.Create(
            clientId, "Laptop", "Business", 1, "USD", DateTimeOffset.UtcNow);
        Offer offer = request.AddOffer(
            providerId, new Money(100, "USD"), 5, null, DateTimeOffset.UtcNow);

        var clientAuthorization = new ResourceAuthorizationService(
            new StubCurrentUser(clientId, UserRole.Client));
        clientAuthorization.EnsureClientOwns(request);
        clientAuthorization.EnsureCanAccessOffer(request, offer);

        var otherClientAuthorization = new ResourceAuthorizationService(
            new StubCurrentUser(Guid.NewGuid(), UserRole.Client));
        Assert.Throws<ResourceAccessDeniedException>(() => otherClientAuthorization.EnsureClientOwns(request));

        var providerAuthorization = new ResourceAuthorizationService(
            new StubCurrentUser(providerId, UserRole.Provider));
        providerAuthorization.EnsureProviderOwns(offer);
        providerAuthorization.EnsureCanAccessOffer(request, offer);

        var competitorAuthorization = new ResourceAuthorizationService(
            new StubCurrentUser(Guid.NewGuid(), UserRole.Provider));
        Assert.Throws<ResourceAccessDeniedException>(() => competitorAuthorization.EnsureProviderOwns(offer));
        Assert.Throws<ResourceAccessDeniedException>(() => competitorAuthorization.EnsureCanAccessOffer(request, offer));
    }

    private sealed record StubCurrentUser(Guid Id, UserRole Role) : ICurrentUser
    {
        public string Email => "test@example.com";
    }
}

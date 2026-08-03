using System.IdentityModel.Tokens.Jwt;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using ProductRequests.Domain.Users;
using ProductRequests.Infrastructure.Persistence;
using ProductRequests.Infrastructure.Seeding;

namespace ProductRequests.IntegrationTests;

[Collection(MySqlDatabaseFixtureSet.Name)]
[SuppressMessage("Design", "CA1001", Justification = "xUnit IAsyncLifetime disposes owned resources.")]
public sealed class AuthTests(MySqlFixture database) : IAsyncLifetime
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

    [Theory]
    [InlineData("client@example.com", "Client")]
    [InlineData("provider1@example.com", "Provider")]
    public async Task DemoUserCanLoginAndTokenContainsClaims(string email, string expectedRole)
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new { email, password = "Passw0rd!" });

        response.EnsureSuccessStatusCode();
        LoginPayload payload = (await response.Content.ReadFromJsonAsync<LoginPayload>())!;
        JwtSecurityToken token = new JwtSecurityTokenHandler().ReadJwtToken(payload.AccessToken);
        Assert.Equal(expectedRole, payload.User.Role);
        Assert.Equal(email, token.Claims.Single(claim => claim.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal(expectedRole, token.Claims.Single(claim => claim.Type == "role").Value);
        Assert.True(Guid.TryParse(token.Claims.Single(claim => claim.Type == JwtRegisteredClaimNames.Sub).Value, out _));
    }

    [Theory]
    [InlineData("client@example.com", "wrong")]
    [InlineData("missing@example.com", "Passw0rd!")]
    public async Task InvalidCredentialsReturnUnauthorized(string email, string password)
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        using JsonDocument problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("INVALID_CREDENTIALS", problem.RootElement.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(problem.RootElement.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task InactiveUserReturnsUnauthorized()
    {
        const string email = "inactive@example.com";
        await using (AsyncServiceScope scope = _factory.Services.CreateAsyncScope())
        {
            ProductRequestsDbContext context = scope.ServiceProvider.GetRequiredService<ProductRequestsDbContext>();
            var hasher = new PasswordHasher<User>();
            User placeholder = User.Create(Guid.NewGuid(), "Inactive", email, "pending", UserRole.Client, DateTimeOffset.UtcNow);
            User user = User.Create(
                placeholder.Id,
                placeholder.Name,
                placeholder.Email,
                hasher.HashPassword(placeholder, "Passw0rd!"),
                placeholder.Role,
                placeholder.CreatedAt);
            user.Deactivate();
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }

        HttpResponseMessage response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new { email, password = "Passw0rd!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpointRejectsMissingAndExpiredTokens()
    {
        HttpResponseMessage missing = await _client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, missing.StatusCode);

        string expiredToken = CreateExpiredToken();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);
        HttpResponseMessage expired = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, expired.StatusCode);
    }

    [Fact]
    public async Task DemoSeedingIsIdempotent()
    {
        await using AsyncServiceScope scope = _factory.Services.CreateAsyncScope();
        DemoUserSeeder seeder = scope.ServiceProvider.GetRequiredService<DemoUserSeeder>();

        await seeder.SeedAsync();
        await seeder.SeedAsync();

        ProductRequestsDbContext context = scope.ServiceProvider.GetRequiredService<ProductRequestsDbContext>();
        int count = await context.Users.CountAsync(user =>
            user.NormalizedEmail == "CLIENT@EXAMPLE.COM" ||
            user.NormalizedEmail == "CLIENT2@EXAMPLE.COM" ||
            user.NormalizedEmail == "PROVIDER1@EXAMPLE.COM" ||
            user.NormalizedEmail == "PROVIDER2@EXAMPLE.COM");
        Assert.Equal(4, count);
    }

    private static string CreateExpiredToken()
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ProductRequestsApiFactory.JwtSigningKey)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            ProductRequestsApiFactory.JwtIssuer,
            ProductRequestsApiFactory.JwtAudience,
            [
                new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email, "expired@example.com"),
                new Claim("role", "Client")
            ],
            DateTime.UtcNow.AddHours(-2),
            DateTime.UtcNow.AddHours(-1),
            credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed record LoginPayload(string AccessToken, int ExpiresIn, LoginUserPayload User);
    private sealed record LoginUserPayload(Guid Id, string Name, string Email, string Role);
}

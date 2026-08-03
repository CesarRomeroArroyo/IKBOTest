using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ProductRequests.IntegrationTests;

public sealed class ProductRequestsApiFactory(
    string connectionString,
    IReadOnlyDictionary<string, string?>? overrides = null) : WebApplicationFactory<Program>
{
    public const string JwtIssuer = "product-requests-tests";
    public const string JwtAudience = "product-requests-tests-client";
    public const string JwtSigningKey = "integration-tests-signing-key-at-least-32-characters";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:ProductRequests", connectionString);
        builder.UseSetting("Jwt:Issuer", JwtIssuer);
        builder.UseSetting("Jwt:Audience", JwtAudience);
        builder.UseSetting("Jwt:SigningKey", JwtSigningKey);
        builder.UseSetting("Jwt:ExpirationMinutes", "60");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            var values = new Dictionary<string, string?>
            {
                ["ConnectionStrings:ProductRequests"] = connectionString,
                ["Jwt:Issuer"] = JwtIssuer,
                ["Jwt:Audience"] = JwtAudience,
                ["Jwt:SigningKey"] = JwtSigningKey,
                ["Jwt:ExpirationMinutes"] = "60"
            };

            if (overrides is not null)
            {
                foreach ((string key, string? value) in overrides)
                {
                    values[key] = value;
                }
            }

            configuration.AddInMemoryCollection(values);
        });
    }
}

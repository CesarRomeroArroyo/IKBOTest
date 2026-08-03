using Microsoft.EntityFrameworkCore;
using ProductRequests.Infrastructure.Persistence;
using Testcontainers.MySql;

namespace ProductRequests.IntegrationTests;

public sealed class MySqlFixture : IAsyncLifetime
{
    private readonly MySqlContainer _container = new MySqlBuilder()
        .WithImage("mysql:8.4.0")
        .WithDatabase("product_requests_tests")
        .WithUsername("product_requests_tests")
        .WithPassword("integration_tests_only")
        .Build();

    public string ConnectionString => $"{_container.GetConnectionString()};UseAffectedRows=true";

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using ProductRequestsDbContext context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    public ProductRequestsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ProductRequestsDbContext>()
            .UseMySQL(ConnectionString, mysql => mysql.MaxBatchSize(1))
            .Options;
        return new ProductRequestsDbContext(options);
    }
}

[CollectionDefinition(Name)]
public sealed class MySqlDatabaseFixtureSet : ICollectionFixture<MySqlFixture>
{
    public const string Name = "MySQL";
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ProductRequests.Infrastructure.Persistence;

public sealed class ProductRequestsDbContextFactory : IDesignTimeDbContextFactory<ProductRequestsDbContext>
{
    public ProductRequestsDbContext CreateDbContext(string[] args)
    {
        string connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__ProductRequests")
            ?? throw new InvalidOperationException(
                "ConnectionStrings__ProductRequests is required for design-time operations.");
        var options = new DbContextOptionsBuilder<ProductRequestsDbContext>()
            .UseMySQL(connectionString, mysql => mysql.MaxBatchSize(1))
            .Options;
        return new ProductRequestsDbContext(options);
    }
}

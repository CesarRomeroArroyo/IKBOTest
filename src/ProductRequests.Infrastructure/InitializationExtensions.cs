using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProductRequests.Infrastructure.Persistence;
using ProductRequests.Infrastructure.Seeding;

namespace ProductRequests.Infrastructure;

public static class InitializationExtensions
{
    public static async Task InitializeDevelopmentAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        ProductRequestsDbContext? context = scope.ServiceProvider.GetService<ProductRequestsDbContext>();
        if (context is null)
        {
            return;
        }

        await context.Database.MigrateAsync(cancellationToken);
        await scope.ServiceProvider.GetRequiredService<DemoUserSeeder>().SeedAsync(cancellationToken);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProductRequests.Application.Abstractions;
using ProductRequests.Infrastructure.Persistence;
using ProductRequests.Infrastructure.Repositories;

namespace ProductRequests.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString("ProductRequests");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<ProductRequestsDbContext>(options =>
                options.UseMySQL(connectionString, mysql => mysql.MaxBatchSize(1)));
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IProductRequestRepository, ProductRequestRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
        }
        return services;
    }
}

using Microsoft.Extensions.DependencyInjection;
using ProductRequests.Application.Auth;
using ProductRequests.Application.Authorization;
using ProductRequests.Application.ProductRequests;

namespace ProductRequests.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<ResourceAuthorizationService>();
        services.AddScoped<ProductRequestService>();
        return services;
    }
}

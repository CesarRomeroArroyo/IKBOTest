using Microsoft.Extensions.DependencyInjection;
using ProductRequests.Application.Auth;

namespace ProductRequests.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        return services;
    }
}

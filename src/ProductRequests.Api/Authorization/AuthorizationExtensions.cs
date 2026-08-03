using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using ProductRequests.Application.Abstractions;

namespace ProductRequests.Api.Authorization;

public static class AuthorizationExtensions
{
    public static IServiceCollection AddProductRequestAuthorization(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, ProblemAuthorizationResultHandler>();
        services.AddAuthorization(options =>
        {
            options.AddPolicy(PolicyNames.Client, policy =>
                policy.RequireAuthenticatedUser().RequireRole(PolicyNames.Client));
            options.AddPolicy(PolicyNames.Provider, policy =>
                policy.RequireAuthenticatedUser().RequireRole(PolicyNames.Provider));
        });
        return services;
    }
}

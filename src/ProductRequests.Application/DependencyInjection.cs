using Microsoft.Extensions.DependencyInjection;
using ProductRequests.Application.Auth;
using ProductRequests.Application.Authorization;
using ProductRequests.Application.ProductRequests;
using ProductRequests.Application.Offers;
using ProductRequests.Application.Negotiation;
using ProductRequests.Application.History;

namespace ProductRequests.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<ResourceAuthorizationService>();
        services.AddScoped<ProductRequestService>();
        services.AddScoped<OfferService>();
        services.AddScoped<NegotiationService>();
        services.AddScoped<HistoryService>();
        return services;
    }
}

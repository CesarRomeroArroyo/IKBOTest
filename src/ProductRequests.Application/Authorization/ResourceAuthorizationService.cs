using ProductRequests.Application.Abstractions;
using ProductRequests.Application.Exceptions;
using ProductRequests.Domain.Offers;
using ProductRequests.Domain.ProductRequests;
using ProductRequests.Domain.Users;

namespace ProductRequests.Application.Authorization;

public sealed class ResourceAuthorizationService(ICurrentUser currentUser)
{
    public void EnsureClientOwns(ProductRequest request)
    {
        if (currentUser.Role != UserRole.Client || currentUser.Id != request.ClientId)
        {
            throw new ResourceAccessDeniedException("Only the client owner can access this request.");
        }
    }

    public void EnsureProviderOwns(Offer offer)
    {
        if (currentUser.Role != UserRole.Provider || currentUser.Id != offer.ProviderId)
        {
            throw new ResourceAccessDeniedException("Only the provider owner can access this offer.");
        }
    }

    public void EnsureCanAccessOffer(ProductRequest request, Offer offer)
    {
        bool clientOwner = currentUser.Role == UserRole.Client && currentUser.Id == request.ClientId;
        bool providerOwner = currentUser.Role == UserRole.Provider && currentUser.Id == offer.ProviderId;
        if (!clientOwner && !providerOwner)
        {
            throw new ResourceAccessDeniedException("Offer access is denied.");
        }
    }
}

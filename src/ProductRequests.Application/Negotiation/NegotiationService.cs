using ProductRequests.Application.Abstractions;
using ProductRequests.Application.Authorization;
using ProductRequests.Application.Exceptions;
using ProductRequests.Domain.Offers;
using ProductRequests.Domain.ProductRequests;

namespace ProductRequests.Application.Negotiation;

public sealed class NegotiationService(
    ICurrentUser currentUser,
    IProductRequestRepository requests,
    IUnitOfWork unitOfWork,
    ResourceAuthorizationService authorization)
{
    public Task<OfferDecisionDto> AcceptInitialAsync(Guid offerId, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteInTransactionAsync(async transactionToken =>
        {
            ProductRequest request = await requests.GetByOfferIdForUpdateAsync(offerId, transactionToken)
                ?? throw new ResourceNotFoundException("OFFER_NOT_FOUND", "Offer was not found.");
            Offer offer = request.Offers.Single(item => item.Id == offerId);
            authorization.EnsureClientOwns(request);
            request.AcceptInitialOffer(offerId, currentUser.Id, DateTimeOffset.UtcNow);
            return Map(request, offer);
        }, cancellationToken);

    private static OfferDecisionDto Map(ProductRequest request, Offer offer) => new(
        offer.Id,
        request.Id,
        offer.Status.ToString(),
        request.Status.ToString(),
        offer.AgreedAmount,
        request.AcceptedOfferId);
}

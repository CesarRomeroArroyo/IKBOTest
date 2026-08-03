using ProductRequests.Application.Abstractions;
using ProductRequests.Application.Authorization;
using ProductRequests.Application.Exceptions;
using ProductRequests.Domain.Offers;
using ProductRequests.Domain.ProductRequests;

namespace ProductRequests.Application.History;

public sealed class HistoryService(
    IProductRequestRepository requests,
    ResourceAuthorizationService authorization)
{
    public async Task<IReadOnlyList<OfferHistoryDto>> GetAsync(
        Guid offerId,
        CancellationToken cancellationToken)
    {
        ProductRequest request = await requests.GetByOfferIdAsync(offerId, cancellationToken)
            ?? throw new ResourceNotFoundException("OFFER_NOT_FOUND", "Offer was not found.");
        Offer offer = request.Offers.Single(item => item.Id == offerId);
        authorization.EnsureCanAccessOffer(request, offer);
        return offer.Histories
            .OrderBy(item => item.OccurredAt)
            .ThenBy(item => item.Id)
            .Select(item => new OfferHistoryDto(
                item.Id,
                item.Action.ToString(),
                item.ActorRole.ToString(),
                item.PreviousStatus?.ToString(),
                item.NewStatus?.ToString(),
                item.Amount,
                item.Comment,
                item.OccurredAt))
            .ToArray();
    }
}

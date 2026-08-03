using ProductRequests.Application.Abstractions;
using ProductRequests.Application.Authorization;
using ProductRequests.Application.Common;
using ProductRequests.Application.Exceptions;
using ProductRequests.Domain.Common;
using ProductRequests.Domain.Offers;
using ProductRequests.Domain.ProductRequests;

namespace ProductRequests.Application.Offers;

public sealed class OfferService(
    ICurrentUser currentUser,
    IProductRequestRepository requests,
    IOfferReadRepository offerReads,
    IUnitOfWork unitOfWork,
    ResourceAuthorizationService authorization)
{
    public async Task<OfferDto> CreateAsync(
        Guid requestId,
        CreateOfferCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Currency) || command.Currency.Trim().Length != 3 ||
            !command.Currency.Trim().All(char.IsLetter))
        {
            throw new ValidationException("A valid three-letter currency is required.");
        }

        ProductRequest request = await requests.GetAsync(requestId, cancellationToken)
            ?? throw new ResourceNotFoundException("PRODUCT_REQUEST_NOT_FOUND", "Product request was not found.");
        Offer offer = request.AddOffer(
            currentUser.Id,
            new Money(command.Amount, command.Currency),
            command.DeliveryDays,
            command.Notes,
            DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(offer, request.Currency);
    }

    public async Task<IReadOnlyList<OfferDto>> GetForRequestAsync(
        Guid requestId,
        CancellationToken cancellationToken)
    {
        ProductRequest request = await requests.GetAsync(requestId, cancellationToken)
            ?? throw new ResourceNotFoundException("PRODUCT_REQUEST_NOT_FOUND", "Product request was not found.");
        authorization.EnsureClientOwns(request);
        return request.Offers
            .OrderByDescending(offer => offer.CreatedAt)
            .ThenBy(offer => offer.Id)
            .Select(offer => Map(offer, request.Currency))
            .ToArray();
    }

    public async Task<PagedResult<OfferDto>> GetMineAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        ValidatePagination(page, pageSize);
        (IReadOnlyList<OfferDto> items, int total) =
            await offerReads.ListByProviderAsync(currentUser.Id, page, pageSize, cancellationToken);
        return new PagedResult<OfferDto>(
            items,
            page,
            pageSize,
            total,
            (int)Math.Ceiling(total / (double)pageSize));
    }

    public async Task<OfferDto> GetByIdAsync(Guid offerId, CancellationToken cancellationToken)
    {
        ProductRequest request = await requests.GetByOfferIdAsync(offerId, cancellationToken)
            ?? throw new ResourceNotFoundException("OFFER_NOT_FOUND", "Offer was not found.");
        Offer offer = request.Offers.Single(item => item.Id == offerId);
        authorization.EnsureCanAccessOffer(request, offer);
        return Map(offer, request.Currency);
    }

    private static OfferDto Map(Offer offer, string currency) => new(
        offer.Id,
        offer.ProductRequestId,
        offer.ProviderId,
        offer.ProposedAmount,
        offer.CounterAmount,
        offer.AgreedAmount,
        currency,
        offer.DeliveryDays,
        offer.Notes,
        offer.Status.ToString(),
        offer.CreatedAt,
        offer.UpdatedAt);

    private static void ValidatePagination(int page, int pageSize)
    {
        if (page < 1 || pageSize is < 1 or > 100)
        {
            throw new ValidationException("Page must be at least 1 and pageSize must be between 1 and 100.");
        }
    }
}

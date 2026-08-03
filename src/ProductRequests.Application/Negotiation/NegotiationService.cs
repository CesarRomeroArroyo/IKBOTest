using ProductRequests.Application.Abstractions;
using ProductRequests.Application.Authorization;
using ProductRequests.Application.Exceptions;
using ProductRequests.Domain.Common;
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

    public Task<OfferDecisionDto> RejectInitialAsync(
        Guid offerId,
        string? reason,
        CancellationToken cancellationToken) =>
        unitOfWork.ExecuteInTransactionAsync(async transactionToken =>
        {
            ProductRequest request = await requests.GetByOfferIdForUpdateAsync(offerId, transactionToken)
                ?? throw new ResourceNotFoundException("OFFER_NOT_FOUND", "Offer was not found.");
            Offer offer = request.Offers.Single(item => item.Id == offerId);
            authorization.EnsureClientOwns(request);
            request.RejectInitialOffer(offerId, currentUser.Id, DateTimeOffset.UtcNow, reason);
            return Map(request, offer);
        }, cancellationToken);

    public Task<OfferDecisionDto> SubmitCounterOfferAsync(
        Guid offerId,
        CounterOfferCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Currency) || command.Currency.Trim().Length != 3 ||
            !command.Currency.Trim().All(char.IsLetter))
        {
            throw new ValidationException("A valid three-letter currency is required.");
        }

        return unitOfWork.ExecuteInTransactionAsync(async transactionToken =>
        {
            ProductRequest request = await requests.GetByOfferIdForUpdateAsync(offerId, transactionToken)
                ?? throw new ResourceNotFoundException("OFFER_NOT_FOUND", "Offer was not found.");
            Offer offer = request.Offers.Single(item => item.Id == offerId);
            authorization.EnsureClientOwns(request);
            request.SubmitCounterOffer(
                offerId,
                currentUser.Id,
                new Money(command.Amount, command.Currency),
                DateTimeOffset.UtcNow,
                command.Comment);
            return Map(request, offer);
        }, cancellationToken);
    }

    private static OfferDecisionDto Map(ProductRequest request, Offer offer) => new(
        offer.Id,
        request.Id,
        offer.Status.ToString(),
        request.Status.ToString(),
        offer.AgreedAmount,
        request.AcceptedOfferId);
}

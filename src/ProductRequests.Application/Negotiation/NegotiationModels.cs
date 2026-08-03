namespace ProductRequests.Application.Negotiation;

public sealed record OfferDecisionDto(
    Guid OfferId,
    Guid ProductRequestId,
    string OfferStatus,
    string ProductRequestStatus,
    decimal? AgreedAmount,
    Guid? AcceptedOfferId);

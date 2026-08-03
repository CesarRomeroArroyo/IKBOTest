namespace ProductRequests.Application.Negotiation;

public sealed record OfferDecisionDto(
    Guid OfferId,
    Guid ProductRequestId,
    string OfferStatus,
    string ProductRequestStatus,
    decimal? AgreedAmount,
    Guid? AcceptedOfferId);

public sealed record RejectOfferCommand(string? Reason);

public sealed record CounterOfferCommand(decimal Amount, string? Currency, string? Comment);

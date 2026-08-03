namespace ProductRequests.Application.Offers;

public sealed record CreateOfferCommand(
    decimal Amount,
    string? Currency,
    int DeliveryDays,
    string? Notes);

public sealed record OfferDto(
    Guid Id,
    Guid ProductRequestId,
    Guid ProviderId,
    decimal ProposedAmount,
    decimal? CounterAmount,
    decimal? AgreedAmount,
    string Currency,
    int DeliveryDays,
    string? Notes,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

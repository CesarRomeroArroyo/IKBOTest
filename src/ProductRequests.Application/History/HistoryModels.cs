namespace ProductRequests.Application.History;

public sealed record OfferHistoryDto(
    Guid Id,
    string Action,
    string ActorRole,
    string? PreviousStatus,
    string? NewStatus,
    decimal? Amount,
    string? Comment,
    DateTimeOffset OccurredAt);

using ProductRequests.Domain.Users;

namespace ProductRequests.Domain.Offers;

public sealed class OfferHistory
{
    private OfferHistory()
    {
    }

    internal OfferHistory(
        Guid offerId,
        Guid productRequestId,
        Guid actorId,
        UserRole actorRole,
        OfferHistoryAction action,
        OfferStatus? previousStatus,
        OfferStatus? newStatus,
        decimal? amount,
        string? comment,
        DateTimeOffset occurredAt)
    {
        Id = Guid.NewGuid();
        OfferId = offerId;
        ProductRequestId = productRequestId;
        ActorId = actorId;
        ActorRole = actorRole;
        Action = action;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        Amount = amount;
        Comment = comment;
        OccurredAt = occurredAt.ToUniversalTime();
    }

    public Guid Id { get; private set; }
    public Guid OfferId { get; private set; }
    public Guid ProductRequestId { get; private set; }
    public Guid ActorId { get; private set; }
    public UserRole ActorRole { get; private set; }
    public OfferHistoryAction Action { get; private set; }
    public OfferStatus? PreviousStatus { get; private set; }
    public OfferStatus? NewStatus { get; private set; }
    public decimal? Amount { get; private set; }
    public string? Comment { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
}

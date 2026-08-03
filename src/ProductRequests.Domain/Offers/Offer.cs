using ProductRequests.Domain.Common;
using ProductRequests.Domain.Exceptions;
using ProductRequests.Domain.Users;

namespace ProductRequests.Domain.Offers;

public sealed class Offer
{
    private readonly List<OfferHistory> _histories = [];

    private Offer()
    {
    }

    internal Offer(
        Guid productRequestId,
        Guid providerId,
        Money proposed,
        int deliveryDays,
        string? notes,
        DateTimeOffset now)
    {
        if (providerId == Guid.Empty)
        {
            throw new DomainException("VALIDATION_ERROR", "Provider is required.");
        }

        if (deliveryDays <= 0)
        {
            throw new DomainException("INVALID_DELIVERY_DAYS", "Delivery days must be greater than zero.");
        }

        Id = Guid.NewGuid();
        ProductRequestId = productRequestId;
        ProviderId = providerId;
        ProposedAmount = proposed.Amount;
        DeliveryDays = deliveryDays;
        Notes = NormalizeOptional(notes, 1000);
        Status = OfferStatus.PendingClientDecision;
        CreatedAt = now.ToUniversalTime();
        UpdatedAt = CreatedAt;
        Version = Guid.NewGuid();
    }

    public Guid Id { get; private set; }
    public Guid ProductRequestId { get; private set; }
    public Guid ProviderId { get; private set; }
    public decimal ProposedAmount { get; private set; }
    public decimal? CounterAmount { get; private set; }
    public decimal? AgreedAmount { get; private set; }
    public int DeliveryDays { get; private set; }
    public string? Notes { get; private set; }
    public OfferStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid Version { get; private set; }
    public IReadOnlyCollection<OfferHistory> Histories => _histories.AsReadOnly();

    internal void AddHistory(
        Guid actorId,
        UserRole actorRole,
        OfferHistoryAction action,
        OfferStatus? previousStatus,
        OfferStatus? newStatus,
        decimal? amount,
        string? comment,
        DateTimeOffset now) =>
        _histories.Add(new OfferHistory(
            Id, ProductRequestId, actorId, actorRole, action, previousStatus, newStatus,
            amount, NormalizeOptional(comment, 1000), now));

    internal void SubmitCounterOffer(Money counter, Guid actorId, DateTimeOffset now, string? comment)
    {
        EnsureStatus(OfferStatus.PendingClientDecision, "OFFER_NOT_PENDING_CLIENT_DECISION");
        if (CounterAmount.HasValue)
        {
            throw new DomainException("COUNTER_OFFER_ALREADY_EXISTS", "A counter offer already exists.");
        }

        OfferStatus previous = Status;
        CounterAmount = counter.Amount;
        Status = OfferStatus.PendingProviderDecision;
        Touch(now);
        AddHistory(actorId, UserRole.Client, OfferHistoryAction.CounterOfferSubmittedByClient,
            previous, Status, CounterAmount, comment, now);
    }

    internal void AcceptInitial(Guid actorId, DateTimeOffset now)
    {
        EnsureStatus(OfferStatus.PendingClientDecision, "OFFER_NOT_PENDING_CLIENT_DECISION");
        OfferStatus previous = Status;
        Status = OfferStatus.Accepted;
        AgreedAmount = ProposedAmount;
        Touch(now);
        AddHistory(actorId, UserRole.Client, OfferHistoryAction.OfferAcceptedByClient,
            previous, Status, AgreedAmount, null, now);
    }

    internal void RejectInitial(Guid actorId, DateTimeOffset now, string? reason)
    {
        EnsureStatus(OfferStatus.PendingClientDecision, "OFFER_NOT_PENDING_CLIENT_DECISION");
        Reject(actorId, UserRole.Client, OfferHistoryAction.OfferRejectedByClient, now, reason);
    }

    internal void AcceptCounter(Guid actorId, DateTimeOffset now)
    {
        EnsureStatus(OfferStatus.PendingProviderDecision, "OFFER_NOT_PENDING_PROVIDER_DECISION");
        if (!CounterAmount.HasValue)
        {
            throw new DomainException("OFFER_NOT_PENDING_PROVIDER_DECISION", "Counter offer is missing.");
        }

        OfferStatus previous = Status;
        Status = OfferStatus.Accepted;
        AgreedAmount = CounterAmount.Value;
        Touch(now);
        AddHistory(actorId, UserRole.Provider, OfferHistoryAction.CounterOfferAcceptedByProvider,
            previous, Status, AgreedAmount, null, now);
    }

    internal void RejectCounter(Guid actorId, DateTimeOffset now, string? reason)
    {
        EnsureStatus(OfferStatus.PendingProviderDecision, "OFFER_NOT_PENDING_PROVIDER_DECISION");
        Reject(actorId, UserRole.Provider, OfferHistoryAction.CounterOfferRejectedByProvider, now, reason);
    }

    internal void MarkNotSelected(Guid actorId, UserRole actorRole, DateTimeOffset now)
    {
        if (Status is not (OfferStatus.PendingClientDecision or OfferStatus.PendingProviderDecision))
        {
            return;
        }

        OfferStatus previous = Status;
        Status = OfferStatus.NotSelected;
        Touch(now);
        AddHistory(actorId, actorRole, OfferHistoryAction.OfferMarkedAsNotSelected,
            previous, Status, null, null, now);
    }

    private void Reject(
        Guid actorId,
        UserRole actorRole,
        OfferHistoryAction action,
        DateTimeOffset now,
        string? reason)
    {
        OfferStatus previous = Status;
        Status = OfferStatus.Rejected;
        Touch(now);
        AddHistory(actorId, actorRole, action, previous, Status, null, reason, now);
    }

    private void EnsureStatus(OfferStatus expected, string code)
    {
        if (Status is OfferStatus.Accepted or OfferStatus.Rejected or OfferStatus.NotSelected)
        {
            throw new DomainException("OFFER_ALREADY_RESOLVED", "Offer is already resolved.");
        }

        if (Status != expected)
        {
            throw new DomainException(code, $"Offer must be {expected}.");
        }
    }

    private void Touch(DateTimeOffset now)
    {
        UpdatedAt = now.ToUniversalTime();
        Version = Guid.NewGuid();
    }

    private static string? NormalizeOptional(string? value, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string normalized = value.Trim();
        if (normalized.Length > maximumLength)
        {
            throw new DomainException("VALIDATION_ERROR", $"Text cannot exceed {maximumLength} characters.");
        }

        return normalized;
    }
}

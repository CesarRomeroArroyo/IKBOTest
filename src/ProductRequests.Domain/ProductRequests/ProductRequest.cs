using ProductRequests.Domain.Common;
using ProductRequests.Domain.Exceptions;
using ProductRequests.Domain.Offers;
using ProductRequests.Domain.Users;

namespace ProductRequests.Domain.ProductRequests;

public sealed class ProductRequest
{
    private readonly List<Offer> _offers = [];

    private ProductRequest()
    {
    }

    private ProductRequest(
        Guid clientId,
        string productName,
        string description,
        int quantity,
        string currency,
        DateTimeOffset now)
    {
        Id = Guid.NewGuid();
        ClientId = clientId;
        ProductName = productName;
        Description = description;
        Quantity = quantity;
        Currency = currency;
        Status = ProductRequestStatus.Open;
        CreatedAt = now.ToUniversalTime();
        UpdatedAt = CreatedAt;
        Version = Guid.NewGuid();
    }

    public Guid Id { get; private set; }
    public Guid ClientId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public ProductRequestStatus Status { get; private set; }
    public Guid? AcceptedOfferId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid Version { get; private set; }
    public IReadOnlyCollection<Offer> Offers => _offers.AsReadOnly();

    public static ProductRequest Create(
        Guid clientId,
        string productName,
        string description,
        int quantity,
        string currency,
        DateTimeOffset now)
    {
        if (clientId == Guid.Empty || string.IsNullOrWhiteSpace(productName) || string.IsNullOrWhiteSpace(description))
        {
            throw new DomainException("VALIDATION_ERROR", "Client, product name and description are required.");
        }

        if (quantity <= 0)
        {
            throw new DomainException("INVALID_QUANTITY", "Quantity must be greater than zero.");
        }

        string normalizedCurrency = new Money(1m, currency).Currency;
        return new ProductRequest(clientId, productName.Trim(), description.Trim(), quantity, normalizedCurrency, now);
    }

    public Offer AddOffer(
        Guid providerId,
        Money proposed,
        int deliveryDays,
        string? notes,
        DateTimeOffset now)
    {
        EnsureOpen();
        if (!string.Equals(Currency, proposed.Currency, StringComparison.Ordinal))
        {
            throw new DomainException("CURRENCY_MISMATCH", "Offer currency must match request currency.");
        }

        if (_offers.Any(offer => offer.ProviderId == providerId))
        {
            throw new DomainException("DUPLICATE_PROVIDER_OFFER", "Provider already submitted an offer.");
        }

        var offer = new Offer(Id, providerId, proposed, deliveryDays, notes, now);
        offer.AddHistory(providerId, UserRole.Provider, OfferHistoryAction.OfferSubmitted,
            null, OfferStatus.PendingClientDecision, proposed.Amount, notes, now);
        _offers.Add(offer);
        Touch(now);
        return offer;
    }

    public void AcceptInitialOffer(Guid offerId, Guid actorId, DateTimeOffset now)
    {
        EnsureOpen();
        Offer selected = GetOffer(offerId);
        selected.AcceptInitial(actorId, now);
        Award(selected, actorId, UserRole.Client, now);
    }

    public void RejectInitialOffer(Guid offerId, Guid actorId, DateTimeOffset now, string? reason = null)
    {
        EnsureOpen();
        GetOffer(offerId).RejectInitial(actorId, now, reason);
        Touch(now);
    }

    public void SubmitCounterOffer(
        Guid offerId,
        Guid actorId,
        Money counter,
        DateTimeOffset now,
        string? comment = null)
    {
        EnsureOpen();
        if (!string.Equals(Currency, counter.Currency, StringComparison.Ordinal))
        {
            throw new DomainException("CURRENCY_MISMATCH", "Counter offer currency must match request currency.");
        }

        GetOffer(offerId).SubmitCounterOffer(counter, actorId, now, comment);
        Touch(now);
    }

    public void AcceptCounterOffer(Guid offerId, Guid actorId, DateTimeOffset now)
    {
        EnsureOpen();
        Offer selected = GetOffer(offerId);
        selected.AcceptCounter(actorId, now);
        Award(selected, actorId, UserRole.Provider, now);
    }

    public void RejectCounterOffer(Guid offerId, Guid actorId, DateTimeOffset now, string? reason = null)
    {
        EnsureOpen();
        GetOffer(offerId).RejectCounter(actorId, now, reason);
        Touch(now);
    }

    private void Award(Offer selected, Guid actorId, UserRole actorRole, DateTimeOffset now)
    {
        if (AcceptedOfferId.HasValue)
        {
            throw new DomainException("REQUEST_ALREADY_AWARDED", "Request already has an accepted offer.");
        }

        Status = ProductRequestStatus.Awarded;
        AcceptedOfferId = selected.Id;
        selected.AddHistory(actorId, actorRole, OfferHistoryAction.RequestAwarded,
            selected.Status, selected.Status, selected.AgreedAmount, null, now);

        foreach (Offer competitor in _offers.Where(offer => offer.Id != selected.Id))
        {
            competitor.MarkNotSelected(actorId, actorRole, now);
        }

        Touch(now);
    }

    private Offer GetOffer(Guid offerId) =>
        _offers.SingleOrDefault(offer => offer.Id == offerId)
        ?? throw new DomainException("OFFER_NOT_FOUND", "Offer was not found.");

    private void EnsureOpen()
    {
        if (Status == ProductRequestStatus.Awarded)
        {
            throw new DomainException("REQUEST_ALREADY_AWARDED", "Request is already awarded.");
        }

        if (Status != ProductRequestStatus.Open)
        {
            throw new DomainException("PRODUCT_REQUEST_NOT_OPEN", "Request is not open.");
        }
    }

    private void Touch(DateTimeOffset now)
    {
        UpdatedAt = now.ToUniversalTime();
        Version = Guid.NewGuid();
    }
}

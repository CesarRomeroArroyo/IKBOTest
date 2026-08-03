using ProductRequests.Domain.Common;
using ProductRequests.Domain.Exceptions;
using ProductRequests.Domain.Offers;
using ProductRequests.Domain.ProductRequests;

namespace ProductRequests.Domain.Tests;

public sealed class ProductRequestTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private readonly Guid _clientId = Guid.NewGuid();
    private readonly Guid _provider1 = Guid.NewGuid();
    private readonly Guid _provider2 = Guid.NewGuid();

    [Fact]
    public void ValidRequestStartsOpen()
    {
        ProductRequest request = CreateRequest();

        Assert.Equal(ProductRequestStatus.Open, request.Status);
        Assert.Null(request.AcceptedOfferId);
        Assert.Equal(TimeSpan.Zero, request.CreatedAt.Offset);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void InvalidQuantityIsRejected(int quantity)
    {
        DomainException error = Assert.Throws<DomainException>(() =>
            ProductRequest.Create(_clientId, "Laptop", "Business", quantity, "USD", Now));

        Assert.Equal("INVALID_QUANTITY", error.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("US")]
    [InlineData("US12")]
    public void InvalidCurrencyIsRejected(string currency)
    {
        Assert.Throws<DomainException>(() =>
            ProductRequest.Create(_clientId, "Laptop", "Business", 1, currency, Now));
    }

    [Fact]
    public void ValidOfferStartsPendingClientDecision()
    {
        Offer offer = AddOffer(CreateRequest(), _provider1, 100m);

        Assert.Equal(OfferStatus.PendingClientDecision, offer.Status);
        Assert.Single(offer.Histories);
        Assert.Equal(OfferHistoryAction.OfferSubmitted, offer.Histories.Single().Action);
    }

    [Fact]
    public void InvalidOfferValuesAreRejected()
    {
        ProductRequest request = CreateRequest();

        Assert.Throws<DomainException>(() => request.AddOffer(_provider1, new Money(0m, "USD"), 1, null, Now));
        Assert.Throws<DomainException>(() => request.AddOffer(_provider1, new Money(1m, "USD"), 0, null, Now));
    }

    [Fact]
    public void CannotOfferOnAwardedRequest()
    {
        ProductRequest request = CreateRequest();
        Offer offer = AddOffer(request, _provider1, 100m);
        request.AcceptInitialOffer(offer.Id, _clientId, Now.AddMinutes(1));

        Assert.Throws<DomainException>(() => AddOffer(request, _provider2, 90m));
    }

    [Fact]
    public void ProviderCannotSubmitSecondOffer()
    {
        ProductRequest request = CreateRequest();
        AddOffer(request, _provider1, 100m);

        DomainException error = Assert.Throws<DomainException>(() => AddOffer(request, _provider1, 90m));

        Assert.Equal("DUPLICATE_PROVIDER_OFFER", error.Code);
    }

    [Fact]
    public void InitialAcceptanceAwardsProposedAmountAndMarksCompetitor()
    {
        ProductRequest request = CreateRequest();
        Offer selected = AddOffer(request, _provider1, 100m);
        Offer competitor = AddOffer(request, _provider2, 90m);

        request.AcceptInitialOffer(selected.Id, _clientId, Now.AddMinutes(1));

        Assert.Equal(ProductRequestStatus.Awarded, request.Status);
        Assert.Equal(selected.Id, request.AcceptedOfferId);
        Assert.Equal(OfferStatus.Accepted, selected.Status);
        Assert.Equal(100m, selected.AgreedAmount);
        Assert.Equal(OfferStatus.NotSelected, competitor.Status);
        Assert.Contains(selected.Histories, item => item.Action == OfferHistoryAction.RequestAwarded);
        Assert.Contains(competitor.Histories, item => item.Action == OfferHistoryAction.OfferMarkedAsNotSelected);
    }

    [Fact]
    public void InitialRejectionKeepsRequestOpen()
    {
        ProductRequest request = CreateRequest();
        Offer offer = AddOffer(request, _provider1, 100m);

        request.RejectInitialOffer(offer.Id, _clientId, Now.AddMinutes(1), "Too high");

        Assert.Equal(ProductRequestStatus.Open, request.Status);
        Assert.Equal(OfferStatus.Rejected, offer.Status);
        Assert.Equal("Too high", offer.Histories.Last().Comment);
    }

    [Fact]
    public void CounterOfferChangesStateAndSecondCounterIsRejected()
    {
        ProductRequest request = CreateRequest();
        Offer offer = AddOffer(request, _provider1, 100m);

        request.SubmitCounterOffer(offer.Id, _clientId, new Money(90m, "USD"), Now.AddMinutes(1));

        Assert.Equal(ProductRequestStatus.Open, request.Status);
        Assert.Equal(OfferStatus.PendingProviderDecision, offer.Status);
        Assert.Equal(90m, offer.CounterAmount);
        Assert.Throws<DomainException>(() =>
            request.SubmitCounterOffer(offer.Id, _clientId, new Money(80m, "USD"), Now.AddMinutes(2)));
    }

    [Fact]
    public void AcceptedCounterUsesCounterAmount()
    {
        ProductRequest request = CreateRequest();
        Offer offer = AddOffer(request, _provider1, 100m);
        request.SubmitCounterOffer(offer.Id, _clientId, new Money(90m, "USD"), Now.AddMinutes(1));

        request.AcceptCounterOffer(offer.Id, _provider1, Now.AddMinutes(2));

        Assert.Equal(ProductRequestStatus.Awarded, request.Status);
        Assert.Equal(OfferStatus.Accepted, offer.Status);
        Assert.Equal(90m, offer.AgreedAmount);
    }

    [Fact]
    public void RejectedCounterKeepsRequestOpen()
    {
        ProductRequest request = CreateRequest();
        Offer offer = AddOffer(request, _provider1, 100m);
        request.SubmitCounterOffer(offer.Id, _clientId, new Money(90m, "USD"), Now.AddMinutes(1));

        request.RejectCounterOffer(offer.Id, _provider1, Now.AddMinutes(2));

        Assert.Equal(ProductRequestStatus.Open, request.Status);
        Assert.Equal(OfferStatus.Rejected, offer.Status);
    }

    [Fact]
    public void TerminalOfferCannotChangeAndRequestCannotAwardTwice()
    {
        ProductRequest request = CreateRequest();
        Offer selected = AddOffer(request, _provider1, 100m);
        Offer competitor = AddOffer(request, _provider2, 90m);
        request.AcceptInitialOffer(selected.Id, _clientId, Now.AddMinutes(1));

        Assert.Throws<DomainException>(() =>
            request.AcceptInitialOffer(competitor.Id, _clientId, Now.AddMinutes(2)));
        Assert.Throws<DomainException>(() =>
            request.RejectInitialOffer(selected.Id, _clientId, Now.AddMinutes(2)));
    }

    [Fact]
    public void EveryModificationUpdatesVersionAndTimestamp()
    {
        ProductRequest request = CreateRequest();
        Guid originalVersion = request.Version;

        AddOffer(request, _provider1, 100m);

        Assert.NotEqual(originalVersion, request.Version);
        Assert.True(request.UpdatedAt > request.CreatedAt);
    }

    [Fact]
    public void DifferentCurrencyIsRejected()
    {
        ProductRequest request = CreateRequest();

        DomainException error = Assert.Throws<DomainException>(() =>
            request.AddOffer(_provider1, new Money(100m, "EUR"), 5, null, Now));

        Assert.Equal("CURRENCY_MISMATCH", error.Code);
    }

    private ProductRequest CreateRequest() =>
        ProductRequest.Create(_clientId, "Laptop", "Business laptop", 2, "usd", Now);

    private static Offer AddOffer(ProductRequest request, Guid providerId, decimal amount) =>
        request.AddOffer(providerId, new Money(amount, "USD"), 5, null, Now.AddSeconds(1));
}

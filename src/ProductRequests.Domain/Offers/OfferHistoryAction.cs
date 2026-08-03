namespace ProductRequests.Domain.Offers;

public enum OfferHistoryAction
{
    OfferSubmitted = 1,
    OfferAcceptedByClient = 2,
    OfferRejectedByClient = 3,
    CounterOfferSubmittedByClient = 4,
    CounterOfferAcceptedByProvider = 5,
    CounterOfferRejectedByProvider = 6,
    OfferMarkedAsNotSelected = 7,
    RequestAwarded = 8
}

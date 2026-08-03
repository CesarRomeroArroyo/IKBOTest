namespace ProductRequests.Domain.Offers;

public enum OfferStatus
{
    PendingClientDecision = 1,
    PendingProviderDecision = 2,
    Accepted = 3,
    Rejected = 4,
    NotSelected = 5
}

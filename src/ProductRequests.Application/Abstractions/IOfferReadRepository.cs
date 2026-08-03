using ProductRequests.Application.Offers;

namespace ProductRequests.Application.Abstractions;

public interface IOfferReadRepository
{
    Task<(IReadOnlyList<OfferDto> Items, int Total)> ListByProviderAsync(
        Guid providerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}

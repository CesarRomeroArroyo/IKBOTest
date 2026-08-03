using Microsoft.EntityFrameworkCore;
using ProductRequests.Application.Abstractions;
using ProductRequests.Application.Offers;
using ProductRequests.Infrastructure.Persistence;

namespace ProductRequests.Infrastructure.Repositories;

internal sealed class OfferReadRepository(ProductRequestsDbContext context) : IOfferReadRepository
{
    public async Task<(IReadOnlyList<OfferDto> Items, int Total)> ListByProviderAsync(
        Guid providerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = from offer in context.Offers.AsNoTracking()
                    join request in context.ProductRequests.AsNoTracking()
                        on offer.ProductRequestId equals request.Id
                    where offer.ProviderId == providerId
                    select new { Offer = offer, request.Currency };
        int total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(row => row.Offer.CreatedAt)
            .ThenBy(row => row.Offer.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        OfferDto[] items = rows.Select(row => new OfferDto(
            row.Offer.Id,
            row.Offer.ProductRequestId,
            row.Offer.ProviderId,
            row.Offer.ProposedAmount,
            row.Offer.CounterAmount,
            row.Offer.AgreedAmount,
            row.Currency,
            row.Offer.DeliveryDays,
            row.Offer.Notes,
            row.Offer.Status.ToString(),
            row.Offer.CreatedAt,
            row.Offer.UpdatedAt)).ToArray();
        return (items, total);
    }
}

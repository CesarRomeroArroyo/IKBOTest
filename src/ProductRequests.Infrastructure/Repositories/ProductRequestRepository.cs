using Microsoft.EntityFrameworkCore;
using ProductRequests.Application.Abstractions;
using ProductRequests.Domain.ProductRequests;
using ProductRequests.Infrastructure.Persistence;

namespace ProductRequests.Infrastructure.Repositories;

internal sealed class ProductRequestRepository(ProductRequestsDbContext context) : IProductRequestRepository
{
    public Task<ProductRequest?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        context.ProductRequests
            .Include(request => request.Offers)
            .ThenInclude(offer => offer.Histories)
            .SingleOrDefaultAsync(request => request.Id == id, cancellationToken);

    public void Add(ProductRequest productRequest) => context.ProductRequests.Add(productRequest);
}

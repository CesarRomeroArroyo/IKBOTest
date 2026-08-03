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

    public Task<ProductRequest?> GetReadOnlyAsync(Guid id, CancellationToken cancellationToken) =>
        context.ProductRequests.AsNoTracking()
            .SingleOrDefaultAsync(request => request.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<ProductRequest> Items, int Total)> ListByClientAsync(
        Guid clientId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        IQueryable<ProductRequest> query = context.ProductRequests.AsNoTracking()
            .Where(request => request.ClientId == clientId);
        int total = await query.CountAsync(cancellationToken);
        List<ProductRequest> items = await query
            .OrderByDescending(request => request.CreatedAt)
            .ThenBy(request => request.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<(IReadOnlyList<ProductRequest> Items, int Total)> ListOpenAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        IQueryable<ProductRequest> query = context.ProductRequests.AsNoTracking()
            .Where(request => request.Status == ProductRequestStatus.Open);
        int total = await query.CountAsync(cancellationToken);
        List<ProductRequest> items = await query
            .OrderByDescending(request => request.CreatedAt)
            .ThenBy(request => request.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    public void Add(ProductRequest productRequest) => context.ProductRequests.Add(productRequest);
}

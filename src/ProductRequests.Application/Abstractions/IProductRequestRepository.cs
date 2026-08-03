using ProductRequests.Domain.ProductRequests;

namespace ProductRequests.Application.Abstractions;

public interface IProductRequestRepository
{
    Task<ProductRequest?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<ProductRequest?> GetReadOnlyAsync(Guid id, CancellationToken cancellationToken);
    Task<(IReadOnlyList<ProductRequest> Items, int Total)> ListByClientAsync(
        Guid clientId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
    Task<(IReadOnlyList<ProductRequest> Items, int Total)> ListOpenAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken);
    void Add(ProductRequest productRequest);
}

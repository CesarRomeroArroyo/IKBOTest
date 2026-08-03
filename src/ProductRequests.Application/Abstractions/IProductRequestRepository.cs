using ProductRequests.Domain.ProductRequests;

namespace ProductRequests.Application.Abstractions;

public interface IProductRequestRepository
{
    Task<ProductRequest?> GetAsync(Guid id, CancellationToken cancellationToken);
    void Add(ProductRequest productRequest);
}

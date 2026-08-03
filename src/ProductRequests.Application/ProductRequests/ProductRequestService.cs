using ProductRequests.Application.Abstractions;
using ProductRequests.Application.Authorization;
using ProductRequests.Application.Common;
using ProductRequests.Application.Exceptions;
using ProductRequests.Domain.ProductRequests;
using ProductRequests.Domain.Users;

namespace ProductRequests.Application.ProductRequests;

public sealed class ProductRequestService(
    ICurrentUser currentUser,
    IProductRequestRepository requests,
    IUnitOfWork unitOfWork,
    ResourceAuthorizationService authorization)
{
    public async Task<ProductRequestDto> CreateAsync(
        CreateProductRequestCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.ProductName) ||
            string.IsNullOrWhiteSpace(command.Description) ||
            string.IsNullOrWhiteSpace(command.Currency) ||
            command.Currency.Trim().Length != 3 ||
            !command.Currency.Trim().All(char.IsLetter))
        {
            throw new ValidationException("Product name, description and currency are required.");
        }

        ProductRequest request = ProductRequest.Create(
            currentUser.Id,
            command.ProductName,
            command.Description,
            command.Quantity,
            command.Currency,
            DateTimeOffset.UtcNow);
        requests.Add(request);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(request);
    }

    public async Task<PagedResult<ProductRequestDto>> GetMineAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        ValidatePagination(page, pageSize);
        (IReadOnlyList<ProductRequest> items, int total) =
            await requests.ListByClientAsync(currentUser.Id, page, pageSize, cancellationToken);
        return CreatePage(items.Select(Map).ToArray(), page, pageSize, total);
    }

    public async Task<PagedResult<ProductRequestDto>> GetOpenAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        ValidatePagination(page, pageSize);
        (IReadOnlyList<ProductRequest> items, int total) =
            await requests.ListOpenAsync(page, pageSize, cancellationToken);
        return CreatePage(items.Select(Map).ToArray(), page, pageSize, total);
    }

    public async Task<ProductRequestDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        ProductRequest request = await requests.GetReadOnlyAsync(id, cancellationToken)
            ?? throw new ResourceNotFoundException("PRODUCT_REQUEST_NOT_FOUND", "Product request was not found.");

        if (currentUser.Role == UserRole.Client)
        {
            authorization.EnsureClientOwns(request);
        }
        else if (currentUser.Role != UserRole.Provider || request.Status != ProductRequestStatus.Open)
        {
            throw new ResourceAccessDeniedException("Product request access is denied.");
        }

        return Map(request);
    }

    private static ProductRequestDto Map(ProductRequest request) => new(
        request.Id,
        request.ProductName,
        request.Description,
        request.Quantity,
        request.Currency,
        request.Status.ToString(),
        request.AcceptedOfferId,
        request.CreatedAt,
        request.UpdatedAt);

    private static PagedResult<ProductRequestDto> CreatePage(
        IReadOnlyList<ProductRequestDto> items,
        int page,
        int pageSize,
        int total) =>
        new(items, page, pageSize, total, (int)Math.Ceiling(total / (double)pageSize));

    private static void ValidatePagination(int page, int pageSize)
    {
        if (page < 1 || pageSize is < 1 or > 100)
        {
            throw new ValidationException("Page must be at least 1 and pageSize must be between 1 and 100.");
        }
    }
}

namespace ProductRequests.Application.ProductRequests;

public sealed record CreateProductRequestCommand(
    string? ProductName,
    string? Description,
    int Quantity,
    string? Currency);

public sealed record ProductRequestDto(
    Guid Id,
    string ProductName,
    string Description,
    int Quantity,
    string Currency,
    string Status,
    Guid? AcceptedOfferId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

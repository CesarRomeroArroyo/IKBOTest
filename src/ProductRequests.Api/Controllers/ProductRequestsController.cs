using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductRequests.Api.Authorization;
using ProductRequests.Application.Common;
using ProductRequests.Application.ProductRequests;

namespace ProductRequests.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/product-requests")]
public sealed class ProductRequestsController(ProductRequestService service) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = PolicyNames.Client)]
    [ProducesResponseType<ProductRequestDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ProductRequestDto>> Create(
        CreateProductRequestCommand command,
        CancellationToken cancellationToken)
    {
        ProductRequestDto created = await service.CreateAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { requestId = created.Id }, created);
    }

    [HttpGet("mine")]
    [Authorize(Policy = PolicyNames.Client)]
    public Task<PagedResult<ProductRequestDto>> GetMine(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        service.GetMineAsync(page, pageSize, cancellationToken);

    [HttpGet("open")]
    [Authorize(Policy = PolicyNames.Provider)]
    public Task<PagedResult<ProductRequestDto>> GetOpen(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        service.GetOpenAsync(page, pageSize, cancellationToken);

    [HttpGet("{requestId:guid}")]
    public Task<ProductRequestDto> GetById(Guid requestId, CancellationToken cancellationToken) =>
        service.GetByIdAsync(requestId, cancellationToken);
}

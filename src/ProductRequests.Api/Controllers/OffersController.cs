using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductRequests.Api.Authorization;
using ProductRequests.Application.Common;
using ProductRequests.Application.Offers;

namespace ProductRequests.Api.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public sealed class OffersController(OfferService service) : ControllerBase
{
    [HttpPost("product-requests/{requestId:guid}/offers")]
    [Authorize(Policy = PolicyNames.Provider)]
    public async Task<ActionResult<OfferDto>> Create(
        Guid requestId,
        CreateOfferCommand command,
        CancellationToken cancellationToken)
    {
        OfferDto created = await service.CreateAsync(requestId, command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { offerId = created.Id }, created);
    }

    [HttpGet("product-requests/{requestId:guid}/offers")]
    [Authorize(Policy = PolicyNames.Client)]
    public Task<IReadOnlyList<OfferDto>> GetForRequest(
        Guid requestId,
        CancellationToken cancellationToken) =>
        service.GetForRequestAsync(requestId, cancellationToken);

    [HttpGet("offers/mine")]
    [Authorize(Policy = PolicyNames.Provider)]
    public Task<PagedResult<OfferDto>> GetMine(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        service.GetMineAsync(page, pageSize, cancellationToken);

    [HttpGet("offers/{offerId:guid}")]
    public Task<OfferDto> GetById(Guid offerId, CancellationToken cancellationToken) =>
        service.GetByIdAsync(offerId, cancellationToken);
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductRequests.Api.Authorization;
using ProductRequests.Application.Common;
using ProductRequests.Application.Offers;
using ProductRequests.Application.Negotiation;

namespace ProductRequests.Api.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public sealed class OffersController(OfferService service, NegotiationService negotiation) : ControllerBase
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

    [HttpPost("offers/{offerId:guid}/accept")]
    [Authorize(Policy = PolicyNames.Client)]
    public Task<OfferDecisionDto> AcceptInitial(Guid offerId, CancellationToken cancellationToken) =>
        negotiation.AcceptInitialAsync(offerId, cancellationToken);

    [HttpPost("offers/{offerId:guid}/reject")]
    [Authorize(Policy = PolicyNames.Client)]
    public Task<OfferDecisionDto> RejectInitial(
        Guid offerId,
        RejectOfferCommand command,
        CancellationToken cancellationToken) =>
        negotiation.RejectInitialAsync(offerId, command.Reason, cancellationToken);
}

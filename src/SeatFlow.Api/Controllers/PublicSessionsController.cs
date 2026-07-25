using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeatFlow.Application.Catalog;

namespace SeatFlow.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/sessions")]
public sealed class PublicSessionsController
    : ControllerBase
{
    private readonly IEventCatalogService
        _eventCatalogService;

    public PublicSessionsController(
        IEventCatalogService eventCatalogService)
    {
        _eventCatalogService =
            eventCatalogService;
    }

    [HttpGet("{sessionId:guid}/seats")]
    public async Task<ActionResult<SessionSeatMap>>
        GetSessionSeats(
            Guid sessionId,
            CancellationToken cancellationToken)
    {
        var result =
            await _eventCatalogService
                .GetSessionSeatsAsync(
                    sessionId,
                    cancellationToken);

        return Ok(result);
    }
}
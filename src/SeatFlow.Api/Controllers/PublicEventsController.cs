using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeatFlow.Api.Contracts.Catalog;
using SeatFlow.Application.Catalog;

namespace SeatFlow.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/events")]
public sealed class PublicEventsController
    : ControllerBase
{
    private readonly IEventCatalogService
        _eventCatalogService;

    public PublicEventsController(
        IEventCatalogService eventCatalogService)
    {
        _eventCatalogService =
            eventCatalogService;
    }

    [HttpGet]
    public async Task<
        ActionResult<PagedResult<EventCatalogItem>>>
        GetEvents(
            [FromQuery] EventCatalogRequest request,
            CancellationToken cancellationToken)
    {
        var result =
            await _eventCatalogService.GetEventsAsync(
                request.ToQuery(),
                cancellationToken);

        return Ok(result);
    }

    [HttpGet("{eventId:guid}")]
    public async Task<ActionResult<EventCatalogDetails>>
        GetEvent(
            Guid eventId,
            CancellationToken cancellationToken)
    {
        var result =
            await _eventCatalogService.GetEventAsync(
                eventId,
                cancellationToken);

        return Ok(result);
    }

    [HttpGet("{eventId:guid}/sessions")]
    public async Task<
        ActionResult<
            PagedResult<EventSessionCatalogItem>>>
        GetEventSessions(
            Guid eventId,
            [FromQuery] EventSessionCatalogRequest request,
            CancellationToken cancellationToken)
    {
        var result =
            await _eventCatalogService
                .GetEventSessionsAsync(
                    eventId,
                    request.ToQuery(),
                    cancellationToken);

        return Ok(result);
    }
}
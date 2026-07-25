using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeatFlow.Api.Contracts.Catalog;
using SeatFlow.Application.Catalog;
using SeatFlow.Domain.Enums;

namespace SeatFlow.Api.Controllers;

[ApiController]
[Authorize(Roles = nameof(UserRole.Admin))]
[Route("api/admin/events")]
public sealed class EventsController
    : ControllerBase
{
    private readonly IEventManagementService
        _eventManagementService;

    public EventsController(
        IEventManagementService eventManagementService)
    {
        _eventManagementService =
            eventManagementService;
    }

    [HttpPost]
    public async Task<ActionResult<EventDetails>> Create(
        CreateEventRequest request,
        CancellationToken cancellationToken)
    {
        var eventDetails =
            await _eventManagementService.CreateEventAsync(
                request.Title,
                request.Description,
                request.Category,
                request.AgeRestriction,
                cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new
            {
                eventId = eventDetails.Id
            },
            eventDetails);
    }

    [HttpGet("{eventId:guid}")]
    public async Task<ActionResult<EventDetails>> GetById(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var eventDetails =
            await _eventManagementService.GetEventAsync(
                eventId,
                cancellationToken);

        return Ok(eventDetails);
    }

    [HttpPut("{eventId:guid}")]
    public async Task<ActionResult<EventDetails>> Update(
        Guid eventId,
        UpdateEventRequest request,
        CancellationToken cancellationToken)
    {
        var eventDetails =
            await _eventManagementService.UpdateEventAsync(
                eventId,
                request.Title,
                request.Description,
                request.Category,
                request.AgeRestriction,
                cancellationToken);

        return Ok(eventDetails);
    }

    [HttpDelete("{eventId:guid}")]
    public async Task<IActionResult> Delete(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        await _eventManagementService.DeleteEventAsync(
            eventId,
            cancellationToken);

        return NoContent();
    }
}
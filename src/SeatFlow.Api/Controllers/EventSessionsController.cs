using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeatFlow.Api.Contracts.Catalog;
using SeatFlow.Application.Catalog;
using SeatFlow.Domain.Enums;

namespace SeatFlow.Api.Controllers;

[ApiController]
[Authorize(Roles = nameof(UserRole.Admin))]
[Route("api/admin/sessions")]
public sealed class EventSessionsController
    : ControllerBase
{
    private readonly IEventManagementService
        _eventManagementService;

    public EventSessionsController(
        IEventManagementService eventManagementService)
    {
        _eventManagementService =
            eventManagementService;
    }

    [HttpPost]
    public async Task<ActionResult<EventSessionDetails>> Create(
        CreateEventSessionRequest request,
        CancellationToken cancellationToken)
    {
        var session =
            await _eventManagementService.CreateSessionAsync(
                request.EventId,
                request.HallId,
                request.StartsAtUtc,
                request.BookingOpensAtUtc,
                request.BookingClosesAtUtc,
                request.DefaultPrice,
                cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new
            {
                sessionId = session.Id
            },
            session);
    }

    [HttpGet("{sessionId:guid}")]
    public async Task<ActionResult<EventSessionDetails>> GetById(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var session =
            await _eventManagementService.GetSessionAsync(
                sessionId,
                cancellationToken);

        return Ok(session);
    }

    [HttpPut("{sessionId:guid}")]
    public async Task<ActionResult<EventSessionDetails>> Update(
        Guid sessionId,
        UpdateEventSessionRequest request,
        CancellationToken cancellationToken)
    {
        var session =
            await _eventManagementService.UpdateSessionAsync(
                sessionId,
                request.StartsAtUtc,
                request.BookingOpensAtUtc,
                request.BookingClosesAtUtc,
                cancellationToken);

        return Ok(session);
    }

    [HttpPost("{sessionId:guid}/cancel")]
    public async Task<ActionResult<EventSessionDetails>> Cancel(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var session =
            await _eventManagementService.CancelSessionAsync(
                sessionId,
                cancellationToken);

        return Ok(session);
    }

    [HttpDelete("{sessionId:guid}")]
    public async Task<IActionResult> Delete(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        await _eventManagementService.DeleteSessionAsync(
            sessionId,
            cancellationToken);

        return NoContent();
    }
}
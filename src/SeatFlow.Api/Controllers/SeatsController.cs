using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeatFlow.Api.Contracts.Catalog;
using SeatFlow.Application.Catalog;
using SeatFlow.Domain.Enums;

namespace SeatFlow.Api.Controllers;

[ApiController]
[Authorize(Roles = nameof(UserRole.Admin))]
[Route("api/admin/seats")]
public sealed class SeatsController
    : ControllerBase
{
    private readonly IEventManagementService
        _eventManagementService;

    public SeatsController(
        IEventManagementService eventManagementService)
    {
        _eventManagementService =
            eventManagementService;
    }

    [HttpPost]
    public async Task<ActionResult<SeatDetails>> Create(
        CreateSeatRequest request,
        CancellationToken cancellationToken)
    {
        var seat =
            await _eventManagementService.CreateSeatAsync(
                request.HallId,
                request.RowLabel,
                request.Number,
                request.Category,
                cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new
            {
                seatId = seat.Id
            },
            seat);
    }

    [HttpGet("{seatId:guid}")]
    public async Task<ActionResult<SeatDetails>> GetById(
        Guid seatId,
        CancellationToken cancellationToken)
    {
        var seat =
            await _eventManagementService.GetSeatAsync(
                seatId,
                cancellationToken);

        return Ok(seat);
    }

    [HttpPut("{seatId:guid}")]
    public async Task<ActionResult<SeatDetails>> Update(
        Guid seatId,
        UpdateSeatRequest request,
        CancellationToken cancellationToken)
    {
        var seat =
            await _eventManagementService.UpdateSeatAsync(
                seatId,
                request.RowLabel,
                request.Number,
                request.Category,
                cancellationToken);

        return Ok(seat);
    }

    [HttpDelete("{seatId:guid}")]
    public async Task<IActionResult> Delete(
        Guid seatId,
        CancellationToken cancellationToken)
    {
        await _eventManagementService.DeleteSeatAsync(
            seatId,
            cancellationToken);

        return NoContent();
    }
}
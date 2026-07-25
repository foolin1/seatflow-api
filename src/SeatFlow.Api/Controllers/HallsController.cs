using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeatFlow.Api.Contracts.Catalog;
using SeatFlow.Application.Catalog;
using SeatFlow.Domain.Enums;

namespace SeatFlow.Api.Controllers;

[ApiController]
[Authorize(Roles = nameof(UserRole.Admin))]
[Route("api/admin/halls")]
public sealed class HallsController
    : ControllerBase
{
    private readonly IEventManagementService
        _eventManagementService;

    public HallsController(
        IEventManagementService eventManagementService)
    {
        _eventManagementService =
            eventManagementService;
    }

    [HttpPost]
    public async Task<ActionResult<HallDetails>> Create(
        CreateHallRequest request,
        CancellationToken cancellationToken)
    {
        var hall =
            await _eventManagementService.CreateHallAsync(
                request.VenueId,
                request.Name,
                request.Capacity,
                cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new
            {
                hallId = hall.Id
            },
            hall);
    }

    [HttpGet("{hallId:guid}")]
    public async Task<ActionResult<HallDetails>> GetById(
        Guid hallId,
        CancellationToken cancellationToken)
    {
        var hall =
            await _eventManagementService.GetHallAsync(
                hallId,
                cancellationToken);

        return Ok(hall);
    }

    [HttpPut("{hallId:guid}")]
    public async Task<ActionResult<HallDetails>> Update(
        Guid hallId,
        UpdateHallRequest request,
        CancellationToken cancellationToken)
    {
        var hall =
            await _eventManagementService.UpdateHallAsync(
                hallId,
                request.Name,
                request.Capacity,
                cancellationToken);

        return Ok(hall);
    }

    [HttpDelete("{hallId:guid}")]
    public async Task<IActionResult> Delete(
        Guid hallId,
        CancellationToken cancellationToken)
    {
        await _eventManagementService.DeleteHallAsync(
            hallId,
            cancellationToken);

        return NoContent();
    }
}
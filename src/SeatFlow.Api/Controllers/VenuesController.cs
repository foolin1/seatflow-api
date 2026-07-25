using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeatFlow.Api.Contracts.Catalog;
using SeatFlow.Application.Catalog;
using SeatFlow.Domain.Enums;

namespace SeatFlow.Api.Controllers;

[ApiController]
[Authorize(Roles = nameof(UserRole.Admin))]
[Route("api/admin/venues")]
public sealed class VenuesController
    : ControllerBase
{
    private readonly IEventManagementService
        _eventManagementService;

    public VenuesController(
        IEventManagementService eventManagementService)
    {
        _eventManagementService =
            eventManagementService;
    }

    [HttpPost]
    public async Task<ActionResult<VenueDetails>> Create(
        CreateVenueRequest request,
        CancellationToken cancellationToken)
    {
        var venue =
            await _eventManagementService.CreateVenueAsync(
                request.Name,
                request.Address,
                request.Description,
                cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new
            {
                venueId = venue.Id
            },
            venue);
    }

    [HttpGet("{venueId:guid}")]
    public async Task<ActionResult<VenueDetails>> GetById(
        Guid venueId,
        CancellationToken cancellationToken)
    {
        var venue =
            await _eventManagementService.GetVenueAsync(
                venueId,
                cancellationToken);

        return Ok(venue);
    }

    [HttpPut("{venueId:guid}")]
    public async Task<ActionResult<VenueDetails>> Update(
        Guid venueId,
        UpdateVenueRequest request,
        CancellationToken cancellationToken)
    {
        var venue =
            await _eventManagementService.UpdateVenueAsync(
                venueId,
                request.Name,
                request.Address,
                request.Description,
                cancellationToken);

        return Ok(venue);
    }

    [HttpDelete("{venueId:guid}")]
    public async Task<IActionResult> Delete(
        Guid venueId,
        CancellationToken cancellationToken)
    {
        await _eventManagementService.DeleteVenueAsync(
            venueId,
            cancellationToken);

        return NoContent();
    }
}
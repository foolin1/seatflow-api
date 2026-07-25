using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeatFlow.Api.Contracts.Reservations;
using SeatFlow.Application.Reservations;

namespace SeatFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/reservations")]
public sealed class ReservationsController
    : ControllerBase
{
    private const string SubjectClaim = "sub";

    private readonly IReservationService
        _reservationService;

    public ReservationsController(
        IReservationService reservationService)
    {
        _reservationService =
            reservationService;
    }

    [HttpPost]
    public async Task<ActionResult<ReservationDetails>>
        Create(
            CreateReservationRequest request,
            CancellationToken cancellationToken)
    {
        if (!TryGetUserId(
                out var userId))
        {
            return Unauthorized();
        }

        var reservation =
            await _reservationService
                .CreateReservationAsync(
                    userId,
                    request.EventSessionId,
                    request.SessionSeatIds,
                    cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new
            {
                reservationId =
                    reservation.Id
            },
            reservation);
    }

    [HttpGet]
    public async Task<
        ActionResult<
            IReadOnlyList<ReservationDetails>>>
        GetMine(
            CancellationToken cancellationToken)
    {
        if (!TryGetUserId(
                out var userId))
        {
            return Unauthorized();
        }

        var reservations =
            await _reservationService
                .GetUserReservationsAsync(
                    userId,
                    cancellationToken);

        return Ok(reservations);
    }

    [HttpGet("{reservationId:guid}")]
    public async Task<ActionResult<ReservationDetails>>
        GetById(
            Guid reservationId,
            CancellationToken cancellationToken)
    {
        if (!TryGetUserId(
                out var userId))
        {
            return Unauthorized();
        }

        var reservation =
            await _reservationService
                .GetReservationAsync(
                    userId,
                    reservationId,
                    cancellationToken);

        return Ok(reservation);
    }

    [HttpPost("{reservationId:guid}/cancel")]
    public async Task<ActionResult<ReservationDetails>>
        Cancel(
            Guid reservationId,
            CancellationToken cancellationToken)
    {
        if (!TryGetUserId(
                out var userId))
        {
            return Unauthorized();
        }

        var reservation =
            await _reservationService
                .CancelReservationAsync(
                    userId,
                    reservationId,
                    cancellationToken);

        return Ok(reservation);
    }

    [HttpPost("{reservationId:guid}/pay")]
    public async Task<ActionResult<ReservationDetails>>
        Pay(
            Guid reservationId,
            CancellationToken cancellationToken)
    {
        if (!TryGetUserId(
                out var userId))
        {
            return Unauthorized();
        }

        var reservation =
            await _reservationService
                .PayReservationAsync(
                    userId,
                    reservationId,
                    cancellationToken);

        return Ok(reservation);
    }

    private bool TryGetUserId(
        out Guid userId)
    {
        var subject =
            User.FindFirstValue(
                SubjectClaim);

        return Guid.TryParse(
            subject,
            out userId);
    }
}
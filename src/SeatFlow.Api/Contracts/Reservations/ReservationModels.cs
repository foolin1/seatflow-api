using System.ComponentModel.DataAnnotations;

namespace SeatFlow.Api.Contracts.Reservations;

public sealed class CreateReservationRequest
{
    public Guid EventSessionId { get; init; }

    [Required]
    [MinLength(1)]
    [MaxLength(8)]
    public Guid[] SessionSeatIds { get; init; } = [];
}
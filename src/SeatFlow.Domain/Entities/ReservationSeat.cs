using SeatFlow.Domain.Common;

namespace SeatFlow.Domain.Entities;

public sealed class ReservationSeat : Entity
{
    private ReservationSeat()
    {
    }

    public ReservationSeat(
        Guid id,
        Guid reservationId,
        Guid sessionSeatId,
        decimal price)
        : base(id)
    {
        Guard.AgainstEmpty(
            reservationId,
            nameof(reservationId));

        Guard.AgainstEmpty(
            sessionSeatId,
            nameof(sessionSeatId));

        ReservationId = reservationId;
        SessionSeatId = sessionSeatId;
        Price = Guard.PositiveMoney(
            price,
            nameof(Price));
    }

    public Guid ReservationId { get; private set; }

    public Guid SessionSeatId { get; private set; }

    public decimal Price { get; private set; }
}
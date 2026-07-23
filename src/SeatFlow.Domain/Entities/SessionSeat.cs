using SeatFlow.Domain.Common;
using SeatFlow.Domain.Enums;
using SeatFlow.Domain.Exceptions;

namespace SeatFlow.Domain.Entities;

public sealed class SessionSeat : Entity
{
    private SessionSeat()
    {
    }

    public SessionSeat(
        Guid id,
        Guid eventSessionId,
        Guid seatId,
        decimal price)
        : base(id)
    {
        Guard.AgainstEmpty(
            eventSessionId,
            nameof(eventSessionId));

        Guard.AgainstEmpty(
            seatId,
            nameof(seatId));

        EventSessionId = eventSessionId;
        SeatId = seatId;
        Price = Guard.PositiveMoney(
            price,
            nameof(Price));

        Status = SessionSeatStatus.Available;
    }

    public Guid EventSessionId { get; private set; }

    public Guid SeatId { get; private set; }

    public decimal Price { get; private set; }

    public SessionSeatStatus Status { get; private set; }

    public Guid? ReservationId { get; private set; }

    public DateTimeOffset? ReservedUntilUtc { get; private set; }

    public uint Version { get; private set; }

    public void ChangePrice(decimal price)
    {
        if (Status != SessionSeatStatus.Available)
        {
            throw new DomainConflictException(
                "The price of a reserved or sold seat cannot be changed.");
        }

        Price = Guard.PositiveMoney(
            price,
            nameof(price));
    }

    public void Reserve(
        Guid reservationId,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset currentTimeUtc)
    {
        Guard.AgainstEmpty(
            reservationId,
            nameof(reservationId));

        if (Status != SessionSeatStatus.Available)
        {
            throw new SeatUnavailableException(
                Id,
                Status);
        }

        var normalizedCurrentTime =
            currentTimeUtc.ToUniversalTime();

        var normalizedExpiration =
            expiresAtUtc.ToUniversalTime();

        if (normalizedExpiration <= normalizedCurrentTime)
        {
            throw new DomainValidationException(
                "Reservation expiration time must be in the future.");
        }

        Status = SessionSeatStatus.Reserved;
        ReservationId = reservationId;
        ReservedUntilUtc = normalizedExpiration;
    }

    public void MarkAsSold(
        Guid reservationId,
        DateTimeOffset currentTimeUtc)
    {
        EnsureOwnedReservation(reservationId);

        if (ReservedUntilUtc is null)
        {
            throw new DomainConflictException(
                $"Session seat '{Id}' does not have an expiration time.");
        }

        var normalizedCurrentTime =
            currentTimeUtc.ToUniversalTime();

        if (ReservedUntilUtc.Value <= normalizedCurrentTime)
        {
            throw new ReservationExpiredException(
                reservationId,
                ReservedUntilUtc.Value);
        }

        Status = SessionSeatStatus.Sold;
        ReservedUntilUtc = null;
    }

    public void Release(Guid reservationId)
    {
        EnsureOwnedReservation(reservationId);
        MakeAvailable();
    }

    public bool ExpireReservation(
        DateTimeOffset currentTimeUtc)
    {
        if (Status != SessionSeatStatus.Reserved ||
            ReservedUntilUtc is null)
        {
            return false;
        }

        var normalizedCurrentTime =
            currentTimeUtc.ToUniversalTime();

        if (ReservedUntilUtc.Value > normalizedCurrentTime)
        {
            return false;
        }

        MakeAvailable();

        return true;
    }

    private void EnsureOwnedReservation(Guid reservationId)
    {
        Guard.AgainstEmpty(
            reservationId,
            nameof(reservationId));

        if (Status != SessionSeatStatus.Reserved ||
            ReservationId != reservationId)
        {
            throw new DomainConflictException(
                $"Session seat '{Id}' is not reserved by " +
                $"reservation '{reservationId}'.");
        }
    }

    private void MakeAvailable()
    {
        Status = SessionSeatStatus.Available;
        ReservationId = null;
        ReservedUntilUtc = null;
    }
}
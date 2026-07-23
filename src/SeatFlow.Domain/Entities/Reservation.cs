using SeatFlow.Domain.Common;
using SeatFlow.Domain.Enums;
using SeatFlow.Domain.Exceptions;

namespace SeatFlow.Domain.Entities;

public sealed class Reservation : Entity
{
    private readonly List<ReservationSeat> _seats = [];

    private Reservation()
    {
    }

    public Reservation(
        Guid id,
        Guid userId,
        Guid eventSessionId,
        IEnumerable<ReservationSeat> seats,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc)
        : base(id)
    {
        Guard.AgainstEmpty(
            userId,
            nameof(userId));

        Guard.AgainstEmpty(
            eventSessionId,
            nameof(eventSessionId));

        ArgumentNullException.ThrowIfNull(seats);

        var normalizedCreatedAt =
            createdAtUtc.ToUniversalTime();

        var normalizedExpiresAt =
            expiresAtUtc.ToUniversalTime();

        if (normalizedExpiresAt <= normalizedCreatedAt)
        {
            throw new DomainValidationException(
                "Reservation expiration time must be after creation time.");
        }

        var reservationSeats = seats.ToList();

        if (reservationSeats.Count == 0)
        {
            throw new DomainValidationException(
                "A reservation must contain at least one seat.");
        }

        if (reservationSeats.Any(
                seat => seat.ReservationId != id))
        {
            throw new DomainValidationException(
                "All reservation seats must belong to the reservation.");
        }

        var containsDuplicateSeats = reservationSeats
            .GroupBy(seat => seat.SessionSeatId)
            .Any(group => group.Count() > 1);

        if (containsDuplicateSeats)
        {
            throw new DomainValidationException(
                "A reservation cannot contain duplicate session seats.");
        }

        UserId = userId;
        EventSessionId = eventSessionId;
        CreatedAtUtc = normalizedCreatedAt;
        ExpiresAtUtc = normalizedExpiresAt;
        Status = ReservationStatus.Pending;

        _seats.AddRange(reservationSeats);

        TotalAmount = _seats.Sum(
            seat => seat.Price);
    }

    public Guid UserId { get; private set; }

    public Guid EventSessionId { get; private set; }

    public ReservationStatus Status { get; private set; }

    public decimal TotalAmount { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset? ConfirmedAtUtc { get; private set; }

    public DateTimeOffset? CancelledAtUtc { get; private set; }

    public DateTimeOffset? ExpiredAtUtc { get; private set; }

    public IReadOnlyCollection<ReservationSeat> Seats => _seats;

    public void Confirm(DateTimeOffset confirmedAtUtc)
    {
        EnsurePending("confirm");

        var normalizedConfirmationTime =
            confirmedAtUtc.ToUniversalTime();

        if (normalizedConfirmationTime >= ExpiresAtUtc)
        {
            throw new ReservationExpiredException(
                Id,
                ExpiresAtUtc);
        }

        if (normalizedConfirmationTime < CreatedAtUtc)
        {
            throw new DomainValidationException(
                "Confirmation time cannot be before creation time.");
        }

        Status = ReservationStatus.Confirmed;
        ConfirmedAtUtc = normalizedConfirmationTime;
    }

    public void Cancel(DateTimeOffset cancelledAtUtc)
    {
        EnsurePending("cancel");

        var normalizedCancellationTime =
            cancelledAtUtc.ToUniversalTime();

        if (normalizedCancellationTime >= ExpiresAtUtc)
        {
            throw new ReservationExpiredException(
                Id,
                ExpiresAtUtc);
        }

        if (normalizedCancellationTime < CreatedAtUtc)
        {
            throw new DomainValidationException(
                "Cancellation time cannot be before creation time.");
        }

        Status = ReservationStatus.Cancelled;
        CancelledAtUtc = normalizedCancellationTime;
    }

    public bool Expire(DateTimeOffset currentTimeUtc)
    {
        if (Status != ReservationStatus.Pending)
        {
            return false;
        }

        var normalizedCurrentTime =
            currentTimeUtc.ToUniversalTime();

        if (normalizedCurrentTime < ExpiresAtUtc)
        {
            return false;
        }

        Status = ReservationStatus.Expired;
        ExpiredAtUtc = normalizedCurrentTime;

        return true;
    }

    private void EnsurePending(string operation)
    {
        if (Status != ReservationStatus.Pending)
        {
            throw new InvalidReservationStateException(
                Id,
                Status,
                operation);
        }
    }
}
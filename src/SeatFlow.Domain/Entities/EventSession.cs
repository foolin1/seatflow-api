using SeatFlow.Domain.Common;
using SeatFlow.Domain.Exceptions;

namespace SeatFlow.Domain.Entities;

public sealed class EventSession : Entity
{
    private EventSession()
    {
    }

    public EventSession(
        Guid id,
        Guid eventId,
        Guid hallId,
        DateTimeOffset startsAtUtc,
        DateTimeOffset bookingOpensAtUtc,
        DateTimeOffset bookingClosesAtUtc)
        : base(id)
    {
        Guard.AgainstEmpty(eventId, nameof(eventId));
        Guard.AgainstEmpty(hallId, nameof(hallId));

        EventId = eventId;
        HallId = hallId;

        Schedule(
            startsAtUtc,
            bookingOpensAtUtc,
            bookingClosesAtUtc);
    }

    public Guid EventId { get; private set; }

    public Guid HallId { get; private set; }

    public DateTimeOffset StartsAtUtc { get; private set; }

    public DateTimeOffset BookingOpensAtUtc { get; private set; }

    public DateTimeOffset BookingClosesAtUtc { get; private set; }

    public bool IsCancelled { get; private set; }

    public DateTimeOffset? CancelledAtUtc { get; private set; }

    public bool IsBookableAt(DateTimeOffset currentTimeUtc)
    {
        var normalizedCurrentTime =
            currentTimeUtc.ToUniversalTime();

        return !IsCancelled &&
               normalizedCurrentTime >= BookingOpensAtUtc &&
               normalizedCurrentTime < BookingClosesAtUtc;
    }

    public void EnsureBookableAt(DateTimeOffset currentTimeUtc)
    {
        if (!IsBookableAt(currentTimeUtc))
        {
            throw new DomainConflictException(
                $"Event session '{Id}' is not available for booking.");
        }
    }

    public void Schedule(
        DateTimeOffset startsAtUtc,
        DateTimeOffset bookingOpensAtUtc,
        DateTimeOffset bookingClosesAtUtc)
    {
        var normalizedStart =
            startsAtUtc.ToUniversalTime();

        var normalizedBookingOpen =
            bookingOpensAtUtc.ToUniversalTime();

        var normalizedBookingClose =
            bookingClosesAtUtc.ToUniversalTime();

        if (normalizedBookingOpen >= normalizedBookingClose)
        {
            throw new DomainValidationException(
                "Booking opening time must be before " +
                "booking closing time.");
        }

        if (normalizedBookingClose > normalizedStart)
        {
            throw new DomainValidationException(
                "Booking closing time cannot be after " +
                "the session start.");
        }

        StartsAtUtc = normalizedStart;
        BookingOpensAtUtc = normalizedBookingOpen;
        BookingClosesAtUtc = normalizedBookingClose;
    }

    public void Cancel(DateTimeOffset cancelledAtUtc)
    {
        if (IsCancelled)
        {
            throw new DomainConflictException(
                $"Event session '{Id}' has already been cancelled.");
        }

        var normalizedCancellationTime =
            cancelledAtUtc.ToUniversalTime();

        if (normalizedCancellationTime >= StartsAtUtc)
        {
            throw new DomainConflictException(
                "A session cannot be cancelled after it has started.");
        }

        IsCancelled = true;
        CancelledAtUtc = normalizedCancellationTime;
    }
}
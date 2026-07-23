using SeatFlow.Domain.Enums;

namespace SeatFlow.Domain.Exceptions;

public abstract class DomainException : Exception
{
    protected DomainException(string message)
        : base(message)
    {
    }
}

public sealed class DomainValidationException : DomainException
{
    public DomainValidationException(string message)
        : base(message)
    {
    }
}

public class DomainConflictException : DomainException
{
    public DomainConflictException(string message)
        : base(message)
    {
    }
}

public sealed class SeatUnavailableException
    : DomainConflictException
{
    public SeatUnavailableException(
        Guid sessionSeatId,
        SessionSeatStatus currentStatus)
        : base(
            $"Session seat '{sessionSeatId}' is not available. " +
            $"Current status: {currentStatus}.")
    {
        SessionSeatId = sessionSeatId;
        CurrentStatus = currentStatus;
    }

    public Guid SessionSeatId { get; }

    public SessionSeatStatus CurrentStatus { get; }
}

public sealed class InvalidReservationStateException
    : DomainConflictException
{
    public InvalidReservationStateException(
        Guid reservationId,
        ReservationStatus currentStatus,
        string operation)
        : base(
            $"Reservation '{reservationId}' cannot perform operation " +
            $"'{operation}' while its status is {currentStatus}.")
    {
        ReservationId = reservationId;
        CurrentStatus = currentStatus;
        Operation = operation;
    }

    public Guid ReservationId { get; }

    public ReservationStatus CurrentStatus { get; }

    public string Operation { get; }
}

public sealed class ReservationExpiredException
    : DomainConflictException
{
    public ReservationExpiredException(
        Guid reservationId,
        DateTimeOffset expiresAtUtc)
        : base(
            $"Reservation '{reservationId}' expired at " +
            $"{expiresAtUtc:O}.")
    {
        ReservationId = reservationId;
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid ReservationId { get; }

    public DateTimeOffset ExpiresAtUtc { get; }
}
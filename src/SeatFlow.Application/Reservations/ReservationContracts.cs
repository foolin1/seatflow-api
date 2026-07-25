using SeatFlow.Domain.Enums;

namespace SeatFlow.Application.Reservations;

public sealed record ReservationSeatDetails(
    Guid SessionSeatId,
    Guid SeatId,
    string RowLabel,
    int Number,
    SeatCategory Category,
    decimal Price,
    SessionSeatStatus Status);

public sealed record PaymentDetails(
    Guid Id,
    decimal Amount,
    PaymentStatus Status,
    string? ExternalReference,
    string? FailureReason,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CompletedAtUtc);

public sealed record ReservationDetails(
    Guid Id,
    Guid EventSessionId,
    Guid EventId,
    string EventTitle,
    string VenueName,
    string HallName,
    DateTimeOffset StartsAtUtc,
    ReservationStatus Status,
    decimal TotalAmount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? ConfirmedAtUtc,
    DateTimeOffset? CancelledAtUtc,
    DateTimeOffset? ExpiredAtUtc,
    IReadOnlyList<ReservationSeatDetails> Seats,
    PaymentDetails? Payment);

public interface IReservationService
{
    Task<ReservationDetails> CreateReservationAsync(
        Guid userId,
        Guid eventSessionId,
        IReadOnlyCollection<Guid> sessionSeatIds,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ReservationDetails>>
        GetUserReservationsAsync(
            Guid userId,
            CancellationToken cancellationToken);

    Task<ReservationDetails> GetReservationAsync(
        Guid userId,
        Guid reservationId,
        CancellationToken cancellationToken);

    Task<ReservationDetails> CancelReservationAsync(
        Guid userId,
        Guid reservationId,
        CancellationToken cancellationToken);

    Task<ReservationDetails> PayReservationAsync(
        Guid userId,
        Guid reservationId,
        CancellationToken cancellationToken);

    Task<int> ExpireReservationsAsync(
        CancellationToken cancellationToken);
}
using SeatFlow.Domain.Entities;
using SeatFlow.Domain.Enums;
using SeatFlow.Domain.Exceptions;

namespace SeatFlow.UnitTests;

public sealed class ReservationFlowDomainTests
{
    [Fact]
    public void SessionSeat_ReserveAndSell_ChangesStatus()
    {
        var currentTimeUtc =
            new DateTimeOffset(
                2026,
                7,
                25,
                12,
                0,
                0,
                TimeSpan.Zero);

        var reservationId =
            Guid.NewGuid();

        var sessionSeat =
            new SessionSeat(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                75m);

        sessionSeat.Reserve(
            reservationId,
            currentTimeUtc.AddMinutes(10),
            currentTimeUtc);

        Assert.Equal(
            SessionSeatStatus.Reserved,
            sessionSeat.Status);

        sessionSeat.MarkAsSold(
            reservationId,
            currentTimeUtc.AddMinutes(1));

        Assert.Equal(
            SessionSeatStatus.Sold,
            sessionSeat.Status);

        Assert.Null(
            sessionSeat.ReservedUntilUtc);
    }

    [Fact]
    public void Reservation_Cancel_ChangesStatus()
    {
        var currentTimeUtc =
            DateTimeOffset.UtcNow;

        var reservation =
            CreateReservation(
                currentTimeUtc);

        reservation.Cancel(
            currentTimeUtc.AddMinutes(1));

        Assert.Equal(
            ReservationStatus.Cancelled,
            reservation.Status);

        Assert.NotNull(
            reservation.CancelledAtUtc);
    }

    [Fact]
    public void Reservation_Expire_WhenDue_ChangesStatus()
    {
        var currentTimeUtc =
            DateTimeOffset.UtcNow;

        var reservation =
            CreateReservation(
                currentTimeUtc);

        var wasExpired =
            reservation.Expire(
                currentTimeUtc.AddMinutes(10));

        Assert.True(
            wasExpired);

        Assert.Equal(
            ReservationStatus.Expired,
            reservation.Status);
    }

    [Fact]
    public void Payment_Succeed_ChangesStatus()
    {
        var currentTimeUtc =
            DateTimeOffset.UtcNow;

        var payment =
            new Payment(
                Guid.NewGuid(),
                Guid.NewGuid(),
                150m,
                currentTimeUtc);

        payment.Succeed(
            "test-payment",
            currentTimeUtc.AddSeconds(1));

        Assert.Equal(
            PaymentStatus.Succeeded,
            payment.Status);

        Assert.Equal(
            "test-payment",
            payment.ExternalReference);
    }

    [Fact]
    public void Reservation_WithDuplicateSeats_ThrowsValidation()
    {
        var currentTimeUtc =
            DateTimeOffset.UtcNow;

        var reservationId =
            Guid.NewGuid();

        var sessionSeatId =
            Guid.NewGuid();

        var reservationSeats =
            new[]
            {
                new ReservationSeat(
                    Guid.NewGuid(),
                    reservationId,
                    sessionSeatId,
                    50m),

                new ReservationSeat(
                    Guid.NewGuid(),
                    reservationId,
                    sessionSeatId,
                    50m)
            };

        Assert.Throws<
            DomainValidationException>(
                () =>
                    new Reservation(
                        reservationId,
                        Guid.NewGuid(),
                        Guid.NewGuid(),
                        reservationSeats,
                        currentTimeUtc,
                        currentTimeUtc
                            .AddMinutes(10)));
    }

    private static Reservation CreateReservation(
        DateTimeOffset currentTimeUtc)
    {
        var reservationId =
            Guid.NewGuid();

        var reservationSeat =
            new ReservationSeat(
                Guid.NewGuid(),
                reservationId,
                Guid.NewGuid(),
                50m);

        return new Reservation(
            reservationId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            new[]
            {
                reservationSeat
            },
            currentTimeUtc,
            currentTimeUtc.AddMinutes(10));
    }
}
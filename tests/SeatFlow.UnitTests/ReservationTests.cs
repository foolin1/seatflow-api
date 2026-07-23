using SeatFlow.Domain.Entities;
using SeatFlow.Domain.Enums;
using SeatFlow.Domain.Exceptions;

namespace SeatFlow.UnitTests;

public sealed class ReservationTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(
            2026,
            8,
            1,
            12,
            0,
            0,
            TimeSpan.Zero);

    private static readonly DateTimeOffset ExpiresAt =
        CreatedAt.AddMinutes(10);

    [Fact]
    public void Constructor_CalculatesTotalAmountFromSeats()
    {
        var reservation = CreateReservation();

        Assert.Equal(
            125.50m,
            reservation.TotalAmount);

        Assert.Equal(
            2,
            reservation.Seats.Count);

        Assert.Equal(
            ReservationStatus.Pending,
            reservation.Status);
    }

    [Fact]
    public void Constructor_WithoutSeats_ThrowsValidation()
    {
        Assert.Throws<DomainValidationException>(
            () => new Reservation(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                [],
                CreatedAt,
                ExpiresAt));
    }

    [Fact]
    public void Confirm_WhenReservationIsPending_ChangesStatus()
    {
        var reservation = CreateReservation();
        var confirmationTime = CreatedAt.AddMinutes(5);

        reservation.Confirm(confirmationTime);

        Assert.Equal(
            ReservationStatus.Confirmed,
            reservation.Status);

        Assert.Equal(
            confirmationTime,
            reservation.ConfirmedAtUtc);
    }

    [Fact]
    public void Confirm_WhenReservationHasExpired_ThrowsExpired()
    {
        var reservation = CreateReservation();

        Assert.Throws<ReservationExpiredException>(
            () => reservation.Confirm(ExpiresAt));
    }

    [Fact]
    public void Cancel_WhenReservationIsConfirmed_ThrowsInvalidState()
    {
        var reservation = CreateReservation();

        reservation.Confirm(
            CreatedAt.AddMinutes(5));

        Assert.Throws<InvalidReservationStateException>(
            () => reservation.Cancel(
                CreatedAt.AddMinutes(6)));
    }

    [Fact]
    public void Expire_WhenExpirationReached_ChangesStatusToExpired()
    {
        var reservation = CreateReservation();

        var result = reservation.Expire(ExpiresAt);

        Assert.True(result);

        Assert.Equal(
            ReservationStatus.Expired,
            reservation.Status);

        Assert.Equal(
            ExpiresAt,
            reservation.ExpiredAtUtc);
    }

    private static Reservation CreateReservation()
    {
        var reservationId = Guid.NewGuid();

        var seats = new[]
        {
            new ReservationSeat(
                Guid.NewGuid(),
                reservationId,
                Guid.NewGuid(),
                50.00m),

            new ReservationSeat(
                Guid.NewGuid(),
                reservationId,
                Guid.NewGuid(),
                75.50m)
        };

        return new Reservation(
            reservationId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            seats,
            CreatedAt,
            ExpiresAt);
    }
}
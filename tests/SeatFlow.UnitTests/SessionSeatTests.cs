using SeatFlow.Domain.Entities;
using SeatFlow.Domain.Enums;
using SeatFlow.Domain.Exceptions;

namespace SeatFlow.UnitTests;

public sealed class SessionSeatTests
{
    private static readonly DateTimeOffset CurrentTime =
        new(
            2026,
            8,
            1,
            12,
            0,
            0,
            TimeSpan.Zero);

    [Fact]
    public void Reserve_WhenSeatIsAvailable_ChangesStatusToReserved()
    {
        var sessionSeat = CreateSessionSeat();
        var reservationId = Guid.NewGuid();
        var expiration = CurrentTime.AddMinutes(10);

        sessionSeat.Reserve(
            reservationId,
            expiration,
            CurrentTime);

        Assert.Equal(
            SessionSeatStatus.Reserved,
            sessionSeat.Status);

        Assert.Equal(
            reservationId,
            sessionSeat.ReservationId);

        Assert.Equal(
            expiration,
            sessionSeat.ReservedUntilUtc);
    }

    [Fact]
    public void Reserve_WhenSeatIsAlreadyReserved_ThrowsUnavailable()
    {
        var sessionSeat = CreateSessionSeat();

        sessionSeat.Reserve(
            Guid.NewGuid(),
            CurrentTime.AddMinutes(10),
            CurrentTime);

        Assert.Throws<SeatUnavailableException>(
            () => sessionSeat.Reserve(
                Guid.NewGuid(),
                CurrentTime.AddMinutes(10),
                CurrentTime));
    }

    [Fact]
    public void Release_WhenReservationOwnsSeat_MakesSeatAvailable()
    {
        var sessionSeat = CreateSessionSeat();
        var reservationId = Guid.NewGuid();

        sessionSeat.Reserve(
            reservationId,
            CurrentTime.AddMinutes(10),
            CurrentTime);

        sessionSeat.Release(reservationId);

        Assert.Equal(
            SessionSeatStatus.Available,
            sessionSeat.Status);

        Assert.Null(sessionSeat.ReservationId);
        Assert.Null(sessionSeat.ReservedUntilUtc);
    }

    [Fact]
    public void ExpireReservation_BeforeExpiration_ReturnsFalse()
    {
        var sessionSeat = CreateSessionSeat();
        var reservationId = Guid.NewGuid();

        sessionSeat.Reserve(
            reservationId,
            CurrentTime.AddMinutes(10),
            CurrentTime);

        var result = sessionSeat.ExpireReservation(
            CurrentTime.AddMinutes(5));

        Assert.False(result);

        Assert.Equal(
            SessionSeatStatus.Reserved,
            sessionSeat.Status);
    }

    [Fact]
    public void ExpireReservation_WhenExpirationReached_ReleasesSeat()
    {
        var sessionSeat = CreateSessionSeat();
        var reservationId = Guid.NewGuid();
        var expiration = CurrentTime.AddMinutes(10);

        sessionSeat.Reserve(
            reservationId,
            expiration,
            CurrentTime);

        var result = sessionSeat.ExpireReservation(
            expiration);

        Assert.True(result);

        Assert.Equal(
            SessionSeatStatus.Available,
            sessionSeat.Status);

        Assert.Null(sessionSeat.ReservationId);
        Assert.Null(sessionSeat.ReservedUntilUtc);
    }

    [Fact]
    public void MarkAsSold_WhenReservationIsActive_ChangesStatusToSold()
    {
        var sessionSeat = CreateSessionSeat();
        var reservationId = Guid.NewGuid();

        sessionSeat.Reserve(
            reservationId,
            CurrentTime.AddMinutes(10),
            CurrentTime);

        sessionSeat.MarkAsSold(
            reservationId,
            CurrentTime.AddMinutes(5));

        Assert.Equal(
            SessionSeatStatus.Sold,
            sessionSeat.Status);

        Assert.Equal(
            reservationId,
            sessionSeat.ReservationId);

        Assert.Null(sessionSeat.ReservedUntilUtc);
    }

    private static SessionSeat CreateSessionSeat()
    {
        return new SessionSeat(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            75.50m);
    }
}
using SeatFlow.Domain.Entities;
using SeatFlow.Domain.Exceptions;

namespace SeatFlow.UnitTests;

public sealed class EventSessionTests
{
    private static readonly DateTimeOffset SessionStart =
        new(
            2026,
            8,
            10,
            18,
            0,
            0,
            TimeSpan.Zero);

    private static readonly DateTimeOffset BookingOpen =
        SessionStart.AddDays(-30);

    private static readonly DateTimeOffset BookingClose =
        SessionStart.AddHours(-1);

    [Fact]
    public void IsBookableAt_WhenTimeIsInsideWindow_ReturnsTrue()
    {
        var session = CreateSession();

        var result = session.IsBookableAt(
            BookingOpen.AddDays(5));

        Assert.True(result);
    }

    [Fact]
    public void IsBookableAt_WhenBookingHasClosed_ReturnsFalse()
    {
        var session = CreateSession();

        var result = session.IsBookableAt(BookingClose);

        Assert.False(result);
    }

    [Fact]
    public void Constructor_WhenBookingWindowIsInvalid_ThrowsValidation()
    {
        Assert.Throws<DomainValidationException>(
            () => new EventSession(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                SessionStart,
                BookingClose,
                BookingOpen));
    }

    [Fact]
    public void Cancel_WhenSessionIsActive_MarksSessionAsCancelled()
    {
        var session = CreateSession();
        var cancellationTime = SessionStart.AddDays(-2);

        session.Cancel(cancellationTime);

        Assert.True(session.IsCancelled);
        Assert.Equal(
            cancellationTime,
            session.CancelledAtUtc);
    }

    [Fact]
    public void Cancel_WhenSessionIsAlreadyCancelled_ThrowsConflict()
    {
        var session = CreateSession();
        var cancellationTime = SessionStart.AddDays(-2);

        session.Cancel(cancellationTime);

        Assert.Throws<DomainConflictException>(
            () => session.Cancel(cancellationTime));
    }

    private static EventSession CreateSession()
    {
        return new EventSession(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            SessionStart,
            BookingOpen,
            BookingClose);
    }
}
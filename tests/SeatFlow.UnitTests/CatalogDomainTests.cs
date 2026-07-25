using SeatFlow.Domain.Entities;
using SeatFlow.Domain.Enums;
using SeatFlow.Domain.Exceptions;

using DomainEvent = SeatFlow.Domain.Entities.Event;

namespace SeatFlow.UnitTests;

public sealed class CatalogDomainTests
{
    [Fact]
    public void SeatUpdate_WithValidData_UpdatesSeat()
    {
        var seat = new Seat(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "A",
            1,
            SeatCategory.Standard);

        seat.Update(
            " B ",
            5,
            SeatCategory.Premium);

        Assert.Equal("B", seat.RowLabel);
        Assert.Equal(5, seat.Number);

        Assert.Equal(
            SeatCategory.Premium,
            seat.Category);
    }

    [Fact]
    public void SeatUpdate_WithInvalidNumber_ThrowsValidation()
    {
        var seat = new Seat(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "A",
            1,
            SeatCategory.Standard);

        Assert.Throws<DomainValidationException>(
            () => seat.Update(
                "A",
                0,
                SeatCategory.Standard));
    }

    [Fact]
    public void EventUpdate_WithUndefinedCategory_ThrowsValidation()
    {
        var eventEntity = new DomainEvent(
            Guid.NewGuid(),
            "Demo event",
            null,
            EventCategory.Concert,
            12);

        Assert.Throws<DomainValidationException>(
            () => eventEntity.Update(
                "Demo event",
                null,
                (EventCategory)999,
                12));
    }

    [Fact]
    public void HallUpdate_WithInvalidCapacity_ThrowsValidation()
    {
        var hall = new Hall(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Main Hall",
            100);

        Assert.Throws<DomainValidationException>(
            () => hall.Update(
                "Main Hall",
                0));
    }
}
using Microsoft.EntityFrameworkCore;
using SeatFlow.Domain.Entities;
using SeatFlow.Domain.Enums;

using DomainEvent = SeatFlow.Domain.Entities.Event;

namespace SeatFlow.Infrastructure.Persistence;

internal static class SeatFlowSeedData
{
    private static readonly Guid VenueId =
        Guid.Parse(
            "11111111-1111-1111-1111-111111111111");

    private static readonly Guid HallId =
        Guid.Parse(
            "22222222-2222-2222-2222-222222222222");

    private static readonly Guid EventId =
        Guid.Parse(
            "33333333-3333-3333-3333-333333333333");

    private static readonly Guid EventSessionId =
        Guid.Parse(
            "44444444-4444-4444-4444-444444444444");

    private static readonly DateTimeOffset SessionStartsAtUtc =
        new(
            2026,
            12,
            15,
            19,
            0,
            0,
            TimeSpan.Zero);

    public static void Apply(ModelBuilder modelBuilder)
    {
        SeedVenue(modelBuilder);
        SeedHall(modelBuilder);
        SeedSeats(modelBuilder);
        SeedEvent(modelBuilder);
        SeedEventSession(modelBuilder);
        SeedSessionSeats(modelBuilder);
    }

    private static void SeedVenue(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Venue>().HasData(
            new
            {
                Id = VenueId,
                Name = "SeatFlow Arena",
                Address = "100 Demo Avenue",
                Description =
                    "Demonstration venue for the SeatFlow API."
            });
    }

    private static void SeedHall(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Hall>().HasData(
            new
            {
                Id = HallId,
                VenueId,
                Name = "Main Hall",
                Capacity = 12
            });
    }

    private static void SeedSeats(ModelBuilder modelBuilder)
    {
        var firstRowSeats = Enumerable
            .Range(1, 6)
            .Select(
                number => new
                {
                    Id = CreateSeatId(number),
                    HallId,
                    RowLabel = "A",
                    Number = number,
                    Category = SeatCategory.Premium
                });

        var secondRowSeats = Enumerable
            .Range(1, 6)
            .Select(
                number => new
                {
                    Id = CreateSeatId(number + 6),
                    HallId,
                    RowLabel = "B",
                    Number = number,
                    Category = SeatCategory.Standard
                });

        modelBuilder.Entity<Seat>().HasData(
            firstRowSeats
                .Concat(secondRowSeats)
                .ToArray());
    }

    private static void SeedEvent(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DomainEvent>().HasData(
            new
            {
                Id = EventId,
                Title = "SeatFlow Demo Concert",
                Description =
                    "Demonstration event used for local development.",
                Category = EventCategory.Concert,
                AgeRestriction = 12
            });
    }

    private static void SeedEventSession(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EventSession>().HasData(
            new
            {
                Id = EventSessionId,
                EventId,
                HallId,
                StartsAtUtc = SessionStartsAtUtc,
                BookingOpensAtUtc =
                    new DateTimeOffset(
                        2026,
                        7,
                        1,
                        0,
                        0,
                        0,
                        TimeSpan.Zero),
                BookingClosesAtUtc =
                    SessionStartsAtUtc.AddHours(-1),
                IsCancelled = false,
                CancelledAtUtc =
                    (DateTimeOffset?)null
            });
    }

    private static void SeedSessionSeats(
        ModelBuilder modelBuilder)
    {
        var sessionSeats = Enumerable
            .Range(1, 12)
            .Select(
                number => new
                {
                    Id = CreateSessionSeatId(number),
                    EventSessionId,
                    SeatId = CreateSeatId(number),
                    Price = number <= 6
                        ? 80.00m
                        : 50.00m,
                    Status = SessionSeatStatus.Available,
                    ReservationId = (Guid?)null,
                    ReservedUntilUtc =
                        (DateTimeOffset?)null
                })
            .ToArray();

        modelBuilder.Entity<SessionSeat>()
            .HasData(sessionSeats);
    }

    private static Guid CreateSeatId(int number)
    {
        return Guid.Parse(
            $"50000000-0000-0000-0000-{number:D12}");
    }

    private static Guid CreateSessionSeatId(int number)
    {
        return Guid.Parse(
            $"60000000-0000-0000-0000-{number:D12}");
    }
}
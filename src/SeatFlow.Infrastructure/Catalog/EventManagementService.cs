using Microsoft.EntityFrameworkCore;
using Npgsql;
using SeatFlow.Application.Catalog;
using SeatFlow.Application.Common;
using SeatFlow.Domain.Entities;
using SeatFlow.Domain.Enums;
using SeatFlow.Domain.Exceptions;
using SeatFlow.Infrastructure.Persistence;

using DomainEvent = SeatFlow.Domain.Entities.Event;

namespace SeatFlow.Infrastructure.Catalog;

public sealed class EventManagementService
    : IEventManagementService
{
    private readonly SeatFlowDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public EventManagementService(
        SeatFlowDbContext dbContext,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<VenueDetails> CreateVenueAsync(
        string name,
        string address,
        string? description,
        CancellationToken cancellationToken)
    {
        var venue = new Venue(
            Guid.NewGuid(),
            name,
            address,
            description);

        _dbContext.Venues.Add(venue);

        await SaveChangesAsync(
            "A venue with the same data already exists.",
            cancellationToken);

        return MapVenue(venue);
    }

    public async Task<VenueDetails> GetVenueAsync(
        Guid venueId,
        CancellationToken cancellationToken)
    {
        var venue = await _dbContext.Venues
            .AsNoTracking()
            .SingleOrDefaultAsync(
                currentVenue =>
                    currentVenue.Id == venueId,
                cancellationToken)
            ?? throw NotFound(
                nameof(Venue),
                venueId);

        return MapVenue(venue);
    }

    public async Task<VenueDetails> UpdateVenueAsync(
        Guid venueId,
        string name,
        string address,
        string? description,
        CancellationToken cancellationToken)
    {
        var venue = await _dbContext.Venues
            .SingleOrDefaultAsync(
                currentVenue =>
                    currentVenue.Id == venueId,
                cancellationToken)
            ?? throw NotFound(
                nameof(Venue),
                venueId);

        venue.Update(
            name,
            address,
            description);

        await SaveChangesAsync(
            "The venue could not be updated.",
            cancellationToken);

        return MapVenue(venue);
    }

    public async Task DeleteVenueAsync(
        Guid venueId,
        CancellationToken cancellationToken)
    {
        var venue = await _dbContext.Venues
            .SingleOrDefaultAsync(
                currentVenue =>
                    currentVenue.Id == venueId,
                cancellationToken)
            ?? throw NotFound(
                nameof(Venue),
                venueId);

        var containsHalls =
            await _dbContext.Halls.AnyAsync(
                hall => hall.VenueId == venueId,
                cancellationToken);

        if (containsHalls)
        {
            throw new DomainConflictException(
                "A venue containing halls cannot be deleted.");
        }

        _dbContext.Venues.Remove(venue);

        await SaveChangesAsync(
            "The venue cannot be deleted because it is in use.",
            cancellationToken);
    }

    public async Task<HallDetails> CreateHallAsync(
        Guid venueId,
        string name,
        int capacity,
        CancellationToken cancellationToken)
    {
        await EnsureVenueExistsAsync(
            venueId,
            cancellationToken);

        var hall = new Hall(
            Guid.NewGuid(),
            venueId,
            name,
            capacity);

        _dbContext.Halls.Add(hall);

        await SaveChangesAsync(
            "A hall with the same name already exists " +
            "at this venue.",
            cancellationToken);

        return MapHall(hall);
    }

    public async Task<HallDetails> GetHallAsync(
        Guid hallId,
        CancellationToken cancellationToken)
    {
        var hall = await _dbContext.Halls
            .AsNoTracking()
            .SingleOrDefaultAsync(
                currentHall =>
                    currentHall.Id == hallId,
                cancellationToken)
            ?? throw NotFound(
                nameof(Hall),
                hallId);

        return MapHall(hall);
    }

    public async Task<HallDetails> UpdateHallAsync(
        Guid hallId,
        string name,
        int capacity,
        CancellationToken cancellationToken)
    {
        var hall = await _dbContext.Halls
            .SingleOrDefaultAsync(
                currentHall =>
                    currentHall.Id == hallId,
                cancellationToken)
            ?? throw NotFound(
                nameof(Hall),
                hallId);

        var currentSeatCount =
            await _dbContext.Seats.CountAsync(
                seat => seat.HallId == hallId,
                cancellationToken);

        if (capacity < currentSeatCount)
        {
            throw new DomainConflictException(
                $"Hall capacity cannot be lower than its " +
                $"current seat count ({currentSeatCount}).");
        }

        hall.Update(
            name,
            capacity);

        await SaveChangesAsync(
            "A hall with the same name already exists " +
            "at this venue.",
            cancellationToken);

        return MapHall(hall);
    }

    public async Task DeleteHallAsync(
        Guid hallId,
        CancellationToken cancellationToken)
    {
        var hall = await _dbContext.Halls
            .SingleOrDefaultAsync(
                currentHall =>
                    currentHall.Id == hallId,
                cancellationToken)
            ?? throw NotFound(
                nameof(Hall),
                hallId);

        var containsSessions =
            await _dbContext.EventSessions.AnyAsync(
                session => session.HallId == hallId,
                cancellationToken);

        if (containsSessions)
        {
            throw new DomainConflictException(
                "A hall containing event sessions " +
                "cannot be deleted.");
        }

        _dbContext.Halls.Remove(hall);

        await SaveChangesAsync(
            "The hall cannot be deleted because it is in use.",
            cancellationToken);
    }

    public async Task<SeatDetails> CreateSeatAsync(
        Guid hallId,
        string rowLabel,
        int number,
        SeatCategory category,
        CancellationToken cancellationToken)
    {
        var hall = await _dbContext.Halls
            .AsNoTracking()
            .SingleOrDefaultAsync(
                currentHall =>
                    currentHall.Id == hallId,
                cancellationToken)
            ?? throw NotFound(
                nameof(Hall),
                hallId);

        var seat = new Seat(
            Guid.NewGuid(),
            hallId,
            rowLabel,
            number,
            category);

        var currentSeatCount =
            await _dbContext.Seats.CountAsync(
                currentSeat =>
                    currentSeat.HallId == hallId,
                cancellationToken);

        if (currentSeatCount >= hall.Capacity)
        {
            throw new DomainConflictException(
                $"Hall capacity of {hall.Capacity} seats " +
                "has already been reached.");
        }

        var duplicateExists =
            await _dbContext.Seats.AnyAsync(
                currentSeat =>
                    currentSeat.HallId == hallId &&
                    currentSeat.RowLabel ==
                        seat.RowLabel &&
                    currentSeat.Number ==
                        seat.Number,
                cancellationToken);

        if (duplicateExists)
        {
            throw new DomainConflictException(
                $"Seat {seat.RowLabel}-{seat.Number} " +
                "already exists in the hall.");
        }

        _dbContext.Seats.Add(seat);

        await SaveChangesAsync(
            "A seat with the same row and number " +
            "already exists in the hall.",
            cancellationToken);

        return MapSeat(seat);
    }

    public async Task<SeatDetails> GetSeatAsync(
        Guid seatId,
        CancellationToken cancellationToken)
    {
        var seat = await _dbContext.Seats
            .AsNoTracking()
            .SingleOrDefaultAsync(
                currentSeat =>
                    currentSeat.Id == seatId,
                cancellationToken)
            ?? throw NotFound(
                nameof(Seat),
                seatId);

        return MapSeat(seat);
    }

    public async Task<SeatDetails> UpdateSeatAsync(
        Guid seatId,
        string rowLabel,
        int number,
        SeatCategory category,
        CancellationToken cancellationToken)
    {
        var seat = await _dbContext.Seats
            .SingleOrDefaultAsync(
                currentSeat =>
                    currentSeat.Id == seatId,
                cancellationToken)
            ?? throw NotFound(
                nameof(Seat),
                seatId);

        seat.Update(
            rowLabel,
            number,
            category);

        var duplicateExists =
            await _dbContext.Seats.AnyAsync(
                currentSeat =>
                    currentSeat.Id != seatId &&
                    currentSeat.HallId ==
                        seat.HallId &&
                    currentSeat.RowLabel ==
                        seat.RowLabel &&
                    currentSeat.Number ==
                        seat.Number,
                cancellationToken);

        if (duplicateExists)
        {
            throw new DomainConflictException(
                $"Seat {seat.RowLabel}-{seat.Number} " +
                "already exists in the hall.");
        }

        await SaveChangesAsync(
            "A seat with the same row and number " +
            "already exists in the hall.",
            cancellationToken);

        return MapSeat(seat);
    }

    public async Task DeleteSeatAsync(
        Guid seatId,
        CancellationToken cancellationToken)
    {
        var seat = await _dbContext.Seats
            .SingleOrDefaultAsync(
                currentSeat =>
                    currentSeat.Id == seatId,
                cancellationToken)
            ?? throw NotFound(
                nameof(Seat),
                seatId);

        var usedBySession =
            await _dbContext.SessionSeats.AnyAsync(
                sessionSeat =>
                    sessionSeat.SeatId == seatId,
                cancellationToken);

        if (usedBySession)
        {
            throw new DomainConflictException(
                "A seat already used by an event session " +
                "cannot be deleted.");
        }

        _dbContext.Seats.Remove(seat);

        await SaveChangesAsync(
            "The seat cannot be deleted because it is in use.",
            cancellationToken);
    }

    public async Task<EventDetails> CreateEventAsync(
        string title,
        string? description,
        EventCategory category,
        int ageRestriction,
        CancellationToken cancellationToken)
    {
        var eventEntity = new DomainEvent(
            Guid.NewGuid(),
            title,
            description,
            category,
            ageRestriction);

        _dbContext.Events.Add(eventEntity);

        await SaveChangesAsync(
            "The event could not be created.",
            cancellationToken);

        return MapEvent(eventEntity);
    }

    public async Task<EventDetails> GetEventAsync(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var eventEntity = await _dbContext.Events
            .AsNoTracking()
            .SingleOrDefaultAsync(
                currentEvent =>
                    currentEvent.Id == eventId,
                cancellationToken)
            ?? throw NotFound(
                nameof(DomainEvent),
                eventId);

        return MapEvent(eventEntity);
    }

    public async Task<EventDetails> UpdateEventAsync(
        Guid eventId,
        string title,
        string? description,
        EventCategory category,
        int ageRestriction,
        CancellationToken cancellationToken)
    {
        var eventEntity = await _dbContext.Events
            .SingleOrDefaultAsync(
                currentEvent =>
                    currentEvent.Id == eventId,
                cancellationToken)
            ?? throw NotFound(
                nameof(DomainEvent),
                eventId);

        eventEntity.Update(
            title,
            description,
            category,
            ageRestriction);

        await SaveChangesAsync(
            "The event could not be updated.",
            cancellationToken);

        return MapEvent(eventEntity);
    }

    public async Task DeleteEventAsync(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var eventEntity = await _dbContext.Events
            .SingleOrDefaultAsync(
                currentEvent =>
                    currentEvent.Id == eventId,
                cancellationToken)
            ?? throw NotFound(
                nameof(DomainEvent),
                eventId);

        var containsSessions =
            await _dbContext.EventSessions.AnyAsync(
                session =>
                    session.EventId == eventId,
                cancellationToken);

        if (containsSessions)
        {
            throw new DomainConflictException(
                "An event containing sessions cannot be deleted.");
        }

        _dbContext.Events.Remove(eventEntity);

        await SaveChangesAsync(
            "The event cannot be deleted because it is in use.",
            cancellationToken);
    }

    public async Task<EventSessionDetails> CreateSessionAsync(
        Guid eventId,
        Guid hallId,
        DateTimeOffset startsAtUtc,
        DateTimeOffset bookingOpensAtUtc,
        DateTimeOffset bookingClosesAtUtc,
        decimal defaultPrice,
        CancellationToken cancellationToken)
    {
        await EnsureEventExistsAsync(
            eventId,
            cancellationToken);

        await EnsureHallExistsAsync(
            hallId,
            cancellationToken);

        var currentTimeUtc =
            _timeProvider.GetUtcNow();

        if (startsAtUtc.ToUniversalTime() <=
            currentTimeUtc)
        {
            throw new DomainValidationException(
                "The event session must start in the future.");
        }

        var hallSeats = await _dbContext.Seats
            .AsNoTracking()
            .Where(seat => seat.HallId == hallId)
            .OrderBy(seat => seat.RowLabel)
            .ThenBy(seat => seat.Number)
            .ToListAsync(cancellationToken);

        if (hallSeats.Count == 0)
        {
            throw new DomainConflictException(
                "The hall must contain at least one seat " +
                "before an event session can be created.");
        }

        var session = new EventSession(
            Guid.NewGuid(),
            eventId,
            hallId,
            startsAtUtc,
            bookingOpensAtUtc,
            bookingClosesAtUtc);

        var sessionSeats = hallSeats
            .Select(
                seat => new SessionSeat(
                    Guid.NewGuid(),
                    session.Id,
                    seat.Id,
                    defaultPrice))
            .ToList();

        _dbContext.EventSessions.Add(session);

        _dbContext.SessionSeats.AddRange(
            sessionSeats);

        await SaveChangesAsync(
            "The event session could not be created.",
            cancellationToken);

        return new EventSessionDetails(
            session.Id,
            session.EventId,
            session.HallId,
            session.StartsAtUtc,
            session.BookingOpensAtUtc,
            session.BookingClosesAtUtc,
            session.IsCancelled,
            session.CancelledAtUtc,
            sessionSeats.Count,
            defaultPrice,
            defaultPrice);
    }

    public async Task<EventSessionDetails> GetSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var session = await _dbContext.EventSessions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                currentSession =>
                    currentSession.Id == sessionId,
                cancellationToken)
            ?? throw NotFound(
                nameof(EventSession),
                sessionId);

        return await MapSessionAsync(
            session,
            cancellationToken);
    }

    public async Task<EventSessionDetails> UpdateSessionAsync(
        Guid sessionId,
        DateTimeOffset startsAtUtc,
        DateTimeOffset bookingOpensAtUtc,
        DateTimeOffset bookingClosesAtUtc,
        CancellationToken cancellationToken)
    {
        var session = await _dbContext.EventSessions
            .SingleOrDefaultAsync(
                currentSession =>
                    currentSession.Id == sessionId,
                cancellationToken)
            ?? throw NotFound(
                nameof(EventSession),
                sessionId);

        var containsReservations =
            await _dbContext.Reservations.AnyAsync(
                reservation =>
                    reservation.EventSessionId ==
                        sessionId,
                cancellationToken);

        if (containsReservations)
        {
            throw new DomainConflictException(
                "A session containing reservations " +
                "cannot be rescheduled.");
        }

        var currentTimeUtc =
            _timeProvider.GetUtcNow();

        if (startsAtUtc.ToUniversalTime() <=
            currentTimeUtc)
        {
            throw new DomainValidationException(
                "The event session must start in the future.");
        }

        session.Schedule(
            startsAtUtc,
            bookingOpensAtUtc,
            bookingClosesAtUtc);

        await SaveChangesAsync(
            "The event session could not be updated.",
            cancellationToken);

        return await MapSessionAsync(
            session,
            cancellationToken);
    }

    public async Task<EventSessionDetails> CancelSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var session = await _dbContext.EventSessions
            .SingleOrDefaultAsync(
                currentSession =>
                    currentSession.Id == sessionId,
                cancellationToken)
            ?? throw NotFound(
                nameof(EventSession),
                sessionId);

        session.Cancel(
            _timeProvider.GetUtcNow());

        await SaveChangesAsync(
            "The event session could not be cancelled.",
            cancellationToken);

        return await MapSessionAsync(
            session,
            cancellationToken);
    }

    public async Task DeleteSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var session = await _dbContext.EventSessions
            .SingleOrDefaultAsync(
                currentSession =>
                    currentSession.Id == sessionId,
                cancellationToken)
            ?? throw NotFound(
                nameof(EventSession),
                sessionId);

        var containsReservations =
            await _dbContext.Reservations.AnyAsync(
                reservation =>
                    reservation.EventSessionId ==
                        sessionId,
                cancellationToken);

        if (containsReservations)
        {
            throw new DomainConflictException(
                "A session containing reservations " +
                "cannot be deleted.");
        }

        _dbContext.EventSessions.Remove(session);

        await SaveChangesAsync(
            "The event session cannot be deleted " +
            "because it is in use.",
            cancellationToken);
    }

    private async Task EnsureVenueExistsAsync(
        Guid venueId,
        CancellationToken cancellationToken)
    {
        var exists =
            await _dbContext.Venues.AnyAsync(
                venue => venue.Id == venueId,
                cancellationToken);

        if (!exists)
        {
            throw NotFound(
                nameof(Venue),
                venueId);
        }
    }

    private async Task EnsureHallExistsAsync(
        Guid hallId,
        CancellationToken cancellationToken)
    {
        var exists =
            await _dbContext.Halls.AnyAsync(
                hall => hall.Id == hallId,
                cancellationToken);

        if (!exists)
        {
            throw NotFound(
                nameof(Hall),
                hallId);
        }
    }

    private async Task EnsureEventExistsAsync(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var exists =
            await _dbContext.Events.AnyAsync(
                eventEntity =>
                    eventEntity.Id == eventId,
                cancellationToken);

        if (!exists)
        {
            throw NotFound(
                nameof(DomainEvent),
                eventId);
        }
    }

    private async Task<EventSessionDetails> MapSessionAsync(
        EventSession session,
        CancellationToken cancellationToken)
    {
        var prices = await _dbContext.SessionSeats
            .AsNoTracking()
            .Where(
                sessionSeat =>
                    sessionSeat.EventSessionId ==
                        session.Id)
            .Select(sessionSeat => sessionSeat.Price)
            .ToListAsync(cancellationToken);

        return new EventSessionDetails(
            session.Id,
            session.EventId,
            session.HallId,
            session.StartsAtUtc,
            session.BookingOpensAtUtc,
            session.BookingClosesAtUtc,
            session.IsCancelled,
            session.CancelledAtUtc,
            prices.Count,
            prices.Count == 0
                ? null
                : prices.Min(),
            prices.Count == 0
                ? null
                : prices.Max());
    }

    private async Task SaveChangesAsync(
        string conflictMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(
                cancellationToken);
        }
        catch (DbUpdateException exception)
            when (IsDatabaseConflict(exception))
        {
            _dbContext.ChangeTracker.Clear();

            throw new DomainConflictException(
                conflictMessage);
        }
    }

    private static bool IsDatabaseConflict(
        DbUpdateException exception)
    {
        return exception.InnerException
            is PostgresException postgresException &&
            postgresException.SqlState
                is PostgresErrorCodes.UniqueViolation
                or PostgresErrorCodes.ForeignKeyViolation
                or PostgresErrorCodes.CheckViolation;
    }

    private static ResourceNotFoundException NotFound(
        string resourceName,
        Guid resourceId)
    {
        return new ResourceNotFoundException(
            resourceName,
            resourceId);
    }

    private static VenueDetails MapVenue(
        Venue venue)
    {
        return new VenueDetails(
            venue.Id,
            venue.Name,
            venue.Address,
            venue.Description);
    }

    private static HallDetails MapHall(
        Hall hall)
    {
        return new HallDetails(
            hall.Id,
            hall.VenueId,
            hall.Name,
            hall.Capacity);
    }

    private static SeatDetails MapSeat(
        Seat seat)
    {
        return new SeatDetails(
            seat.Id,
            seat.HallId,
            seat.RowLabel,
            seat.Number,
            seat.Category);
    }

    private static EventDetails MapEvent(
        DomainEvent eventEntity)
    {
        return new EventDetails(
            eventEntity.Id,
            eventEntity.Title,
            eventEntity.Description,
            eventEntity.Category,
            eventEntity.AgeRestriction);
    }
}
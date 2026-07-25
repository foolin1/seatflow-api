using Microsoft.EntityFrameworkCore;
using SeatFlow.Application.Catalog;
using SeatFlow.Application.Common;
using SeatFlow.Domain.Entities;
using SeatFlow.Domain.Enums;
using SeatFlow.Infrastructure.Persistence;

using DomainEvent = SeatFlow.Domain.Entities.Event;

namespace SeatFlow.Infrastructure.Catalog;

public sealed class EventCatalogService
    : IEventCatalogService
{
    private readonly SeatFlowDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public EventCatalogService(
        SeatFlowDbContext dbContext,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<PagedResult<EventCatalogItem>>
        GetEventsAsync(
            EventCatalogQuery query,
            CancellationToken cancellationToken)
    {
        var validatedQuery =
            EventCatalogQueryValidator.ValidateAndNormalize(
                query);

        var currentTimeUtc =
            _timeProvider.GetUtcNow();

        var eventsQuery =
            _dbContext.Events
                .AsNoTracking();

        if (validatedQuery.Search is not null)
        {
            var searchPattern =
                $"%{validatedQuery.Search}%";

            eventsQuery =
                eventsQuery.Where(
                    eventEntity =>
                        EF.Functions.ILike(
                            eventEntity.Title,
                            searchPattern) ||
                        (
                            eventEntity.Description != null &&
                            EF.Functions.ILike(
                                eventEntity.Description,
                                searchPattern)
                        ));
        }

        if (validatedQuery.Category.HasValue)
        {
            var category =
                validatedQuery.Category.Value;

            eventsQuery =
                eventsQuery.Where(
                    eventEntity =>
                        eventEntity.Category == category);
        }

        var events =
            await eventsQuery
                .Select(
                    eventEntity =>
                        new EventBaseProjection(
                            eventEntity.Id,
                            eventEntity.Title,
                            eventEntity.Description,
                            eventEntity.Category,
                            eventEntity.AgeRestriction))
                .ToListAsync(cancellationToken);

        if (events.Count == 0)
        {
            return CreateEmptyEventResult(
                validatedQuery);
        }

        var eventIds =
            events
                .Select(
                    eventEntity =>
                        eventEntity.Id)
                .ToList();

        var sessionsQuery =
            _dbContext.EventSessions
                .AsNoTracking()
                .Where(
                    session =>
                        eventIds.Contains(
                            session.EventId) &&
                        !session.IsCancelled &&
                        session.StartsAtUtc >=
                            currentTimeUtc);

        if (validatedQuery.StartsFromUtc.HasValue)
        {
            var startsFromUtc =
                validatedQuery.StartsFromUtc.Value;

            sessionsQuery =
                sessionsQuery.Where(
                    session =>
                        session.StartsAtUtc >=
                            startsFromUtc);
        }

        if (validatedQuery.StartsToUtc.HasValue)
        {
            var startsToUtc =
                validatedQuery.StartsToUtc.Value;

            sessionsQuery =
                sessionsQuery.Where(
                    session =>
                        session.StartsAtUtc <=
                            startsToUtc);
        }

        if (validatedQuery.VenueId.HasValue)
        {
            var venueId =
                validatedQuery.VenueId.Value;

            var venueHallIds =
                _dbContext.Halls
                    .AsNoTracking()
                    .Where(
                        hall =>
                            hall.VenueId == venueId)
                    .Select(
                        hall =>
                            hall.Id);

            sessionsQuery =
                sessionsQuery.Where(
                    session =>
                        venueHallIds.Contains(
                            session.HallId));
        }

        var sessions =
            await sessionsQuery
                .Select(
                    session =>
                        new EligibleSessionProjection(
                            session.Id,
                            session.EventId,
                            session.StartsAtUtc))
                .ToListAsync(cancellationToken);

        if (sessions.Count == 0)
        {
            return CreateEmptyEventResult(
                validatedQuery);
        }

        var sessionIds =
            sessions
                .Select(
                    session =>
                        session.Id)
                .ToList();

        var sessionSeats =
            await _dbContext.SessionSeats
                .AsNoTracking()
                .Where(
                    sessionSeat =>
                        sessionIds.Contains(
                            sessionSeat.EventSessionId))
                .Select(
                    sessionSeat =>
                        new SessionSeatProjection(
                            sessionSeat.EventSessionId,
                            sessionSeat.Price,
                            sessionSeat.Status))
                .ToListAsync(cancellationToken);

        var sessionsByEvent =
            sessions
                .GroupBy(
                    session =>
                        session.EventId)
                .ToDictionary(
                    group =>
                        group.Key,
                    group =>
                        group.ToList());

        var seatsBySession =
            sessionSeats
                .GroupBy(
                    sessionSeat =>
                        sessionSeat.EventSessionId)
                .ToDictionary(
                    group =>
                        group.Key,
                    group =>
                        group.ToList());

        var catalogItems =
            new List<EventCatalogItem>();

        foreach (var eventEntity in events)
        {
            if (!sessionsByEvent.TryGetValue(
                    eventEntity.Id,
                    out var eventSessions))
            {
                continue;
            }

            var prices =
                eventSessions
                    .Where(
                        session =>
                            seatsBySession.ContainsKey(
                                session.Id))
                    .SelectMany(
                        session =>
                            seatsBySession[session.Id])
                    .Select(
                        sessionSeat =>
                            sessionSeat.Price)
                    .ToList();

            decimal? minimumPrice =
                prices.Count == 0
                    ? null
                    : prices.Min();

            decimal? maximumPrice =
                prices.Count == 0
                    ? null
                    : prices.Max();

            catalogItems.Add(
                new EventCatalogItem(
                    eventEntity.Id,
                    eventEntity.Title,
                    eventEntity.Description,
                    eventEntity.Category,
                    eventEntity.AgeRestriction,
                    eventSessions.Min(
                        session =>
                            session.StartsAtUtc),
                    eventSessions.Count,
                    minimumPrice,
                    maximumPrice));
        }

        if (validatedQuery.MinPrice.HasValue)
        {
            var minPrice =
                validatedQuery.MinPrice.Value;

            catalogItems =
                catalogItems
                    .Where(
                        item =>
                            item.MinimumPrice.HasValue &&
                            item.MinimumPrice.Value >=
                                minPrice)
                    .ToList();
        }

        if (validatedQuery.MaxPrice.HasValue)
        {
            var maxPrice =
                validatedQuery.MaxPrice.Value;

            catalogItems =
                catalogItems
                    .Where(
                        item =>
                            item.MinimumPrice.HasValue &&
                            item.MinimumPrice.Value <=
                                maxPrice)
                    .ToList();
        }

        var totalCount =
            catalogItems.Count;

        var orderedItems =
            ApplyEventSorting(
                catalogItems,
                validatedQuery.SortBy,
                validatedQuery.SortDirection);

        var skip =
            (validatedQuery.Page - 1) *
            validatedQuery.PageSize;

        var pageItems =
            orderedItems
                .Skip(skip)
                .Take(validatedQuery.PageSize)
                .ToList();

        return new PagedResult<EventCatalogItem>(
            pageItems,
            validatedQuery.Page,
            validatedQuery.PageSize,
            totalCount);
    }

    public async Task<EventCatalogDetails>
        GetEventAsync(
            Guid eventId,
            CancellationToken cancellationToken)
    {
        if (eventId == Guid.Empty)
        {
            throw new ResourceNotFoundException(
                nameof(DomainEvent),
                eventId);
        }

        var eventEntity =
            await _dbContext.Events
                .AsNoTracking()
                .Where(
                    currentEvent =>
                        currentEvent.Id == eventId)
                .Select(
                    currentEvent =>
                        new EventBaseProjection(
                            currentEvent.Id,
                            currentEvent.Title,
                            currentEvent.Description,
                            currentEvent.Category,
                            currentEvent.AgeRestriction))
                .SingleOrDefaultAsync(
                    cancellationToken)
            ?? throw new ResourceNotFoundException(
                nameof(DomainEvent),
                eventId);

        var currentTimeUtc =
            _timeProvider.GetUtcNow();

        var upcomingSessions =
            await _dbContext.EventSessions
                .AsNoTracking()
                .Where(
                    session =>
                        session.EventId == eventId &&
                        !session.IsCancelled &&
                        session.StartsAtUtc >=
                            currentTimeUtc)
                .Select(
                    session =>
                        new EligibleSessionProjection(
                            session.Id,
                            session.EventId,
                            session.StartsAtUtc))
                .ToListAsync(cancellationToken);

        if (upcomingSessions.Count == 0)
        {
            return new EventCatalogDetails(
                eventEntity.Id,
                eventEntity.Title,
                eventEntity.Description,
                eventEntity.Category,
                eventEntity.AgeRestriction,
                null,
                0,
                null,
                null);
        }

        var sessionIds =
            upcomingSessions
                .Select(
                    session =>
                        session.Id)
                .ToList();

        var prices =
            await _dbContext.SessionSeats
                .AsNoTracking()
                .Where(
                    sessionSeat =>
                        sessionIds.Contains(
                            sessionSeat.EventSessionId))
                .Select(
                    sessionSeat =>
                        sessionSeat.Price)
                .ToListAsync(cancellationToken);

        decimal? minimumPrice =
            prices.Count == 0
                ? null
                : prices.Min();

        decimal? maximumPrice =
            prices.Count == 0
                ? null
                : prices.Max();

        return new EventCatalogDetails(
            eventEntity.Id,
            eventEntity.Title,
            eventEntity.Description,
            eventEntity.Category,
            eventEntity.AgeRestriction,
            upcomingSessions.Min(
                session =>
                    session.StartsAtUtc),
            upcomingSessions.Count,
            minimumPrice,
            maximumPrice);
    }

    public async Task<
        PagedResult<EventSessionCatalogItem>>
        GetEventSessionsAsync(
            Guid eventId,
            EventSessionCatalogQuery query,
            CancellationToken cancellationToken)
    {
        var validatedQuery =
            EventCatalogQueryValidator.ValidateAndNormalize(
                query);

        var eventExists =
            await _dbContext.Events
                .AsNoTracking()
                .AnyAsync(
                    eventEntity =>
                        eventEntity.Id == eventId,
                    cancellationToken);

        if (!eventExists)
        {
            throw new ResourceNotFoundException(
                nameof(DomainEvent),
                eventId);
        }

        var currentTimeUtc =
            _timeProvider.GetUtcNow();

        var eventSessionsQuery =
            _dbContext.EventSessions
                .AsNoTracking()
                .Where(
                    session =>
                        session.EventId == eventId &&
                        !session.IsCancelled &&
                        session.StartsAtUtc >=
                            currentTimeUtc);

        if (validatedQuery.StartsFromUtc.HasValue)
        {
            var startsFromUtc =
                validatedQuery.StartsFromUtc.Value;

            eventSessionsQuery =
                eventSessionsQuery.Where(
                    session =>
                        session.StartsAtUtc >=
                            startsFromUtc);
        }

        if (validatedQuery.StartsToUtc.HasValue)
        {
            var startsToUtc =
                validatedQuery.StartsToUtc.Value;

            eventSessionsQuery =
                eventSessionsQuery.Where(
                    session =>
                        session.StartsAtUtc <=
                            startsToUtc);
        }

        if (validatedQuery.VenueId.HasValue)
        {
            var venueId =
                validatedQuery.VenueId.Value;

            var venueHallIds =
                _dbContext.Halls
                    .AsNoTracking()
                    .Where(
                        hall =>
                            hall.VenueId == venueId)
                    .Select(
                        hall =>
                            hall.Id);

            eventSessionsQuery =
                eventSessionsQuery.Where(
                    session =>
                        venueHallIds.Contains(
                            session.HallId));
        }

        var sessions =
            await (
                from session in eventSessionsQuery
                join eventEntity in
                    _dbContext.Events.AsNoTracking()
                    on session.EventId equals
                        eventEntity.Id
                join hall in
                    _dbContext.Halls.AsNoTracking()
                    on session.HallId equals
                        hall.Id
                join venue in
                    _dbContext.Venues.AsNoTracking()
                    on hall.VenueId equals
                        venue.Id
                select new SessionCatalogBaseProjection(
                    session.Id,
                    eventEntity.Id,
                    eventEntity.Title,
                    venue.Id,
                    venue.Name,
                    venue.Address,
                    hall.Id,
                    hall.Name,
                    session.StartsAtUtc,
                    session.BookingOpensAtUtc,
                    session.BookingClosesAtUtc)
            ).ToListAsync(cancellationToken);

        if (sessions.Count == 0)
        {
            return new PagedResult<
                EventSessionCatalogItem>(
                    Array.Empty<
                        EventSessionCatalogItem>(),
                    validatedQuery.Page,
                    validatedQuery.PageSize,
                    0);
        }

        var sessionIds =
            sessions
                .Select(
                    session =>
                        session.Id)
                .ToList();

        var sessionSeats =
            await _dbContext.SessionSeats
                .AsNoTracking()
                .Where(
                    sessionSeat =>
                        sessionIds.Contains(
                            sessionSeat.EventSessionId))
                .Select(
                    sessionSeat =>
                        new SessionSeatProjection(
                            sessionSeat.EventSessionId,
                            sessionSeat.Price,
                            sessionSeat.Status))
                .ToListAsync(cancellationToken);

        var seatsBySession =
            sessionSeats
                .GroupBy(
                    sessionSeat =>
                        sessionSeat.EventSessionId)
                .ToDictionary(
                    group =>
                        group.Key,
                    group =>
                        group.ToList());

        var sessionItems =
            sessions
                .Select(
                    session =>
                    {
                        seatsBySession.TryGetValue(
                            session.Id,
                            out var seats);

                        seats ??=
                            new List<
                                SessionSeatProjection>();

                        decimal? minimumPrice =
                            seats.Count == 0
                                ? null
                                : seats.Min(
                                    seat =>
                                        seat.Price);

                        decimal? maximumPrice =
                            seats.Count == 0
                                ? null
                                : seats.Max(
                                    seat =>
                                        seat.Price);

                        var isBookingOpen =
                            currentTimeUtc >=
                                session.BookingOpensAtUtc &&
                            currentTimeUtc <
                                session.BookingClosesAtUtc;

                        return new EventSessionCatalogItem(
                            session.Id,
                            session.EventId,
                            session.EventTitle,
                            session.VenueId,
                            session.VenueName,
                            session.VenueAddress,
                            session.HallId,
                            session.HallName,
                            session.StartsAtUtc,
                            session.BookingOpensAtUtc,
                            session.BookingClosesAtUtc,
                            isBookingOpen,
                            seats.Count,
                            seats.Count(
                                seat =>
                                    seat.Status ==
                                    SessionSeatStatus.Available),
                            minimumPrice,
                            maximumPrice);
                    })
                .ToList();

        var totalCount =
            sessionItems.Count;

        var orderedItems =
            ApplySessionSorting(
                sessionItems,
                validatedQuery.SortBy,
                validatedQuery.SortDirection);

        var skip =
            (validatedQuery.Page - 1) *
            validatedQuery.PageSize;

        var pageItems =
            orderedItems
                .Skip(skip)
                .Take(validatedQuery.PageSize)
                .ToList();

        return new PagedResult<
            EventSessionCatalogItem>(
                pageItems,
                validatedQuery.Page,
                validatedQuery.PageSize,
                totalCount);
    }

    public async Task<SessionSeatMap>
        GetSessionSeatsAsync(
            Guid sessionId,
            CancellationToken cancellationToken)
    {
        var header =
            await (
                from session in
                    _dbContext.EventSessions
                        .AsNoTracking()
                join eventEntity in
                    _dbContext.Events
                        .AsNoTracking()
                    on session.EventId equals
                        eventEntity.Id
                join hall in
                    _dbContext.Halls
                        .AsNoTracking()
                    on session.HallId equals
                        hall.Id
                join venue in
                    _dbContext.Venues
                        .AsNoTracking()
                    on hall.VenueId equals
                        venue.Id
                where session.Id == sessionId
                select new SessionHeaderProjection(
                    session.Id,
                    eventEntity.Id,
                    eventEntity.Title,
                    venue.Id,
                    venue.Name,
                    hall.Id,
                    hall.Name,
                    session.StartsAtUtc,
                    session.BookingOpensAtUtc,
                    session.BookingClosesAtUtc,
                    session.IsCancelled)
            ).SingleOrDefaultAsync(
                cancellationToken)
            ?? throw new ResourceNotFoundException(
                nameof(EventSession),
                sessionId);

        var currentTimeUtc =
            _timeProvider.GetUtcNow();

        var isBookingOpen =
            !header.IsCancelled &&
            currentTimeUtc >=
                header.BookingOpensAtUtc &&
            currentTimeUtc <
                header.BookingClosesAtUtc;

        var seatProjections =
            await (
                from sessionSeat in
                    _dbContext.SessionSeats
                        .AsNoTracking()
                join seat in
                    _dbContext.Seats
                        .AsNoTracking()
                    on sessionSeat.SeatId equals
                        seat.Id
                where
                    sessionSeat.EventSessionId ==
                        sessionId
                select new SeatMapProjection(
                    sessionSeat.Id,
                    seat.Id,
                    seat.RowLabel,
                    seat.Number,
                    seat.Category,
                    sessionSeat.Price,
                    sessionSeat.Status)
            ).ToListAsync(cancellationToken);

        var seats =
            seatProjections
                .OrderBy(
                    seat =>
                        seat.RowLabel,
                    StringComparer.OrdinalIgnoreCase)
                .ThenBy(
                    seat =>
                        seat.Number)
                .Select(
                    seat =>
                        new SessionSeatCatalogItem(
                            seat.SessionSeatId,
                            seat.SeatId,
                            seat.RowLabel,
                            seat.Number,
                            seat.Category,
                            seat.Price,
                            seat.Status,
                            isBookingOpen &&
                            seat.Status ==
                                SessionSeatStatus.Available))
                .ToList();

        var rows =
            seats
                .GroupBy(
                    seat =>
                        seat.RowLabel,
                    StringComparer.OrdinalIgnoreCase)
                .OrderBy(
                    group =>
                        group.Key,
                    StringComparer.OrdinalIgnoreCase)
                .Select(
                    group =>
                        new SeatMapRow(
                            group.Key,
                            group
                                .OrderBy(
                                    seat =>
                                        seat.Number)
                                .ToList()))
                .ToList();

        return new SessionSeatMap(
            header.SessionId,
            header.EventId,
            header.EventTitle,
            header.VenueId,
            header.VenueName,
            header.HallId,
            header.HallName,
            header.StartsAtUtc,
            header.BookingOpensAtUtc,
            header.BookingClosesAtUtc,
            header.IsCancelled,
            isBookingOpen,
            seats.Count,
            seats.Count(
                seat =>
                    seat.Status ==
                    SessionSeatStatus.Available),
            seats.Count(
                seat =>
                    seat.Status ==
                    SessionSeatStatus.Reserved),
            seats.Count(
                seat =>
                    seat.Status ==
                    SessionSeatStatus.Sold),
            rows);
    }

    private static PagedResult<EventCatalogItem>
        CreateEmptyEventResult(
            EventCatalogQuery query)
    {
        return new PagedResult<EventCatalogItem>(
            Array.Empty<EventCatalogItem>(),
            query.Page,
            query.PageSize,
            0);
    }

    private static IEnumerable<EventCatalogItem>
        ApplyEventSorting(
            IEnumerable<EventCatalogItem> items,
            EventCatalogSortField sortBy,
            CatalogSortDirection sortDirection)
    {
        return (sortBy, sortDirection) switch
        {
            (
                EventCatalogSortField.Title,
                CatalogSortDirection.Ascending
            ) =>
                items
                    .OrderBy(
                        item =>
                            item.Title,
                        StringComparer.OrdinalIgnoreCase)
                    .ThenBy(
                        item =>
                            item.NextSessionAtUtc),

            (
                EventCatalogSortField.Title,
                CatalogSortDirection.Descending
            ) =>
                items
                    .OrderByDescending(
                        item =>
                            item.Title,
                        StringComparer.OrdinalIgnoreCase)
                    .ThenBy(
                        item =>
                            item.NextSessionAtUtc),

            (
                EventCatalogSortField.Price,
                CatalogSortDirection.Ascending
            ) =>
                items
                    .OrderBy(
                        item =>
                            item.MinimumPrice ??
                            decimal.MaxValue)
                    .ThenBy(
                        item =>
                            item.NextSessionAtUtc),

            (
                EventCatalogSortField.Price,
                CatalogSortDirection.Descending
            ) =>
                items
                    .OrderByDescending(
                        item =>
                            item.MinimumPrice ??
                            decimal.MinValue)
                    .ThenBy(
                        item =>
                            item.NextSessionAtUtc),

            (
                EventCatalogSortField.StartsAt,
                CatalogSortDirection.Descending
            ) =>
                items
                    .OrderByDescending(
                        item =>
                            item.NextSessionAtUtc)
                    .ThenBy(
                        item =>
                            item.Title,
                        StringComparer.OrdinalIgnoreCase),

            _ =>
                items
                    .OrderBy(
                        item =>
                            item.NextSessionAtUtc)
                    .ThenBy(
                        item =>
                            item.Title,
                        StringComparer.OrdinalIgnoreCase)
        };
    }

    private static IEnumerable<
        EventSessionCatalogItem>
        ApplySessionSorting(
            IEnumerable<
                EventSessionCatalogItem> items,
            EventSessionSortField sortBy,
            CatalogSortDirection sortDirection)
    {
        return (sortBy, sortDirection) switch
        {
            (
                EventSessionSortField.Venue,
                CatalogSortDirection.Ascending
            ) =>
                items
                    .OrderBy(
                        item =>
                            item.VenueName,
                        StringComparer.OrdinalIgnoreCase)
                    .ThenBy(
                        item =>
                            item.StartsAtUtc),

            (
                EventSessionSortField.Venue,
                CatalogSortDirection.Descending
            ) =>
                items
                    .OrderByDescending(
                        item =>
                            item.VenueName,
                        StringComparer.OrdinalIgnoreCase)
                    .ThenBy(
                        item =>
                            item.StartsAtUtc),

            (
                EventSessionSortField.Price,
                CatalogSortDirection.Ascending
            ) =>
                items
                    .OrderBy(
                        item =>
                            item.MinimumPrice ??
                            decimal.MaxValue)
                    .ThenBy(
                        item =>
                            item.StartsAtUtc),

            (
                EventSessionSortField.Price,
                CatalogSortDirection.Descending
            ) =>
                items
                    .OrderByDescending(
                        item =>
                            item.MinimumPrice ??
                            decimal.MinValue)
                    .ThenBy(
                        item =>
                            item.StartsAtUtc),

            (
                EventSessionSortField.StartsAt,
                CatalogSortDirection.Descending
            ) =>
                items
                    .OrderByDescending(
                        item =>
                            item.StartsAtUtc),

            _ =>
                items
                    .OrderBy(
                        item =>
                            item.StartsAtUtc)
        };
    }

    private sealed record EventBaseProjection(
        Guid Id,
        string Title,
        string? Description,
        EventCategory Category,
        int AgeRestriction);

    private sealed record EligibleSessionProjection(
        Guid Id,
        Guid EventId,
        DateTimeOffset StartsAtUtc);

    private sealed record SessionCatalogBaseProjection(
        Guid Id,
        Guid EventId,
        string EventTitle,
        Guid VenueId,
        string VenueName,
        string VenueAddress,
        Guid HallId,
        string HallName,
        DateTimeOffset StartsAtUtc,
        DateTimeOffset BookingOpensAtUtc,
        DateTimeOffset BookingClosesAtUtc);

    private sealed record SessionSeatProjection(
        Guid EventSessionId,
        decimal Price,
        SessionSeatStatus Status);

    private sealed record SessionHeaderProjection(
        Guid SessionId,
        Guid EventId,
        string EventTitle,
        Guid VenueId,
        string VenueName,
        Guid HallId,
        string HallName,
        DateTimeOffset StartsAtUtc,
        DateTimeOffset BookingOpensAtUtc,
        DateTimeOffset BookingClosesAtUtc,
        bool IsCancelled);

    private sealed record SeatMapProjection(
        Guid SessionSeatId,
        Guid SeatId,
        string RowLabel,
        int Number,
        SeatCategory Category,
        decimal Price,
        SessionSeatStatus Status);
}
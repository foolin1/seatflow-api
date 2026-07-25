using Microsoft.EntityFrameworkCore;
using SeatFlow.Application.Common;
using SeatFlow.Application.Reservations;
using SeatFlow.Domain.Entities;
using SeatFlow.Domain.Enums;
using SeatFlow.Domain.Exceptions;
using SeatFlow.Infrastructure.Persistence;

using DomainEvent = SeatFlow.Domain.Entities.Event;

namespace SeatFlow.Infrastructure.Reservations;

public sealed class ReservationService
    : IReservationService
{
    private const int MaximumSeatsPerReservation = 8;

    private static readonly TimeSpan ReservationDuration =
        TimeSpan.FromMinutes(10);

    private readonly SeatFlowDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public ReservationService(
        SeatFlowDbContext dbContext,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<ReservationDetails>
        CreateReservationAsync(
            Guid userId,
            Guid eventSessionId,
            IReadOnlyCollection<Guid> sessionSeatIds,
            CancellationToken cancellationToken)
    {
        ValidateIdentifier(
            userId,
            nameof(userId));

        ValidateIdentifier(
            eventSessionId,
            nameof(eventSessionId));

        ArgumentNullException.ThrowIfNull(
            sessionSeatIds);

        var requestedSeatIds =
            sessionSeatIds.ToArray();

        if (requestedSeatIds.Length == 0)
        {
            throw new DomainValidationException(
                "At least one session seat must be selected.");
        }

        if (requestedSeatIds.Length >
            MaximumSeatsPerReservation)
        {
            throw new DomainValidationException(
                $"A reservation cannot contain more than " +
                $"{MaximumSeatsPerReservation} seats.");
        }

        if (requestedSeatIds.Any(
                sessionSeatId =>
                    sessionSeatId == Guid.Empty))
        {
            throw new DomainValidationException(
                "Session seat identifiers cannot be empty.");
        }

        var distinctSeatIds =
            requestedSeatIds
                .Distinct()
                .ToArray();

        if (distinctSeatIds.Length !=
            requestedSeatIds.Length)
        {
            throw new DomainValidationException(
                "A reservation cannot contain duplicate seats.");
        }

        var userExists =
            await _dbContext.Users
                .AsNoTracking()
                .AnyAsync(
                    user =>
                        user.Id == userId &&
                        user.IsActive,
                    cancellationToken);

        if (!userExists)
        {
            throw new ResourceNotFoundException(
                nameof(User),
                userId);
        }

        var currentTimeUtc =
            _timeProvider.GetUtcNow();

        var eventSession =
            await _dbContext.EventSessions
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    session =>
                        session.Id == eventSessionId,
                    cancellationToken)
            ?? throw new ResourceNotFoundException(
                nameof(EventSession),
                eventSessionId);

        eventSession.EnsureBookableAt(
            currentTimeUtc);

        var sessionSeats =
            await _dbContext.SessionSeats
                .Where(
                    sessionSeat =>
                        sessionSeat.EventSessionId ==
                            eventSessionId &&
                        distinctSeatIds.Contains(
                            sessionSeat.Id))
                .OrderBy(
                    sessionSeat =>
                        sessionSeat.Id)
                .ToListAsync(cancellationToken);

        if (sessionSeats.Count !=
            distinctSeatIds.Length)
        {
            var loadedSeatIds =
                sessionSeats
                    .Select(
                        sessionSeat =>
                            sessionSeat.Id)
                    .ToHashSet();

            var missingSeatId =
                distinctSeatIds.First(
                    sessionSeatId =>
                        !loadedSeatIds.Contains(
                            sessionSeatId));

            throw new ResourceNotFoundException(
                nameof(SessionSeat),
                missingSeatId);
        }

        var expiresAtUtc =
            currentTimeUtc.Add(
                ReservationDuration);

        if (expiresAtUtc >
            eventSession.BookingClosesAtUtc)
        {
            expiresAtUtc =
                eventSession.BookingClosesAtUtc;
        }

        if (expiresAtUtc <= currentTimeUtc)
        {
            throw new DomainConflictException(
                "The reservation cannot be created because " +
                "the booking window is closing.");
        }

        var reservationId =
            Guid.NewGuid();

        foreach (var sessionSeat in sessionSeats)
        {
            sessionSeat.Reserve(
                reservationId,
                expiresAtUtc,
                currentTimeUtc);
        }

        var reservationSeats =
            sessionSeats
                .Select(
                    sessionSeat =>
                        new ReservationSeat(
                            Guid.NewGuid(),
                            reservationId,
                            sessionSeat.Id,
                            sessionSeat.Price))
                .ToList();

        var reservation =
            new Reservation(
                reservationId,
                userId,
                eventSessionId,
                reservationSeats,
                currentTimeUtc,
                expiresAtUtc);

        _dbContext.Reservations.Add(
            reservation);

        await SaveBookingChangesAsync(
            "One or more selected seats were changed by " +
            "another booking request. Refresh the seat map " +
            "and try again.",
            cancellationToken);

        return await LoadReservationDetailsAsync(
            userId,
            reservationId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<ReservationDetails>>
        GetUserReservationsAsync(
            Guid userId,
            CancellationToken cancellationToken)
    {
        ValidateIdentifier(
            userId,
            nameof(userId));

        var reservationIds =
            await _dbContext.Reservations
                .AsNoTracking()
                .Where(
                    reservation =>
                        reservation.UserId == userId)
                .OrderByDescending(
                    reservation =>
                        reservation.CreatedAtUtc)
                .Take(50)
                .Select(
                    reservation =>
                        reservation.Id)
                .ToListAsync(cancellationToken);

        var result =
            new List<ReservationDetails>(
                reservationIds.Count);

        foreach (var reservationId in reservationIds)
        {
            result.Add(
                await LoadReservationDetailsAsync(
                    userId,
                    reservationId,
                    cancellationToken));
        }

        return result;
    }

    public Task<ReservationDetails>
        GetReservationAsync(
            Guid userId,
            Guid reservationId,
            CancellationToken cancellationToken)
    {
        ValidateIdentifier(
            userId,
            nameof(userId));

        ValidateIdentifier(
            reservationId,
            nameof(reservationId));

        return LoadReservationDetailsAsync(
            userId,
            reservationId,
            cancellationToken);
    }

    public async Task<ReservationDetails>
        CancelReservationAsync(
            Guid userId,
            Guid reservationId,
            CancellationToken cancellationToken)
    {
        var reservation =
            await LoadOwnedReservationForUpdateAsync(
                userId,
                reservationId,
                cancellationToken);

        var currentTimeUtc =
            _timeProvider.GetUtcNow();

        var sessionSeats =
            await LoadReservationSessionSeatsAsync(
                reservation,
                cancellationToken);

        if (currentTimeUtc >=
            reservation.ExpiresAtUtc)
        {
            if (reservation.Expire(
                    currentTimeUtc))
            {
                ReleaseSessionSeats(
                    sessionSeats,
                    reservation.Id);
            }

            await SaveBookingChangesAsync(
                "The reservation was changed by another request.",
                cancellationToken);

            throw new ReservationExpiredException(
                reservation.Id,
                reservation.ExpiresAtUtc);
        }

        reservation.Cancel(
            currentTimeUtc);

        ReleaseSessionSeats(
            sessionSeats,
            reservation.Id);

        await SaveBookingChangesAsync(
            "The reservation was changed by another request.",
            cancellationToken);

        return await LoadReservationDetailsAsync(
            userId,
            reservationId,
            cancellationToken);
    }

    public async Task<ReservationDetails>
        PayReservationAsync(
            Guid userId,
            Guid reservationId,
            CancellationToken cancellationToken)
    {
        var reservation =
            await LoadOwnedReservationForUpdateAsync(
                userId,
                reservationId,
                cancellationToken);

        var currentTimeUtc =
            _timeProvider.GetUtcNow();

        var sessionSeats =
            await LoadReservationSessionSeatsAsync(
                reservation,
                cancellationToken);

        if (currentTimeUtc >=
            reservation.ExpiresAtUtc)
        {
            if (reservation.Expire(
                    currentTimeUtc))
            {
                ReleaseSessionSeats(
                    sessionSeats,
                    reservation.Id);
            }

            await SaveBookingChangesAsync(
                "The reservation was changed by another request.",
                cancellationToken);

            throw new ReservationExpiredException(
                reservation.Id,
                reservation.ExpiresAtUtc);
        }

        foreach (var sessionSeat in sessionSeats)
        {
            sessionSeat.MarkAsSold(
                reservation.Id,
                currentTimeUtc);
        }

        reservation.Confirm(
            currentTimeUtc);

        var payment =
            new Payment(
                Guid.NewGuid(),
                reservation.Id,
                reservation.TotalAmount,
                currentTimeUtc);

        payment.Succeed(
            $"test-{Guid.NewGuid():N}",
            currentTimeUtc);

        _dbContext.Payments.Add(
            payment);

        await SaveBookingChangesAsync(
            "The reservation or one of its seats was changed " +
            "by another request.",
            cancellationToken);

        return await LoadReservationDetailsAsync(
            userId,
            reservationId,
            cancellationToken);
    }

    public async Task<int> ExpireReservationsAsync(
        CancellationToken cancellationToken)
    {
        var currentTimeUtc =
            _timeProvider.GetUtcNow();

        var reservations =
            await _dbContext.Reservations
                .Include(
                    reservation =>
                        reservation.Seats)
                .Where(
                    reservation =>
                        reservation.Status ==
                            ReservationStatus.Pending &&
                        reservation.ExpiresAtUtc <=
                            currentTimeUtc)
                .OrderBy(
                    reservation =>
                        reservation.ExpiresAtUtc)
                .Take(100)
                .ToListAsync(cancellationToken);

        if (reservations.Count == 0)
        {
            return 0;
        }

        var reservationIds =
            reservations
                .Select(
                    reservation =>
                        reservation.Id)
                .ToArray();

        var sessionSeats =
            await _dbContext.SessionSeats
                .Where(
                    sessionSeat =>
                        sessionSeat.Status ==
                            SessionSeatStatus.Reserved &&
                        sessionSeat.ReservationId.HasValue &&
                        reservationIds.Contains(
                            sessionSeat.ReservationId.Value))
                .ToListAsync(cancellationToken);

        var sessionSeatsByReservation =
            sessionSeats.ToLookup(
                sessionSeat =>
                    sessionSeat.ReservationId
                        .GetValueOrDefault());

        var expiredCount = 0;

        foreach (var reservation in reservations)
        {
            if (!reservation.Expire(
                    currentTimeUtc))
            {
                continue;
            }

            foreach (var sessionSeat in
                     sessionSeatsByReservation[
                         reservation.Id])
            {
                sessionSeat.Release(
                    reservation.Id);
            }

            expiredCount++;
        }

        if (expiredCount == 0)
        {
            return 0;
        }

        await SaveBookingChangesAsync(
            "Some reservations were changed while " +
            "the expiration process was running.",
            cancellationToken);

        return expiredCount;
    }

    private async Task<Reservation>
        LoadOwnedReservationForUpdateAsync(
            Guid userId,
            Guid reservationId,
            CancellationToken cancellationToken)
    {
        ValidateIdentifier(
            userId,
            nameof(userId));

        ValidateIdentifier(
            reservationId,
            nameof(reservationId));

        return await _dbContext.Reservations
            .Include(
                reservation =>
                    reservation.Seats)
            .SingleOrDefaultAsync(
                reservation =>
                    reservation.Id == reservationId &&
                    reservation.UserId == userId,
                cancellationToken)
            ?? throw new ResourceNotFoundException(
                nameof(Reservation),
                reservationId);
    }

    private async Task<List<SessionSeat>>
        LoadReservationSessionSeatsAsync(
            Reservation reservation,
            CancellationToken cancellationToken)
    {
        var sessionSeatIds =
            reservation.Seats
                .Select(
                    reservationSeat =>
                        reservationSeat.SessionSeatId)
                .ToArray();

        return await _dbContext.SessionSeats
            .Where(
                sessionSeat =>
                    sessionSeatIds.Contains(
                        sessionSeat.Id))
            .OrderBy(
                sessionSeat =>
                    sessionSeat.Id)
            .ToListAsync(cancellationToken);
    }

    private async Task<ReservationDetails>
        LoadReservationDetailsAsync(
            Guid userId,
            Guid reservationId,
            CancellationToken cancellationToken)
    {
        var header =
            await (
                from reservation in
                    _dbContext.Reservations
                        .AsNoTracking()
                join eventSession in
                    _dbContext.EventSessions
                        .AsNoTracking()
                    on reservation.EventSessionId equals
                        eventSession.Id
                join eventEntity in
                    _dbContext.Events
                        .AsNoTracking()
                    on eventSession.EventId equals
                        eventEntity.Id
                join hall in
                    _dbContext.Halls
                        .AsNoTracking()
                    on eventSession.HallId equals
                        hall.Id
                join venue in
                    _dbContext.Venues
                        .AsNoTracking()
                    on hall.VenueId equals
                        venue.Id
                where
                    reservation.Id == reservationId &&
                    reservation.UserId == userId
                select new ReservationHeaderProjection(
                    reservation.Id,
                    reservation.EventSessionId,
                    eventEntity.Id,
                    eventEntity.Title,
                    venue.Name,
                    hall.Name,
                    eventSession.StartsAtUtc,
                    reservation.Status,
                    reservation.TotalAmount,
                    reservation.CreatedAtUtc,
                    reservation.ExpiresAtUtc,
                    reservation.ConfirmedAtUtc,
                    reservation.CancelledAtUtc,
                    reservation.ExpiredAtUtc)
            ).SingleOrDefaultAsync(
                cancellationToken)
            ?? throw new ResourceNotFoundException(
                nameof(Reservation),
                reservationId);

        var seats =
            await (
                from reservationSeat in
                    _dbContext.ReservationSeats
                        .AsNoTracking()
                join sessionSeat in
                    _dbContext.SessionSeats
                        .AsNoTracking()
                    on reservationSeat.SessionSeatId equals
                        sessionSeat.Id
                join seat in
                    _dbContext.Seats
                        .AsNoTracking()
                    on sessionSeat.SeatId equals
                        seat.Id
                where
                    reservationSeat.ReservationId ==
                        reservationId
                orderby
                    seat.RowLabel,
                    seat.Number
                select new ReservationSeatDetails(
                    sessionSeat.Id,
                    seat.Id,
                    seat.RowLabel,
                    seat.Number,
                    seat.Category,
                    reservationSeat.Price,
                    sessionSeat.Status)
            ).ToListAsync(cancellationToken);

        var payment =
            await _dbContext.Payments
                .AsNoTracking()
                .Where(
                    currentPayment =>
                        currentPayment.ReservationId ==
                            reservationId)
                .OrderByDescending(
                    currentPayment =>
                        currentPayment.CreatedAtUtc)
                .Select(
                    currentPayment =>
                        new PaymentDetails(
                            currentPayment.Id,
                            currentPayment.Amount,
                            currentPayment.Status,
                            currentPayment.ExternalReference,
                            currentPayment.FailureReason,
                            currentPayment.CreatedAtUtc,
                            currentPayment.CompletedAtUtc))
                .FirstOrDefaultAsync(cancellationToken);

        return new ReservationDetails(
            header.Id,
            header.EventSessionId,
            header.EventId,
            header.EventTitle,
            header.VenueName,
            header.HallName,
            header.StartsAtUtc,
            header.Status,
            header.TotalAmount,
            header.CreatedAtUtc,
            header.ExpiresAtUtc,
            header.ConfirmedAtUtc,
            header.CancelledAtUtc,
            header.ExpiredAtUtc,
            seats,
            payment);
    }

    private async Task SaveBookingChangesAsync(
        string concurrencyMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new DomainConflictException(
                concurrencyMessage);
        }
    }

    private static void ReleaseSessionSeats(
        IEnumerable<SessionSeat> sessionSeats,
        Guid reservationId)
    {
        foreach (var sessionSeat in sessionSeats)
        {
            sessionSeat.Release(
                reservationId);
        }
    }

    private static void ValidateIdentifier(
        Guid identifier,
        string parameterName)
    {
        if (identifier == Guid.Empty)
        {
            throw new DomainValidationException(
                $"{parameterName} cannot be empty.");
        }
    }

    private sealed record ReservationHeaderProjection(
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
        DateTimeOffset? ExpiredAtUtc);
}
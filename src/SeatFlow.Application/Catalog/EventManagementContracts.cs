using SeatFlow.Domain.Enums;

namespace SeatFlow.Application.Catalog;

public sealed record VenueDetails(
    Guid Id,
    string Name,
    string Address,
    string? Description);

public sealed record HallDetails(
    Guid Id,
    Guid VenueId,
    string Name,
    int Capacity);

public sealed record SeatDetails(
    Guid Id,
    Guid HallId,
    string RowLabel,
    int Number,
    SeatCategory Category);

public sealed record EventDetails(
    Guid Id,
    string Title,
    string? Description,
    EventCategory Category,
    int AgeRestriction);

public sealed record EventSessionDetails(
    Guid Id,
    Guid EventId,
    Guid HallId,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset BookingOpensAtUtc,
    DateTimeOffset BookingClosesAtUtc,
    bool IsCancelled,
    DateTimeOffset? CancelledAtUtc,
    int SeatCount,
    decimal? MinimumPrice,
    decimal? MaximumPrice);

public interface IEventManagementService
{
    Task<VenueDetails> CreateVenueAsync(
        string name,
        string address,
        string? description,
        CancellationToken cancellationToken);

    Task<VenueDetails> GetVenueAsync(
        Guid venueId,
        CancellationToken cancellationToken);

    Task<VenueDetails> UpdateVenueAsync(
        Guid venueId,
        string name,
        string address,
        string? description,
        CancellationToken cancellationToken);

    Task DeleteVenueAsync(
        Guid venueId,
        CancellationToken cancellationToken);

    Task<HallDetails> CreateHallAsync(
        Guid venueId,
        string name,
        int capacity,
        CancellationToken cancellationToken);

    Task<HallDetails> GetHallAsync(
        Guid hallId,
        CancellationToken cancellationToken);

    Task<HallDetails> UpdateHallAsync(
        Guid hallId,
        string name,
        int capacity,
        CancellationToken cancellationToken);

    Task DeleteHallAsync(
        Guid hallId,
        CancellationToken cancellationToken);

    Task<SeatDetails> CreateSeatAsync(
        Guid hallId,
        string rowLabel,
        int number,
        SeatCategory category,
        CancellationToken cancellationToken);

    Task<SeatDetails> GetSeatAsync(
        Guid seatId,
        CancellationToken cancellationToken);

    Task<SeatDetails> UpdateSeatAsync(
        Guid seatId,
        string rowLabel,
        int number,
        SeatCategory category,
        CancellationToken cancellationToken);

    Task DeleteSeatAsync(
        Guid seatId,
        CancellationToken cancellationToken);

    Task<EventDetails> CreateEventAsync(
        string title,
        string? description,
        EventCategory category,
        int ageRestriction,
        CancellationToken cancellationToken);

    Task<EventDetails> GetEventAsync(
        Guid eventId,
        CancellationToken cancellationToken);

    Task<EventDetails> UpdateEventAsync(
        Guid eventId,
        string title,
        string? description,
        EventCategory category,
        int ageRestriction,
        CancellationToken cancellationToken);

    Task DeleteEventAsync(
        Guid eventId,
        CancellationToken cancellationToken);

    Task<EventSessionDetails> CreateSessionAsync(
        Guid eventId,
        Guid hallId,
        DateTimeOffset startsAtUtc,
        DateTimeOffset bookingOpensAtUtc,
        DateTimeOffset bookingClosesAtUtc,
        decimal defaultPrice,
        CancellationToken cancellationToken);

    Task<EventSessionDetails> GetSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken);

    Task<EventSessionDetails> UpdateSessionAsync(
        Guid sessionId,
        DateTimeOffset startsAtUtc,
        DateTimeOffset bookingOpensAtUtc,
        DateTimeOffset bookingClosesAtUtc,
        CancellationToken cancellationToken);

    Task<EventSessionDetails> CancelSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken);

    Task DeleteSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken);
}
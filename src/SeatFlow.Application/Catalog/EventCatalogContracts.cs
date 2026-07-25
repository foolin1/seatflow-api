using SeatFlow.Domain.Enums;

namespace SeatFlow.Application.Catalog;

public enum EventCatalogSortField
{
    StartsAt = 1,
    Title = 2,
    Price = 3
}

public enum EventSessionSortField
{
    StartsAt = 1,
    Venue = 2,
    Price = 3
}

public enum CatalogSortDirection
{
    Ascending = 1,
    Descending = 2
}

public sealed record EventCatalogQuery(
    string? Search = null,
    EventCategory? Category = null,
    Guid? VenueId = null,
    DateTimeOffset? StartsFromUtc = null,
    DateTimeOffset? StartsToUtc = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    EventCatalogSortField SortBy =
        EventCatalogSortField.StartsAt,
    CatalogSortDirection SortDirection =
        CatalogSortDirection.Ascending,
    int Page = 1,
    int PageSize = 20);

public sealed record EventSessionCatalogQuery(
    Guid? VenueId = null,
    DateTimeOffset? StartsFromUtc = null,
    DateTimeOffset? StartsToUtc = null,
    EventSessionSortField SortBy =
        EventSessionSortField.StartsAt,
    CatalogSortDirection SortDirection =
        CatalogSortDirection.Ascending,
    int Page = 1,
    int PageSize = 20);

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages =>
        TotalCount == 0
            ? 0
            : (int)Math.Ceiling(
                TotalCount / (double)PageSize);

    public bool HasPreviousPage =>
        Page > 1;

    public bool HasNextPage =>
        Page < TotalPages;
}

public sealed record EventCatalogItem(
    Guid Id,
    string Title,
    string? Description,
    EventCategory Category,
    int AgeRestriction,
    DateTimeOffset NextSessionAtUtc,
    int UpcomingSessionCount,
    decimal? MinimumPrice,
    decimal? MaximumPrice);

public sealed record EventCatalogDetails(
    Guid Id,
    string Title,
    string? Description,
    EventCategory Category,
    int AgeRestriction,
    DateTimeOffset? NextSessionAtUtc,
    int UpcomingSessionCount,
    decimal? MinimumPrice,
    decimal? MaximumPrice);

public sealed record EventSessionCatalogItem(
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
    DateTimeOffset BookingClosesAtUtc,
    bool IsBookingOpen,
    int SeatCount,
    int AvailableSeatCount,
    decimal? MinimumPrice,
    decimal? MaximumPrice);

public sealed record SessionSeatCatalogItem(
    Guid SessionSeatId,
    Guid SeatId,
    string RowLabel,
    int Number,
    SeatCategory Category,
    decimal Price,
    SessionSeatStatus Status,
    bool IsAvailable);

public sealed record SeatMapRow(
    string RowLabel,
    IReadOnlyList<SessionSeatCatalogItem> Seats);

public sealed record SessionSeatMap(
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
    bool IsCancelled,
    bool IsBookingOpen,
    int TotalSeatCount,
    int AvailableSeatCount,
    int ReservedSeatCount,
    int SoldSeatCount,
    IReadOnlyList<SeatMapRow> Rows);

public interface IEventCatalogService
{
    Task<PagedResult<EventCatalogItem>> GetEventsAsync(
        EventCatalogQuery query,
        CancellationToken cancellationToken);

    Task<EventCatalogDetails> GetEventAsync(
        Guid eventId,
        CancellationToken cancellationToken);

    Task<PagedResult<EventSessionCatalogItem>>
        GetEventSessionsAsync(
            Guid eventId,
            EventSessionCatalogQuery query,
            CancellationToken cancellationToken);

    Task<SessionSeatMap> GetSessionSeatsAsync(
        Guid sessionId,
        CancellationToken cancellationToken);
}
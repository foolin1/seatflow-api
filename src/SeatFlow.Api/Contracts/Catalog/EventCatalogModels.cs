using SeatFlow.Application.Catalog;
using SeatFlow.Domain.Enums;

namespace SeatFlow.Api.Contracts.Catalog;

public sealed class EventCatalogRequest
{
    public string? Search { get; init; }

    public EventCategory? Category { get; init; }

    public Guid? VenueId { get; init; }

    public DateTimeOffset? StartsFromUtc { get; init; }

    public DateTimeOffset? StartsToUtc { get; init; }

    public decimal? MinPrice { get; init; }

    public decimal? MaxPrice { get; init; }

    public EventCatalogSortField SortBy { get; init; } =
        EventCatalogSortField.StartsAt;

    public CatalogSortDirection SortDirection { get; init; } =
        CatalogSortDirection.Ascending;

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public EventCatalogQuery ToQuery()
    {
        return new EventCatalogQuery(
            Search,
            Category,
            VenueId,
            StartsFromUtc,
            StartsToUtc,
            MinPrice,
            MaxPrice,
            SortBy,
            SortDirection,
            Page,
            PageSize);
    }
}

public sealed class EventSessionCatalogRequest
{
    public Guid? VenueId { get; init; }

    public DateTimeOffset? StartsFromUtc { get; init; }

    public DateTimeOffset? StartsToUtc { get; init; }

    public EventSessionSortField SortBy { get; init; } =
        EventSessionSortField.StartsAt;

    public CatalogSortDirection SortDirection { get; init; } =
        CatalogSortDirection.Ascending;

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public EventSessionCatalogQuery ToQuery()
    {
        return new EventSessionCatalogQuery(
            VenueId,
            StartsFromUtc,
            StartsToUtc,
            SortBy,
            SortDirection,
            Page,
            PageSize);
    }
}
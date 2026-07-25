using SeatFlow.Domain.Enums;
using SeatFlow.Domain.Exceptions;

namespace SeatFlow.Application.Catalog;

public static class EventCatalogQueryValidator
{
    private const int MaximumPageSize = 100;
    private const int MaximumPageNumber = 1_000_000;
    private const int MaximumSearchLength = 200;

    public static EventCatalogQuery ValidateAndNormalize(
        EventCatalogQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        ValidatePagination(
            query.Page,
            query.PageSize);

        ValidateVenueId(query.VenueId);

        ValidateDateRange(
            query.StartsFromUtc,
            query.StartsToUtc);

        ValidatePriceRange(
            query.MinPrice,
            query.MaxPrice);

        if (query.Category.HasValue &&
            !Enum.IsDefined(
                typeof(EventCategory),
                query.Category.Value))
        {
            throw new DomainValidationException(
                "Event category is invalid.");
        }

        if (!Enum.IsDefined(
                typeof(EventCatalogSortField),
                query.SortBy))
        {
            throw new DomainValidationException(
                "Event catalog sort field is invalid.");
        }

        ValidateSortDirection(
            query.SortDirection);

        var normalizedSearch =
            NormalizeSearch(query.Search);

        return query with
        {
            Search = normalizedSearch,
            StartsFromUtc =
                query.StartsFromUtc?.ToUniversalTime(),
            StartsToUtc =
                query.StartsToUtc?.ToUniversalTime()
        };
    }

    public static EventSessionCatalogQuery
        ValidateAndNormalize(
            EventSessionCatalogQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        ValidatePagination(
            query.Page,
            query.PageSize);

        ValidateVenueId(query.VenueId);

        ValidateDateRange(
            query.StartsFromUtc,
            query.StartsToUtc);

        if (!Enum.IsDefined(
                typeof(EventSessionSortField),
                query.SortBy))
        {
            throw new DomainValidationException(
                "Event session sort field is invalid.");
        }

        ValidateSortDirection(
            query.SortDirection);

        return query with
        {
            StartsFromUtc =
                query.StartsFromUtc?.ToUniversalTime(),
            StartsToUtc =
                query.StartsToUtc?.ToUniversalTime()
        };
    }

    private static string? NormalizeSearch(
        string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return null;
        }

        var normalizedSearch =
            search.Trim();

        if (normalizedSearch.Length >
            MaximumSearchLength)
        {
            throw new DomainValidationException(
                $"Search text cannot exceed " +
                $"{MaximumSearchLength} characters.");
        }

        return normalizedSearch;
    }

    private static void ValidatePagination(
        int page,
        int pageSize)
    {
        if (page < 1 ||
            page > MaximumPageNumber)
        {
            throw new DomainValidationException(
                $"Page must be between 1 and " +
                $"{MaximumPageNumber}.");
        }

        if (pageSize < 1 ||
            pageSize > MaximumPageSize)
        {
            throw new DomainValidationException(
                $"Page size must be between 1 and " +
                $"{MaximumPageSize}.");
        }
    }

    private static void ValidateVenueId(
        Guid? venueId)
    {
        if (venueId == Guid.Empty)
        {
            throw new DomainValidationException(
                "Venue identifier cannot be empty.");
        }
    }

    private static void ValidateDateRange(
        DateTimeOffset? startsFromUtc,
        DateTimeOffset? startsToUtc)
    {
        if (startsFromUtc.HasValue &&
            startsToUtc.HasValue &&
            startsFromUtc.Value.ToUniversalTime() >
            startsToUtc.Value.ToUniversalTime())
        {
            throw new DomainValidationException(
                "Start date cannot be later than end date.");
        }
    }

    private static void ValidatePriceRange(
        decimal? minPrice,
        decimal? maxPrice)
    {
        if (minPrice < 0)
        {
            throw new DomainValidationException(
                "Minimum price cannot be negative.");
        }

        if (maxPrice < 0)
        {
            throw new DomainValidationException(
                "Maximum price cannot be negative.");
        }

        if (minPrice.HasValue &&
            maxPrice.HasValue &&
            minPrice.Value > maxPrice.Value)
        {
            throw new DomainValidationException(
                "Minimum price cannot exceed maximum price.");
        }
    }

    private static void ValidateSortDirection(
        CatalogSortDirection sortDirection)
    {
        if (!Enum.IsDefined(
                typeof(CatalogSortDirection),
                sortDirection))
        {
            throw new DomainValidationException(
                "Catalog sort direction is invalid.");
        }
    }
}
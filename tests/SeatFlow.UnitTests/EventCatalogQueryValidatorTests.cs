using SeatFlow.Application.Catalog;
using SeatFlow.Domain.Exceptions;

namespace SeatFlow.UnitTests;

public sealed class EventCatalogQueryValidatorTests
{
    [Fact]
    public void EventQuery_WithDefaults_ReturnsValidQuery()
    {
        var query =
            new EventCatalogQuery();

        var result =
            EventCatalogQueryValidator
                .ValidateAndNormalize(query);

        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);

        Assert.Equal(
            EventCatalogSortField.StartsAt,
            result.SortBy);

        Assert.Equal(
            CatalogSortDirection.Ascending,
            result.SortDirection);
    }

    [Fact]
    public void EventQuery_NormalizesSearchAndDates()
    {
        var startsFromUtc =
            new DateTimeOffset(
                2027,
                1,
                1,
                12,
                0,
                0,
                TimeSpan.FromHours(3));

        var startsToUtc =
            startsFromUtc.AddDays(30);

        var query =
            new EventCatalogQuery(
                Search: "  concert  ",
                StartsFromUtc: startsFromUtc,
                StartsToUtc: startsToUtc);

        var result =
            EventCatalogQueryValidator
                .ValidateAndNormalize(query);

        Assert.Equal(
            "concert",
            result.Search);

        Assert.Equal(
            startsFromUtc.ToUniversalTime(),
            result.StartsFromUtc);

        Assert.Equal(
            startsToUtc.ToUniversalTime(),
            result.StartsToUtc);
    }

    [Fact]
    public void EventQuery_WithInvalidPage_ThrowsValidation()
    {
        var query =
            new EventCatalogQuery(
                Page: 0);

        Assert.Throws<DomainValidationException>(
            () =>
                EventCatalogQueryValidator
                    .ValidateAndNormalize(query));
    }

    [Fact]
    public void EventQuery_WithInvalidPageSize_ThrowsValidation()
    {
        var query =
            new EventCatalogQuery(
                PageSize: 101);

        Assert.Throws<DomainValidationException>(
            () =>
                EventCatalogQueryValidator
                    .ValidateAndNormalize(query));
    }

    [Fact]
    public void EventQuery_WithInvalidDateRange_ThrowsValidation()
    {
        var startsFromUtc =
            DateTimeOffset.UtcNow.AddDays(10);

        var startsToUtc =
            DateTimeOffset.UtcNow.AddDays(1);

        var query =
            new EventCatalogQuery(
                StartsFromUtc: startsFromUtc,
                StartsToUtc: startsToUtc);

        Assert.Throws<DomainValidationException>(
            () =>
                EventCatalogQueryValidator
                    .ValidateAndNormalize(query));
    }

    [Fact]
    public void EventQuery_WithInvalidPriceRange_ThrowsValidation()
    {
        var query =
            new EventCatalogQuery(
                MinPrice: 100m,
                MaxPrice: 50m);

        Assert.Throws<DomainValidationException>(
            () =>
                EventCatalogQueryValidator
                    .ValidateAndNormalize(query));
    }

    [Fact]
    public void PagedResult_CalculatesPaginationMetadata()
    {
        var result =
            new PagedResult<int>(
                Items: new[] { 11, 12, 13 },
                Page: 2,
                PageSize: 3,
                TotalCount: 8);

        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasPreviousPage);
        Assert.True(result.HasNextPage);
    }
}
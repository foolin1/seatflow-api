using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SeatFlow.Domain.Entities;
using SeatFlow.Infrastructure.Persistence;

namespace SeatFlow.IntegrationTests.Persistence;

public sealed class PersistenceModelTests
{
    [Fact]
    public void SessionSeatVersion_IsMappedToPostgreSqlXmin()
    {
        using var context = CreateContext();

        var entityType =
            context.Model.FindEntityType(
                typeof(SessionSeat))
            ?? throw new InvalidOperationException(
                "SessionSeat entity mapping was not found.");

        var versionProperty =
            entityType.FindProperty(
                nameof(SessionSeat.Version))
            ?? throw new InvalidOperationException(
                "SessionSeat.Version mapping was not found.");

        var tableIdentifier =
            StoreObjectIdentifier.Table(
                "session_seats",
                schema: null);

        Assert.Equal(
            "xmin",
            versionProperty.GetColumnName(
                tableIdentifier));

        Assert.True(
            versionProperty.IsConcurrencyToken);

        Assert.Equal(
            ValueGenerated.OnAddOrUpdate,
            versionProperty.ValueGenerated);
    }

    [Fact]
    public void Seat_HasUniqueIndexForHallRowAndNumber()
    {
        using var context = CreateContext();

        var entityType =
            context.Model.FindEntityType(typeof(Seat))
            ?? throw new InvalidOperationException(
                "Seat entity mapping was not found.");

        var expectedProperties = new[]
        {
            nameof(Seat.HallId),
            nameof(Seat.RowLabel),
            nameof(Seat.Number)
        };

        var index = entityType
            .GetIndexes()
            .SingleOrDefault(
                currentIndex =>
                    currentIndex.Properties
                        .Select(property => property.Name)
                        .SequenceEqual(expectedProperties))
            ?? throw new InvalidOperationException(
                "The hall, row and number index was not found.");

        Assert.True(index.IsUnique);
    }

    private static SeatFlowDbContext CreateContext()
    {
        var options =
            new DbContextOptionsBuilder<SeatFlowDbContext>()
                .UseNpgsql(
                    "Host=localhost;" +
                    "Database=seatflow_model_tests;" +
                    "Username=test;" +
                    "Password=test")
                .Options;

        return new SeatFlowDbContext(options);
    }
}
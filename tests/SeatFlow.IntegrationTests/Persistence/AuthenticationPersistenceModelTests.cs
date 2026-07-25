using Microsoft.EntityFrameworkCore;
using SeatFlow.Domain.Entities;
using SeatFlow.Infrastructure.Persistence;

namespace SeatFlow.IntegrationTests.Persistence;

public sealed class AuthenticationPersistenceModelTests
{
    [Fact]
    public void UserEmail_HasUniqueIndex()
    {
        using var context = CreateContext();

        var entityType =
            context.Model.FindEntityType(typeof(User))
            ?? throw new InvalidOperationException(
                "User mapping was not found.");

        var emailIndex = entityType
            .GetIndexes()
            .SingleOrDefault(
                index =>
                    index.Properties.Count == 1 &&
                    index.Properties[0].Name ==
                    nameof(User.Email))
            ?? throw new InvalidOperationException(
                "User email index was not found.");

        Assert.True(emailIndex.IsUnique);
    }

    [Fact]
    public void RefreshTokenHash_HasUniqueIndex()
    {
        using var context = CreateContext();

        var entityType =
            context.Model.FindEntityType(
                typeof(RefreshToken))
            ?? throw new InvalidOperationException(
                "RefreshToken mapping was not found.");

        var tokenHashIndex = entityType
            .GetIndexes()
            .SingleOrDefault(
                index =>
                    index.Properties.Count == 1 &&
                    index.Properties[0].Name ==
                    nameof(RefreshToken.TokenHash))
            ?? throw new InvalidOperationException(
                "Refresh token hash index was not found.");

        Assert.True(tokenHashIndex.IsUnique);
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
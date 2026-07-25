using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SeatFlow.Infrastructure.Persistence;

namespace SeatFlow.Api.Health;

public sealed class DatabaseHealthCheck
    : IHealthCheck
{
    private readonly IServiceScopeFactory
        _serviceScopeFactory;

    public DatabaseHealthCheck(
        IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory =
            serviceScopeFactory;
    }

    public async Task<HealthCheckResult>
        CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken =
                default)
    {
        await using var scope =
            _serviceScopeFactory.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<
                    SeatFlowDbContext>();

        var canConnect =
            await dbContext.Database.CanConnectAsync(
                cancellationToken);

        return canConnect
            ? HealthCheckResult.Healthy(
                "PostgreSQL connection is available.")
            : HealthCheckResult.Unhealthy(
                "PostgreSQL connection is unavailable.");
    }
}
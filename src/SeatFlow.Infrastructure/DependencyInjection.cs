using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SeatFlow.Infrastructure.Persistence;

namespace SeatFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "The PostgreSQL connection string is not configured.");
        }

        services.AddDbContext<SeatFlowDbContext>(
            options =>
                options.UseNpgsql(
                    connectionString,
                    npgsqlOptions =>
                    {
                        npgsqlOptions.MigrationsAssembly(
                            "SeatFlow.Infrastructure");

                        npgsqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 3,
                            maxRetryDelay:
                                TimeSpan.FromSeconds(5),
                            errorCodesToAdd: null);
                    }));

        return services;
    }
}
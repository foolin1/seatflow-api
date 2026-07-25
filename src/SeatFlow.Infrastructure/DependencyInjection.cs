using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SeatFlow.Application.Authentication;
using SeatFlow.Application.Catalog;
using SeatFlow.Domain.Entities;
using SeatFlow.Infrastructure.Authentication;
using SeatFlow.Infrastructure.Catalog;
using SeatFlow.Infrastructure.Persistence;

namespace SeatFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        JwtOptions jwtOptions)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "The PostgreSQL connection string is not configured.");
        }

        ArgumentNullException.ThrowIfNull(jwtOptions);

        jwtOptions.Validate();

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

        services.AddOptions();

        services.AddSingleton(jwtOptions);

        services.AddSingleton<TimeProvider>(
            TimeProvider.System);

        services.AddScoped<
            IPasswordHasher<User>,
            PasswordHasher<User>>();

        services.AddScoped<
            IAuthenticationService,
            AuthenticationService>();

        services.AddScoped<
            IEventManagementService,
            EventManagementService>();

        return services;
    }
}
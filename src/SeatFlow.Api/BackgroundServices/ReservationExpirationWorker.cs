using SeatFlow.Application.Reservations;

namespace SeatFlow.Api.BackgroundServices;

public sealed class ReservationExpirationWorker
    : BackgroundService
{
    private static readonly TimeSpan CheckInterval =
        TimeSpan.FromSeconds(15);

    private readonly IServiceScopeFactory
        _serviceScopeFactory;

    private readonly ILogger<
        ReservationExpirationWorker> _logger;

    public ReservationExpirationWorker(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<ReservationExpirationWorker> logger)
    {
        _serviceScopeFactory =
            serviceScopeFactory;

        _logger =
            logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        await ProcessExpiredReservationsAsync(
            stoppingToken);

        using var timer =
            new PeriodicTimer(
                CheckInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(
                       stoppingToken))
            {
                await ProcessExpiredReservationsAsync(
                    stoppingToken);
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Application shutdown.
        }
    }

    private async Task
        ProcessExpiredReservationsAsync(
            CancellationToken cancellationToken)
    {
        try
        {
            using var scope =
                _serviceScopeFactory.CreateScope();

            var reservationService =
                scope.ServiceProvider
                    .GetRequiredService<
                        IReservationService>();

            var expiredCount =
                await reservationService
                    .ExpireReservationsAsync(
                        cancellationToken);

            if (expiredCount > 0)
            {
                _logger.LogInformation(
                    "Expired {ReservationCount} reservations.",
                    expiredCount);
            }
        }
        catch (OperationCanceledException)
            when (cancellationToken
                .IsCancellationRequested)
        {
            // Application shutdown.
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Reservation expiration processing failed.");
        }
    }
}
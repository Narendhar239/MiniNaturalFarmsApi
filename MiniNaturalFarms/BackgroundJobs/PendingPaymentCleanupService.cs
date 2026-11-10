using MiniNaturalFarms.Application.Orders;

namespace MiniNaturalFarms.BackgroundJobs;

/// <summary>Every 5 minutes: cancel online orders that were never paid within 30 minutes and release their stock.</summary>
public class PendingPaymentCleanupService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan MaxAge = TimeSpan.FromMinutes(30);

    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<PendingPaymentCleanupService> _logger;

    public PendingPaymentCleanupService(IServiceScopeFactory scopes, ILogger<PendingPaymentCleanupService> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    using var scope = _scopes.CreateScope();
                    var orders = scope.ServiceProvider.GetRequiredService<IOrderService>();
                    var count = await orders.CancelExpiredUnpaidOrdersAsync(MaxAge, stoppingToken);
                    if (count > 0)
                        _logger.LogInformation("Cancelled {Count} unpaid online order(s) and restored stock.", count);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Pending payment cleanup failed.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // app is shutting down
        }
    }
}
namespace NoBeard.Labs.Observability.Web.Services;

/// <summary>
/// Background load so the dashboards are never empty on stage. Can be paused / tuned from the Control page.
/// </summary>
public sealed class TrafficGenerator(OrderService orders, ILogger<TrafficGenerator> logger) : BackgroundService
{
    public bool Enabled { get; set; } = true;
    public double OrdersPerSecond { get; set; } = 2;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Give Alloy/LGTM a moment after startup.
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        logger.LogInformation("Traffic generator started at {OrdersPerSecond} orders/s", OrdersPerSecond);

        while (!stoppingToken.IsCancellationRequested)
        {
            if (!Enabled || OrdersPerSecond <= 0)
            {
                await Task.Delay(500, stoppingToken);
                continue;
            }

            // Fire-and-forget so slow (chaos) orders don't throttle the generator.
            _ = PlaceRandomOrderAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(1 / OrdersPerSecond), stoppingToken);
        }
    }

    private async Task PlaceRandomOrderAsync(CancellationToken ct)
    {
        try
        {
            await orders.PlaceOrderAsync(Catalog.Products[Random.Shared.Next(Catalog.Products.Length)].Id,
                Random.Shared.Next(1, 4), origin: "traffic-generator", ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Synthetic order failed");
        }
    }
}

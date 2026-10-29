using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace NoBeard.Labs.Observability.Web.Services;

public record Product(string Id, string Name, decimal Price);
public record OrderConfirmation(string OrderId, string ProductName, int Quantity, decimal Total);
public record OrderResult(bool Success, OrderConfirmation? Order, string? Error, string TraceId, DateTimeOffset At);

public static class Catalog
{
    public static readonly Product[] Products =
    [
        new("otel-hoodie", "OpenTelemetry Hoodie", 49.90m),
        new("aspire-mug", "Aspire Coffee Mug", 14.50m),
        new("grafana-socks", "Grafana Socks", 9.99m),
        new("blazor-sticker", "Blazor Sticker Pack", 4.20m),
        new("prom-tshirt", "PromQL T-Shirt", 24.00m),
    ];

    public static Product? Find(string id) => Products.FirstOrDefault(p => p.Id == id);
}

public static class ShopTelemetry
{
    public static readonly ActivitySource Source = new("NoBeard.Labs.Observability.Shop");
}

/// <summary>
/// Business metrics. Names follow OTel conventions and map to Prometheus like this:
///   demo.orders.placed               -> demo_orders_placed_total
///   demo.orders.revenue              -> demo_orders_revenue_total
///   demo.orders.failed               -> demo_orders_failed_total
///   demo.orders.processing.duration  -> demo_orders_processing_duration_seconds_bucket/_sum/_count
/// </summary>
public sealed class OrderMetrics
{
    public const string MeterName = "NoBeard.Labs.Observability.Orders";

    private readonly Counter<long> _placed;
    private readonly Counter<double> _revenue;
    private readonly Counter<long> _failed;
    private readonly Histogram<double> _duration;

    public OrderMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _placed = meter.CreateCounter<long>("demo.orders.placed", "{order}", "Orders successfully placed");
        _revenue = meter.CreateCounter<double>("demo.orders.revenue", "{EUR}", "Revenue of placed orders in EUR");
        _failed = meter.CreateCounter<long>("demo.orders.failed", "{order}", "Orders that failed");
        _duration = meter.CreateHistogram<double>("demo.orders.processing.duration", "s", "Time to process an order",
            advice: new InstrumentAdvice<double> { HistogramBucketBoundaries = [0.025, 0.05, 0.1, 0.25, 0.5, 1, 2.5, 5] });
    }

    public void Placed(Product product, int quantity, TimeSpan elapsed)
    {
        var tags = new TagList { { "product.id", product.Id } };
        _placed.Add(1, tags);
        _revenue.Add((double)(product.Price * quantity), tags);
        _duration.Record(elapsed.TotalSeconds, new TagList { { "outcome", "success" } });
    }

    public void Failed(string reason, TimeSpan elapsed)
    {
        _failed.Add(1, new TagList { { "error.type", reason } });
        _duration.Record(elapsed.TotalSeconds, new TagList { { "outcome", "failure" } });
    }
}

public sealed class PaymentDeclinedException(string message) : Exception(message);

/// <summary>
/// Places orders in-process. Each order is one span tree:
///   (Blazor "Event onclick", .NET 10) -> Place order -> Reserve inventory
///                                                    -> Charge payment
/// </summary>
public sealed class OrderService(OrderMetrics metrics, ILogger<OrderService> logger)
{
    public async Task<OrderResult> PlaceOrderAsync(string productId, int quantity, string origin, CancellationToken ct = default)
    {
        var started = Stopwatch.GetTimestamp();
        using var activity = ShopTelemetry.Source.StartActivity("Place order");
        activity?.SetTag("order.origin", origin);
        activity?.SetTag("order.product_id", productId);
        activity?.SetTag("order.quantity", quantity);

        var traceId = activity?.TraceId.ToString() ?? "";
        var product = Catalog.Find(productId);
        if (product is null)
        {
            logger.LogWarning("Unknown product {ProductId} ordered", productId);
            activity?.SetStatus(ActivityStatusCode.Error, "unknown product");
            return new(false, null, "unknown product", traceId, DateTimeOffset.Now);
        }

        try
        {
            await ReserveInventoryAsync(product, quantity, ct);
            await ChargePaymentAsync(product.Price * quantity, ct);

            var order = new OrderConfirmation($"ORD-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                product.Name, quantity, product.Price * quantity);

            activity?.SetTag("order.id", order.OrderId);
            metrics.Placed(product, quantity, Stopwatch.GetElapsedTime(started));
            logger.LogInformation("Order {OrderId} placed: {Quantity} x {ProductName} = {OrderTotal} EUR",
                order.OrderId, quantity, product.Name, order.Total);

            return new(true, order, null, traceId, DateTimeOffset.Now);
        }
        catch (PaymentDeclinedException ex)
        {
            activity?.AddException(ex);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            metrics.Failed("payment_declined", Stopwatch.GetElapsedTime(started));
            logger.LogError(ex, "Payment failed ({ProductName} x {Quantity})", product.Name, quantity);
            return new(false, null, "payment declined", traceId, DateTimeOffset.Now);
        }
    }

    private static async Task ReserveInventoryAsync(Product product, int quantity, CancellationToken ct)
    {
        using var activity = ShopTelemetry.Source.StartActivity("Reserve inventory");
        activity?.SetTag("product.id", product.Id);
        await Task.Delay(Random.Shared.Next(5, 40), ct);
        activity?.AddEvent(new ActivityEvent("inventory.reserved", tags: new() { ["quantity"] = quantity }));
    }

    private static async Task ChargePaymentAsync(decimal amount, CancellationToken ct)
    {
        using var activity = ShopTelemetry.Source.StartActivity("Charge payment", ActivityKind.Client);
        activity?.SetTag("peer.service", "payment-gateway");
        activity?.SetTag("payment.amount", (double)amount);

        await Task.Delay(Random.Shared.Next(20, 90), ct);

        // The payment gateway is a bit flaky: ~2% of payments fail, so there are always some errors to find.
        if (Random.Shared.NextDouble() < 0.02)
        {
            throw new PaymentDeclinedException("Payment gateway timeout");
        }
    }
}

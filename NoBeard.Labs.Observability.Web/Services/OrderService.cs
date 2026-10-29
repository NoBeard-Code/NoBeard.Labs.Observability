namespace NoBeard.Labs.Observability.Web.Services;

public record Product(string Id, string Name, decimal Price);
public record OrderConfirmation(string OrderId, string ProductName, int Quantity, decimal Total);
public record OrderResult(bool Success, OrderConfirmation? Order, string? Error, DateTimeOffset At);

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

public sealed class PaymentDeclinedException(string message) : Exception(message);

/// <summary>
/// Places orders in-process: reserve inventory, then charge the (simulated) payment gateway.
/// No custom telemetry yet - see what Aspire's service defaults show on their own.
/// </summary>
public sealed class OrderService
{
    public async Task<OrderResult> PlaceOrderAsync(string productId, int quantity, CancellationToken ct = default)
    {
        var product = Catalog.Find(productId);
        if (product is null)
        {
            return new(false, null, "unknown product", DateTimeOffset.Now);
        }

        try
        {
            await ReserveInventoryAsync(ct);
            await ChargePaymentAsync(ct);

            var order = new OrderConfirmation($"ORD-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                product.Name, quantity, product.Price * quantity);
            return new(true, order, null, DateTimeOffset.Now);
        }
        catch (PaymentDeclinedException)
        {
            return new(false, null, "payment declined", DateTimeOffset.Now);
        }
    }

    private static Task ReserveInventoryAsync(CancellationToken ct) => Task.Delay(Random.Shared.Next(5, 40), ct);

    private static async Task ChargePaymentAsync(CancellationToken ct)
    {
        await Task.Delay(Random.Shared.Next(20, 90), ct);

        // The payment gateway is a bit flaky: ~2% of payments fail.
        if (Random.Shared.NextDouble() < 0.02)
        {
            throw new PaymentDeclinedException("Payment gateway timeout");
        }
    }
}

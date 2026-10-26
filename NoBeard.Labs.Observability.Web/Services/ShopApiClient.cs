using System.Diagnostics;
using Polly.CircuitBreaker;
using Polly.Timeout;
using NoBeard.Labs.Observability.ServiceDefaults;

namespace NoBeard.Labs.Observability.Web.Services;

public record Product(string Id, string Name, decimal Price);
public record OrderConfirmation(string OrderId, string ProductName, int Quantity, decimal Total);
public record OrderResult(bool Success, OrderConfirmation? Order, int StatusCode, string TraceId, string Tenant, DateTimeOffset At, string? Error = null);
public record ChaosState(double FailureRate, int ExtraLatencyMs, string? Tenant);

public static class WebTelemetry
{
    public static readonly ActivitySource Source = new("NoBeard.Labs.Observability.Web");
}

/// <summary>Typed client for the API service. "https+http://apiservice" is resolved by Aspire service discovery.</summary>
public sealed class ShopApiClient(HttpClient http)
{
    public async Task<Product[]> GetProductsAsync(string tenant, CancellationToken ct = default)
    {
        using var activity = WebTelemetry.Source.StartActivity("Browse catalog");
        activity.SetTenant(tenant);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/products");
        request.Headers.Add(TenantTelemetry.HeaderName, tenant);
        using var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Product[]>(ct) ?? [];
    }

    public async Task<OrderResult> PlaceOrderAsync(string tenant, string productId, int quantity, string origin, CancellationToken ct = default)
    {
        // Root span of the business transaction. Tenant goes in as attribute + baggage, so the API,
        // Redis and every log line downstream are tagged with it automatically.
        using var activity = WebTelemetry.Source.StartActivity("Place order");
        activity.SetTenant(tenant);
        activity?.SetTag("order.origin", origin);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/orders")
        {
            Content = JsonContent.Create(new { productId, quantity }),
        };
        request.Headers.Add(TenantTelemetry.HeaderName, tenant);

        var traceId = activity?.TraceId.ToString() ?? "";
        try
        {
            using var response = await http.SendAsync(request, ct);
            if (response.IsSuccessStatusCode)
            {
                var order = await response.Content.ReadFromJsonAsync<OrderConfirmation>(ct);
                return new(true, order, (int)response.StatusCode, traceId, tenant, DateTimeOffset.Now);
            }

            activity?.SetStatus(ActivityStatusCode.Error, $"API returned {(int)response.StatusCode}");
            return new(false, null, (int)response.StatusCode, traceId, tenant, DateTimeOffset.Now);
        }
        // The standard resilience handler (ServiceDefaults) retries, times out and - once enough calls fail -
        // opens its circuit breaker. That breaker is shared by ALL tenants: one noisy tenant can trip it for everybody.
        catch (Exception ex) when (ex is HttpRequestException or TimeoutRejectedException or BrokenCircuitException
                                   or TaskCanceledException && !ct.IsCancellationRequested)
        {
            activity?.AddException(ex);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            var error = ex switch
            {
                BrokenCircuitException => "circuit open",
                TimeoutRejectedException or TaskCanceledException => "timeout",
                _ => "network",
            };
            return new(false, null, 0, traceId, tenant, DateTimeOffset.Now, error);
        }
    }

    public async Task<ChaosState> GetChaosAsync(CancellationToken ct = default) =>
        await http.GetFromJsonAsync<ChaosState>("/chaos", ct) ?? new(0, 0, null);

    public async Task<ChaosState> SetChaosAsync(ChaosState state, CancellationToken ct = default)
    {
        using var response = await http.PostAsJsonAsync("/chaos", state, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ChaosState>(ct) ?? state;
    }
}

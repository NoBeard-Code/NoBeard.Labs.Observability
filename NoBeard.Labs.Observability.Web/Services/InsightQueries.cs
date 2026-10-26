using NoBeard.Labs.Observability.ServiceDefaults;

namespace NoBeard.Labs.Observability.Web.Services;

/// <summary>
/// All PromQL used by the Live insights page, built server-side.
///
/// Multi-tenant rule of thumb: never let a tenant send PromQL. The tenant only picks from a known list,
/// and the server injects the tenant_id matcher into every query - so a tenant can't read another tenant's data.
/// (In Grafana Cloud you would back this up with label-based access policies on the read token.)
/// </summary>
public sealed class InsightQueries
{
    private readonly string _tenantMatcher;

    public InsightQueries(string? tenant)
    {
        if (tenant is not null && !TenantTelemetry.KnownTenants.Contains(tenant))
        {
            throw new ArgumentException($"Unknown tenant '{tenant}'", nameof(tenant));
        }

        Tenant = tenant;
        _tenantMatcher = tenant is null ? "" : $"tenant_id=\"{tenant}\"";
    }

    public string? Tenant { get; }

    private string M(string extra = "") =>
        "{" + string.Join(",", new[] { _tenantMatcher, extra }.Where(s => s.Length > 0)) + "}";

    // ---- KPI tiles -------------------------------------------------------------------------------------------
    public string OrdersPerMinute => $"sum(rate(demo_orders_placed_total{M()}[1m])) * 60";

    public string Revenue15m => $"sum(increase(demo_orders_revenue_total{M()}[15m]))";

    public string FailedPercent =>
        $"100 * (sum(rate(demo_orders_failed_total{M()}[2m])) or vector(0))" +
        $" / (sum(rate(demo_orders_placed_total{M()}[2m])) + (sum(rate(demo_orders_failed_total{M()}[2m])) or vector(0)))";

    public string P95ProcessingMs =>
        $"1000 * histogram_quantile(0.95, sum by (le) (rate(demo_orders_processing_duration_seconds_bucket{M()}[2m])))";

    // ---- Platform (not tenant data, operator view only) ------------------------------------------------------
    public const string ActiveCircuits = "sum(aspnetcore_components_circuit_active)";

    public const string ApiRequestsPerSecond =
        "sum(rate(http_server_request_duration_seconds_count{service_name=\"apiservice\",http_route=\"/orders\"}[1m]))";

    public const string Api5xxPercent =
        "100 * (sum(rate(http_server_request_duration_seconds_count{service_name=\"apiservice\",http_response_status_code=~\"5..\"}[1m])) or vector(0))" +
        " / sum(rate(http_server_request_duration_seconds_count{service_name=\"apiservice\"}[1m]))";

    // ---- Per-tenant breakdown --------------------------------------------------------------------------------
    public string OrdersPerMinuteByTenant => $"sum by (tenant_id) (rate(demo_orders_placed_total{M()}[1m])) * 60";
    public string FailuresPerMinuteByTenant => $"sum by (tenant_id) (rate(demo_orders_failed_total{M()}[1m])) * 60";
    public string RevenueByTenant => $"sum by (tenant_id) (increase(demo_orders_revenue_total{M()}[15m]))";

    public string P95ByTenant =>
        $"1000 * histogram_quantile(0.95, sum by (tenant_id, le) (rate(demo_orders_processing_duration_seconds_bucket{M()}[2m])))";
}

namespace NoBeard.Labs.Observability.Web.Services;

/// <summary>
/// All PromQL used by the Live insights page. Metric names are the Prometheus translation of the OTel instruments
/// (see OrderMetrics): dots become underscores, counters get _total, the unit "s" becomes _seconds.
/// </summary>
public static class InsightQueries
{
    // ---- KPI tiles -------------------------------------------------------------------------------------------
    public const string OrdersPerMinute = "sum(rate(demo_orders_placed_total[1m])) * 60";

    public const string Revenue15m = "sum(increase(demo_orders_revenue_total[15m]))";

    public const string FailedPercent =
        "100 * (sum(rate(demo_orders_failed_total[2m])) or vector(0))" +
        " / (sum(rate(demo_orders_placed_total[2m])) + (sum(rate(demo_orders_failed_total[2m])) or vector(0)))";

    public const string P95ProcessingMs =
        "1000 * histogram_quantile(0.95, sum by (le) (rate(demo_orders_processing_duration_seconds_bucket[2m])))";

    // ---- Platform: .NET 10 Blazor Server metrics -------------------------------------------------------------
    public const string ActiveCircuits = "sum(aspnetcore_components_circuit_active)";

    public const string EventHandlerP95Ms =
        "1000 * histogram_quantile(0.95, sum by (le) (rate(aspnetcore_components_handle_event_duration_seconds_bucket[2m])))";

    // ---- Charts ----------------------------------------------------------------------------------------------
    public const string OrdersPerMinuteByProduct = "sum by (product_id) (rate(demo_orders_placed_total[1m])) * 60";
    public const string FailuresPerMinute = "sum(rate(demo_orders_failed_total[1m])) * 60";
}

using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Logs;

namespace NoBeard.Labs.Observability.ServiceDefaults;

/// <summary>
/// Multi-tenancy in telemetry: the tenant travels between services as W3C baggage ("baggage: tenant.id=contoso")
/// and is stamped onto every span and log record, so Grafana can slice traces/logs per tenant.
/// Metrics get the tenant as an explicit tag at the instrument (see OrderMetrics) to keep cardinality under control.
/// </summary>
public static class TenantTelemetry
{
    public const string BaggageKey = "tenant.id";
    public const string AttributeName = "tenant.id";
    public const string HeaderName = "X-Tenant-Id";

    public static readonly string[] KnownTenants = ["contoso", "fabrikam", "northwind"];

    /// <summary>Puts the tenant on the activity as an attribute and as baggage that flows to downstream services.</summary>
    public static void SetTenant(this Activity? activity, string tenant)
    {
        activity?.SetTag(AttributeName, tenant);
        activity?.SetBaggage(BaggageKey, tenant);
    }

    public static string? CurrentTenant => Activity.Current?.GetBaggageItem(BaggageKey);
}

/// <summary>Copies the tenant baggage onto each span (incl. auto-instrumented HTTP and Redis spans).</summary>
internal sealed class TenantSpanProcessor : BaseProcessor<Activity>
{
    public override void OnStart(Activity activity)
    {
        // GetBaggageItem walks up the parent chain, so child spans inherit the tenant.
        if (activity.GetBaggageItem(TenantTelemetry.BaggageKey) is { } tenant)
        {
            activity.SetTag(TenantTelemetry.AttributeName, tenant);
        }
    }
}

/// <summary>Adds tenant.id to every log record written while a tenant-scoped activity is current.</summary>
internal sealed class TenantLogProcessor : BaseProcessor<LogRecord>
{
    public override void OnEnd(LogRecord data)
    {
        if (TenantTelemetry.CurrentTenant is { } tenant)
        {
            data.Attributes = [.. data.Attributes ?? [], new(TenantTelemetry.AttributeName, tenant)];
        }
    }
}

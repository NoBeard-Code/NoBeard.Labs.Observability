using System.Text.Json;
using NoBeard.Labs.Observability.ServiceDefaults;

namespace NoBeard.Labs.Observability.Web.Services;

/// <summary>
/// The tenant the current user acts as. Registered as scoped, which in Blazor Server means one instance per circuit
/// (browser tab) - so two tabs can impersonate two different tenants side by side during the demo.
/// </summary>
public sealed class TenantState
{
    public string Current { get; private set; } = TenantTelemetry.KnownTenants[0];

    public event Action? Changed;

    public void Set(string tenant)
    {
        if (!TenantTelemetry.KnownTenants.Contains(tenant) || tenant == Current) return;
        Current = tenant;
        Changed?.Invoke();
    }
}

public sealed class GrafanaOptions
{
    /// <summary>Grafana UI base URL - local LGTM container or https://&lt;stack&gt;.grafana.net</summary>
    public string? Url { get; set; }

    /// <summary>UID of the Tempo data source used for "open trace" links.</summary>
    public string TempoDatasourceUid { get; set; } = "tempo";

    public string? TraceUrl(string traceId)
    {
        if (string.IsNullOrWhiteSpace(Url) || string.IsNullOrEmpty(traceId)) return null;

        var panes = JsonSerializer.Serialize(new
        {
            t = new
            {
                datasource = TempoDatasourceUid,
                queries = new[] { new { refId = "A", datasource = new { type = "tempo", uid = TempoDatasourceUid }, queryType = "traceql", query = traceId } },
                range = new { from = "now-1h", to = "now" },
            },
        });
        return $"{Url.TrimEnd('/')}/explore?schemaVersion=1&panes={Uri.EscapeDataString(panes)}";
    }
}

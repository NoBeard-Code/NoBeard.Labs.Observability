using System.Text.Json;

namespace NoBeard.Labs.Observability.Web.Services;

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

using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using OpenTelemetry;

namespace NoBeard.Labs.Observability.Web.Services;

public sealed class PrometheusOptions
{
    /// <summary>
    /// Local LGTM: http://localhost:&lt;port&gt; (injected by the AppHost).
    /// Grafana Cloud: https://prometheus-prod-XX-prod-REGION.grafana.net/api/prom
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>Grafana Cloud: the Prometheus/Mimir instance (user) id. Empty for local.</summary>
    public string? Username { get; set; }

    /// <summary>Grafana Cloud: an access policy token with the metrics:read scope. Empty for local.</summary>
    public string? Password { get; set; }

    public string SourceName { get; set; } = "Prometheus";
}

public sealed record PromSample(IReadOnlyDictionary<string, string> Labels, double Value);
public sealed record PromPoint(DateTimeOffset Time, double Value);
public sealed record PromSeries(IReadOnlyDictionary<string, string> Labels, IReadOnlyList<PromPoint> Points);

/// <summary>
/// Minimal client for the Prometheus HTTP API (/api/v1/query and /api/v1/query_range).
/// The same API is served by Prometheus, Grafana Mimir and Grafana Cloud Metrics, so only BaseUrl + auth differ.
/// </summary>
public sealed class PrometheusClient(HttpClient http, IOptions<PrometheusOptions> options)
{
    public static void Configure(IServiceProvider services, HttpClient client)
    {
        var o = services.GetRequiredService<IOptions<PrometheusOptions>>().Value;
        if (string.IsNullOrWhiteSpace(o.BaseUrl)) return;

        client.BaseAddress = new Uri(o.BaseUrl.TrimEnd('/') + "/");
        if (!string.IsNullOrEmpty(o.Username))
        {
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{o.Username}:{o.Password}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        }
    }

    public bool IsConfigured => http.BaseAddress is not null;
    public string SourceName => options.Value.SourceName;
    public string? BaseUrl => http.BaseAddress?.ToString();

    public async Task<IReadOnlyList<PromSample>> QueryAsync(string promql, CancellationToken ct = default)
    {
        var data = await GetDataAsync($"api/v1/query?query={Uri.EscapeDataString(promql)}", ct);

        return data.GetProperty("resultType").GetString() switch
        {
            "vector" => [.. data.GetProperty("result").EnumerateArray()
                .Select(r => new PromSample(ReadLabels(r), ReadValue(r.GetProperty("value"))))],
            "scalar" => [new PromSample(new Dictionary<string, string>(), ReadValue(data.GetProperty("result")))],
            _ => [],
        };
    }

    public async Task<IReadOnlyList<PromSeries>> QueryRangeAsync(string promql, TimeSpan range, TimeSpan step, CancellationToken ct = default)
    {
        var end = DateTimeOffset.UtcNow;
        var url = $"api/v1/query_range?query={Uri.EscapeDataString(promql)}" +
                  $"&start={(end - range).ToUnixTimeSeconds()}&end={end.ToUnixTimeSeconds()}&step={(int)step.TotalSeconds}";
        var data = await GetDataAsync(url, ct);

        return [.. data.GetProperty("result").EnumerateArray().Select(r => new PromSeries(
            ReadLabels(r),
            [.. r.GetProperty("values").EnumerateArray().Select(v => new PromPoint(
                DateTimeOffset.FromUnixTimeMilliseconds((long)(v[0].GetDouble() * 1000)), ReadValue(v)))]))];
    }

    private async Task<JsonElement> GetDataAsync(string relativeUrl, CancellationToken ct)
    {
        // Don't observe the observer: the dashboard polls every few seconds, and tracing those calls
        // would flood Tempo with spans about reading metrics.
        using var _ = SuppressInstrumentationScope.Begin();

        using var response = await http.GetAsync(relativeUrl, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        if (root.GetProperty("status").GetString() != "success")
        {
            var error = root.TryGetProperty("error", out var e) ? e.GetString() : body;
            throw new InvalidOperationException($"Prometheus query failed: {error}");
        }

        return root.GetProperty("data").Clone();
    }

    private static Dictionary<string, string> ReadLabels(JsonElement result) =>
        result.GetProperty("metric").EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString() ?? "");

    // Samples come as [ <unix seconds>, "<value as string>" ]; the value can be "NaN" or "+Inf".
    private static double ReadValue(JsonElement pair) =>
        double.Parse(pair[1].GetString()!, NumberStyles.Float, CultureInfo.InvariantCulture);
}

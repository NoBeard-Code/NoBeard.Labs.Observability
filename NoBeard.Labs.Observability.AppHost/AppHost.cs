var builder = DistributedApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------------------------------------------
// Observability backend #1 (always on): grafana/otel-lgtm = Grafana + Prometheus + Loki + Tempo in one container.
// Works offline - the safety net for a conference Wi-Fi.
// ---------------------------------------------------------------------------------------------------------------
// Docker names get the "nobeard-demo-" prefix so they never clash with other local containers/volumes.
var lgtm = builder.AddContainer("grafana", "grafana/otel-lgtm", "0.38.0")
    .WithContainerName("nobeard-demo-grafana")
    .WithHttpEndpoint(port: 3000, targetPort: 3000, name: "http")
    .WithHttpEndpoint(targetPort: 9090, name: "prometheus")
    .WithEndpoint(targetPort: 4318, name: "otlp-http", scheme: "http")
    // Provision the demo dashboard next to the built-in ones.
    .WithBindMount("../grafana/dashboards", "/otel-lgtm/demo-dashboards", isReadOnly: true)
    .WithBindMount("../grafana/provisioning/demo-dashboards.yaml",
        "/otel-lgtm/grafana/conf/provisioning/dashboards/demo-dashboards.yaml", isReadOnly: true)
    // Keep metrics/logs/traces between runs (rehearsal data survives a restart).
    .WithVolume("nobeard-demo-lgtm-data", "/data")
    .WithHttpHealthCheck("/api/health", endpointName: "http")
    .WithUrlForEndpoint("http", u => u.DisplayText = "Grafana")
    .WithUrlForEndpoint("prometheus", u => u.DisplayText = "Prometheus")
    .WithUrlForEndpoint("otlp-http", u => u.DisplayLocation = UrlDisplayLocation.DetailsOnly);

// ---------------------------------------------------------------------------------------------------------------
// Observability backend #2 (optional): Grafana Cloud. Switched on by the "https-cloud" launch profile
// (GrafanaCloud__Enabled=true); credentials come from user secrets (see README). Alloy then fans out to LGTM
// *and* Grafana Cloud. Pick the plain "https" profile to fall back to local-only without touching the secrets.
// ---------------------------------------------------------------------------------------------------------------
var grafanaCloudEnabled = bool.TryParse(builder.Configuration["GrafanaCloud:Enabled"], out var cloud) && cloud;

// ---------------------------------------------------------------------------------------------------------------
// Grafana Alloy: the OTLP collector between the apps and the backends (batching, PII redaction, fan-out).
// ---------------------------------------------------------------------------------------------------------------
var alloy = builder.AddContainer("alloy", "grafana/alloy", "v1.21.2")
    .WithContainerName("nobeard-demo-alloy")
    .WithBindMount("../grafana/alloy", "/etc/alloy", isReadOnly: true)
    .WithArgs("run",
        "--server.http.listen-addr=0.0.0.0:12345",
        "--storage.path=/var/lib/alloy/data",
        grafanaCloudEnabled ? "/etc/alloy/cloud.alloy" : "/etc/alloy/local.alloy")
    .WithHttpEndpoint(targetPort: 12345, name: "http")
    .WithEndpoint(targetPort: 4317, name: "otlp-grpc", scheme: "http")
    .WithEnvironment("LGTM_OTLP_ENDPOINT", lgtm.GetEndpoint("otlp-http"))
    .WithUrlForEndpoint("http", u => u.DisplayText = "Alloy pipeline")
    .WithUrlForEndpoint("otlp-grpc", u => u.DisplayLocation = UrlDisplayLocation.DetailsOnly)
    .WaitFor(lgtm);

// ---------------------------------------------------------------------------------------------------------------
// The application
// ---------------------------------------------------------------------------------------------------------------
var web = builder.AddProject<Projects.NoBeard_Labs_Observability_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithEnvironment("ALLOY_OTLP_ENDPOINT", alloy.GetEndpoint("otlp-grpc"))
    .WaitFor(alloy);

if (grafanaCloudEnabled)
{
    var otlpEndpoint = builder.AddParameter("grafana-cloud-otlp-endpoint");
    var instanceId = builder.AddParameter("grafana-cloud-instance-id");
    var apiToken = builder.AddParameter("grafana-cloud-api-token", secret: true);
    var prometheusUrl = builder.AddParameter("grafana-cloud-prometheus-url");
    var prometheusUser = builder.AddParameter("grafana-cloud-prometheus-user");
    var grafanaUrl = builder.AddParameter("grafana-cloud-grafana-url");

    alloy.WithEnvironment("GRAFANA_CLOUD_OTLP_ENDPOINT", otlpEndpoint)
        .WithEnvironment("GRAFANA_CLOUD_INSTANCE_ID", instanceId)
        .WithEnvironment("GRAFANA_CLOUD_API_TOKEN", apiToken);

    // The Blazor "Live insights" page reads from Grafana Cloud Metrics (Mimir) via the Prometheus HTTP API.
    web.WithEnvironment("Prometheus__SourceName", "Grafana Cloud Metrics")
        .WithEnvironment("Prometheus__BaseUrl", prometheusUrl)
        .WithEnvironment("Prometheus__Username", prometheusUser)
        .WithEnvironment("Prometheus__Password", apiToken)
        .WithEnvironment("Grafana__Url", grafanaUrl)
        // Default UID of the Tempo data source in a Grafana Cloud stack; override in user secrets if yours differs.
        .WithEnvironment("Grafana__TempoDatasourceUid", builder.Configuration["GrafanaCloud:TempoDatasourceUid"] ?? "grafanacloud-traces");
}
else
{
    web.WithEnvironment("Prometheus__SourceName", "Local Prometheus (otel-lgtm)")
        .WithEnvironment("Prometheus__BaseUrl", lgtm.GetEndpoint("prometheus"))
        .WithEnvironment("Grafana__Url", lgtm.GetEndpoint("http"));
}

builder.Build().Run();

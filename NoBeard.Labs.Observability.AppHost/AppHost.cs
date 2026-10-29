var builder = DistributedApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------------------------------------------
// Observability backend: grafana/otel-lgtm = Grafana + Prometheus + Loki + Tempo in one container.
// ---------------------------------------------------------------------------------------------------------------
// Docker names get the "nobeard-demo-" prefix so they never clash with other local containers/volumes.
var lgtm = builder.AddContainer("grafana", "grafana/otel-lgtm", "0.38.0")
    .WithContainerName("nobeard-demo-grafana")
    .WithHttpEndpoint(port: 3000, targetPort: 3000, name: "http")
    .WithHttpEndpoint(targetPort: 9090, name: "prometheus")
    .WithEndpoint(targetPort: 4318, name: "otlp-http", scheme: "http")
    // Keep metrics/logs/traces between runs (rehearsal data survives a restart).
    .WithVolume("nobeard-demo-lgtm-data", "/data")
    .WithHttpHealthCheck("/api/health", endpointName: "http")
    .WithUrlForEndpoint("http", u => u.DisplayText = "Grafana")
    .WithUrlForEndpoint("prometheus", u => u.DisplayText = "Prometheus")
    .WithUrlForEndpoint("otlp-http", u => u.DisplayLocation = UrlDisplayLocation.DetailsOnly);

// ---------------------------------------------------------------------------------------------------------------
// Grafana Alloy: the OTLP collector between the apps and the backends (batching, PII redaction, fan-out).
// ---------------------------------------------------------------------------------------------------------------
var alloy = builder.AddContainer("alloy", "grafana/alloy", "v1.21.2")
    .WithContainerName("nobeard-demo-alloy")
    .WithBindMount("../grafana/alloy", "/etc/alloy", isReadOnly: true)
    .WithArgs("run",
        "--server.http.listen-addr=0.0.0.0:12345",
        "--storage.path=/var/lib/alloy/data",
        "/etc/alloy/local.alloy")
    .WithHttpEndpoint(targetPort: 12345, name: "http")
    .WithEndpoint(targetPort: 4317, name: "otlp-grpc", scheme: "http")
    .WithEnvironment("LGTM_OTLP_ENDPOINT", lgtm.GetEndpoint("otlp-http"))
    .WithUrlForEndpoint("http", u => u.DisplayText = "Alloy pipeline")
    .WithUrlForEndpoint("otlp-grpc", u => u.DisplayLocation = UrlDisplayLocation.DetailsOnly)
    .WaitFor(lgtm);

// ---------------------------------------------------------------------------------------------------------------
// The application
// ---------------------------------------------------------------------------------------------------------------
builder.AddProject<Projects.NoBeard_Labs_Observability_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithEnvironment("ALLOY_OTLP_ENDPOINT", alloy.GetEndpoint("otlp-grpc"))
    .WaitFor(alloy)
    .WithEnvironment("Grafana__Url", lgtm.GetEndpoint("http"));

builder.Build().Run();

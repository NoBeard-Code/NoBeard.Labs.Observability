var builder = DistributedApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------------------------------------------
// Observability backend: grafana/otel-lgtm = Grafana + Prometheus + Loki + Tempo in one container.
// ---------------------------------------------------------------------------------------------------------------
// Docker names get the "nobeard-demo-" prefix so they never clash with other local containers/volumes.
var lgtm = builder.AddContainer("grafana", "grafana/otel-lgtm", "0.38.0")
    .WithContainerName("nobeard-demo-grafana")
    .WithHttpEndpoint(port: 3000, targetPort: 3000, name: "http")
    .WithHttpEndpoint(targetPort: 9090, name: "prometheus")
    .WithEndpoint(targetPort: 4317, name: "otlp-grpc", scheme: "http")
    // Keep metrics/logs/traces between runs (rehearsal data survives a restart).
    .WithVolume("nobeard-demo-lgtm-data", "/data")
    .WithHttpHealthCheck("/api/health", endpointName: "http")
    .WithUrlForEndpoint("http", u => u.DisplayText = "Grafana")
    .WithUrlForEndpoint("prometheus", u => u.DisplayText = "Prometheus")
    .WithUrlForEndpoint("otlp-grpc", u => u.DisplayLocation = UrlDisplayLocation.DetailsOnly);

// ---------------------------------------------------------------------------------------------------------------
// The application: exports OTLP to the Aspire dashboard (automatic) and directly to Grafana.
// ---------------------------------------------------------------------------------------------------------------
builder.AddProject<Projects.NoBeard_Labs_Observability_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithEnvironment("GRAFANA_OTLP_ENDPOINT", lgtm.GetEndpoint("otlp-grpc"))
    .WaitFor(lgtm)
    .WithEnvironment("Grafana__Url", lgtm.GetEndpoint("http"));

builder.Build().Run();

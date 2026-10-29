var builder = DistributedApplication.CreateBuilder(args);

// The application. The AppHost injects the OTEL_* environment variables, so all telemetry lands in the Aspire dashboard.
builder.AddProject<Projects.NoBeard_Labs_Observability_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health");

builder.Build().Run();

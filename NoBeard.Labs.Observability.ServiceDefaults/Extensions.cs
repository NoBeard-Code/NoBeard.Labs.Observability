using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

// Adds common Aspire services: service discovery, resilience, health checks, and OpenTelemetry.
// This project should be referenced by each service project in your solution.
// To learn more about using this project, see https://aka.ms/dotnet/aspire/service-defaults
public static class Extensions
{
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";

    // Every custom ActivitySource / Meter in the demo starts with this prefix, so one wildcard picks them all up.
    public const string DemoTelemetryPrefix = "NoBeard.Labs.Observability.*";

    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Turn on resilience by default.
            // SelectPipelineByAuthority: one pipeline (and circuit breaker) per destination host. Without it, all
            // HttpClients configured here share ONE breaker, so a single failing dependency would cut off the others.
            http.AddStandardResilienceHandler().SelectPipelineByAuthority();

            // Turn on service discovery by default
            http.AddServiceDiscovery();
        });

        return builder;
    }

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        var exporters = new OtlpTargets(builder.Configuration);

        builder.Services.AddOpenTelemetry()
            // Aspire already sets service.name and service.instance.id via OTEL_* env vars.
            // service.namespace ends up as the "job" label prefix in Prometheus: observability-demo/<service>.
            .ConfigureResource(resource => resource.AddAttributes(
            [
                new("service.namespace", "observability-demo"),
                new("deployment.environment.name", builder.Environment.EnvironmentName.ToLowerInvariant()),
            ]))
            .WithLogging(
                logging => exporters.AddTo(logging),
                options =>
                {
                    options.IncludeFormattedMessage = true;
                    options.IncludeScopes = true;
                })
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    // .NET 10: Blazor Server circuit + component metrics
                    .AddMeter("Microsoft.AspNetCore.Components")
                    .AddMeter("Microsoft.AspNetCore.Components.Lifecycle")
                    .AddMeter("Microsoft.AspNetCore.Components.Server.Circuits")
                    .AddMeter(DemoTelemetryPrefix)
                    // Attach trace ids to histogram samples -> jump from a Grafana metric panel straight to the trace.
                    .SetExemplarFilter(ExemplarFilterType.TraceBased);

                exporters.AddTo(metrics);
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource(builder.Environment.ApplicationName)
                    .AddSource(DemoTelemetryPrefix)
                    // .NET 10: Blazor Server circuit, navigation and event-handler spans
                    .AddSource("Microsoft.AspNetCore.Components")
                    .AddSource("Microsoft.AspNetCore.Components.Server.Circuits")
                    .AddAspNetCoreInstrumentation(tracing =>
                        // Exclude health check requests from tracing
                        tracing.Filter = context =>
                            !context.Request.Path.StartsWithSegments(HealthEndpointPath)
                            && !context.Request.Path.StartsWithSegments(AlivenessEndpointPath)
                    )
                    .AddHttpClientInstrumentation();

                exporters.AddTo(tracing);
            });

        return builder;
    }

    /// <summary>
    /// Fan-out of OTLP exporters. Each signal is exported to:
    ///  * the Aspire dashboard (OTEL_EXPORTER_OTLP_* env vars injected by the AppHost) - developer inner loop
    ///  * Grafana Alloy (ALLOY_OTLP_ENDPOINT) - the collector that ships to local Grafana and Grafana Cloud
    /// Signal-specific named exporters are used because UseOtlpExporter() supports only a single destination.
    /// </summary>
    private sealed class OtlpTargets(IConfiguration configuration)
    {
        private readonly bool _aspireDashboard = !string.IsNullOrWhiteSpace(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
        private readonly string? _alloyEndpoint = configuration["ALLOY_OTLP_ENDPOINT"];

        public void AddTo(LoggerProviderBuilder logging)
        {
            if (_aspireDashboard) logging.AddOtlpExporter("aspire", _ => { });
            if (_alloyEndpoint is not null) logging.AddOtlpExporter("alloy", ConfigureAlloy);
        }

        public void AddTo(MeterProviderBuilder metrics)
        {
            if (_aspireDashboard) metrics.AddOtlpExporter("aspire", _ => { });
            if (_alloyEndpoint is not null)
            {
                metrics.AddOtlpExporter("alloy", (exporter, reader) =>
                {
                    ConfigureAlloy(exporter);
                    // Aspire pushes metrics every second for a snappy dashboard. Grafana Cloud bills per
                    // data point per minute (DPM), so the Alloy/Grafana path uses a 10s interval instead.
                    reader.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 10_000;
                });
            }
        }

        public void AddTo(TracerProviderBuilder tracing)
        {
            if (_aspireDashboard) tracing.AddOtlpExporter("aspire", _ => { });
            if (_alloyEndpoint is not null) tracing.AddOtlpExporter("alloy", ConfigureAlloy);
        }

        private void ConfigureAlloy(OtlpExporterOptions options)
        {
            options.Endpoint = new Uri(_alloyEndpoint!);
            options.Protocol = OtlpExportProtocol.Grpc;
            // Don't forward the Aspire dashboard API key (read from OTEL_EXPORTER_OTLP_HEADERS) to Alloy.
            options.Headers = null;
        }
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // Adding health checks endpoints to applications in non-development environments has security implications.
        // See https://aka.ms/dotnet/aspire/healthchecks for details before enabling these endpoints in non-development environments.
        if (app.Environment.IsDevelopment())
        {
            // All health checks must pass for app to be considered ready to accept traffic after starting
            app.MapHealthChecks(HealthEndpointPath);

            // Only health checks tagged with the "live" tag must pass for app to be considered alive
            app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("live")
            });
        }

        return app;
    }
}

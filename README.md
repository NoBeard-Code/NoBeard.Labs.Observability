# NoBeard.Labs.Observability - step 06: Read metrics back via the Prometheus HTTP API in Blazor

Companion code for the session **.NET Observability End-to-End: OpenTelemetry with Aspire and Grafana Cloud**.

## What's new in this step

* `PrometheusClient`: a minimal client for the **Prometheus HTTP API** (`/api/v1/query`, `/api/v1/query_range`) - the same API Grafana Cloud (Mimir) serves.
* **Live insights** page: KPI tiles, Blazor platform metrics, SVG charts and a PromQL console - refreshed every 5 s on the server and pushed over the Blazor circuit.
* `SuppressInstrumentationScope`: don't trace the dashboard's own polling (*don't observe the observer*).
* `SelectPipelineByAuthority()` for the standard resilience handler: one circuit breaker per destination host.

## Try it

1. Live insights → numbers match the Grafana dashboard. Turn on chaos → tiles turn red within seconds.
2. PromQL console → try `topk(5, sum by (__name__) (count by (__name__) ({service_namespace="observability-demo"})))`.

## Look at

* `NoBeard.Labs.Observability.Web/Services/PrometheusClient.cs`
* `NoBeard.Labs.Observability.Web/Services/InsightQueries.cs`
* `NoBeard.Labs.Observability.Web/Components/Pages/Insights.razor`

## Run it

Visual Studio 2026 (or 2022 17.14+) with .NET 10 and Docker Desktop running: open `NoBeard.Labs.Observability.slnx`,
set **NoBeard.Labs.Observability.AppHost** as startup project, **F5**. Or from a terminal:
`dotnet run --project NoBeard.Labs.Observability.AppHost --launch-profile http`.

## Learning path

Each step is one branch building on the previous one. See exactly what a step adds with `git diff <previous> <step>`, e.g. `git diff 03-local-grafana 04-alloy`.

| Step | Branch | What it adds |
|---|---|---|
| 01 | [`01-init`](../../tree/01-init) | Aspire + a plain Blazor Server shop |
| 02 | [`02-custom-telemetry`](../../tree/02-custom-telemetry) | Own spans, metrics and logs + .NET 10 Blazor telemetry |
| 03 | [`03-local-grafana`](../../tree/03-local-grafana) | Send the same telemetry to a local Grafana (otel-lgtm) |
| 04 | [`04-alloy`](../../tree/04-alloy) | Put Grafana Alloy (collector) in the middle |
| 05 | [`05-dashboards-chaos`](../../tree/05-dashboards-chaos) | Provisioned dashboard, exemplars and chaos |
| 06 | [`06-prometheus-api`](../../tree/06-prometheus-api) | Read metrics back via the Prometheus HTTP API in Blazor **← you are here** |
| 07 | [`07-grafana-cloud`](../../tree/07-grafana-cloud) | Fan out to Grafana Cloud with a launch profile |
| 08 | [`08-multi-tenant`](../../tree/08-multi-tenant) | Tenants end to end: telemetry and data access |
| — | [`main`](../../tree/main) | = step 08 + presenter README (demo script, stage checklist) |

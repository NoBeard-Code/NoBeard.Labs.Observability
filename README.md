# NoBeard.Labs.Observability - step 03: Send the same telemetry to a local Grafana (otel-lgtm)

Companion code for the session **.NET Observability End-to-End: OpenTelemetry with Aspire and Grafana Cloud**.

## What's new in this step

* **grafana/otel-lgtm** container in the AppHost (Grafana + Prometheus + Loki + Tempo), container `nobeard-demo-grafana`, data in volume `nobeard-demo-lgtm-data`, Grafana pinned to http://localhost:3000.
* A second destination: `UseOtlpExporter()` (one target) is replaced by **named OTLP exporters per signal** - `aspire` (dashboard) and `grafana` (`GRAFANA_OTLP_ENDPOINT`).
* Trace ids in the shop link straight into **Grafana Explore → Tempo**; *Open Grafana* link in the header.

## Try it

1. Wait until `grafana` is *Running* (first start ≈20 s) → *Open Grafana*.
2. Explore → **Tempo** → Search → service `webfrontend`; Explore → **Loki** `{service_name="webfrontend"}`; Explore → **Prometheus** `demo_orders_placed_total`.
3. Shop → buy → click the trace id → the same trace, now in Tempo. OTLP is vendor-neutral: same app code, a second backend.

## Look at

* `NoBeard.Labs.Observability.AppHost/AppHost.cs`
* `NoBeard.Labs.Observability.ServiceDefaults/Extensions.cs` (`OtlpTargets`)
* `NoBeard.Labs.Observability.Web/Services/GrafanaOptions.cs`

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
| 03 | [`03-local-grafana`](../../tree/03-local-grafana) | Send the same telemetry to a local Grafana (otel-lgtm) **← you are here** |
| 04 | [`04-alloy`](../../tree/04-alloy) | Put Grafana Alloy (collector) in the middle |
| 05 | [`05-dashboards-chaos`](../../tree/05-dashboards-chaos) | Provisioned dashboard, exemplars and chaos |
| 06 | [`06-prometheus-api`](../../tree/06-prometheus-api) | Read metrics back via the Prometheus HTTP API in Blazor |
| 07 | [`07-grafana-cloud`](../../tree/07-grafana-cloud) | Fan out to Grafana Cloud with a launch profile |
| 08 | [`08-multi-tenant`](../../tree/08-multi-tenant) | Tenants end to end: telemetry and data access |
| — | [`main`](../../tree/main) | = step 08 + presenter README (demo script, stage checklist) |

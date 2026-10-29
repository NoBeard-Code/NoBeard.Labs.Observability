# NoBeard.Labs.Observability - step 04: Put Grafana Alloy (collector) in the middle

Companion code for the session **.NET Observability End-to-End: OpenTelemetry with Aspire and Grafana Cloud**.

## What's new in this step

* **Grafana Alloy** container (`nobeard-demo-alloy`) between the app and Grafana: OTLP receiver → **PII redaction** → **batch** → exporter ([`grafana/alloy/local.alloy`](grafana/alloy/local.alloy)).
* The app change is tiny: the `grafana` exporter becomes `alloy` (`ALLOY_OTLP_ENDPOINT`). Everything else moves into collector config.
* Metrics to Alloy every **10 s** (the Aspire dashboard keeps 1 s) - the setting that matters for cost once a SaaS backend is involved.

## Try it

1. Aspire dashboard → `alloy` → **Alloy pipeline** → *Graph*: live flow rates per signal between the components.
2. Everything in Grafana still works - now via the collector.
3. `git diff 03-local-grafana 04-alloy` - see how little the app itself changed.

## Look at

* `grafana/alloy/local.alloy`
* `NoBeard.Labs.Observability.AppHost/AppHost.cs`
* `NoBeard.Labs.Observability.ServiceDefaults/Extensions.cs`

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
| 04 | [`04-alloy`](../../tree/04-alloy) | Put Grafana Alloy (collector) in the middle **← you are here** |
| 05 | [`05-dashboards-chaos`](../../tree/05-dashboards-chaos) | Provisioned dashboard, exemplars and chaos |
| 06 | [`06-prometheus-api`](../../tree/06-prometheus-api) | Read metrics back via the Prometheus HTTP API in Blazor |
| 07 | [`07-grafana-cloud`](../../tree/07-grafana-cloud) | Fan out to Grafana Cloud with a launch profile |
| 08 | [`08-multi-tenant`](../../tree/08-multi-tenant) | Tenants end to end: telemetry and data access |
| — | [`main`](../../tree/main) | = step 08 + presenter README (demo script, stage checklist) |

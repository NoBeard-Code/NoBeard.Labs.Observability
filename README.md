# NoBeard.Labs.Observability - step 01: Aspire + a plain Blazor Server shop

Companion code for the session **.NET Observability End-to-End: OpenTelemetry with Aspire and Grafana Cloud**.

## What's new in this step

* Aspire **AppHost** orchestrating one project: `webfrontend`, a **Blazor Server** app (.NET 10, no WebAssembly).
* Aspire **ServiceDefaults** straight from the template: OpenTelemetry for ASP.NET Core, HttpClient and runtime, exported via OTLP to the **Aspire dashboard**.
* A small **Shop** (in-memory catalog, *Buy* button) whose orders sometimes fail (~2% flaky payment gateway).

## Try it

1. F5 → the Aspire dashboard opens → click the `webfrontend` URL → **Shop** → buy a few items.
2. Aspire dashboard → **Traces**: the Blazor Server SignalR traffic (`/_blazor`) - but nothing about *orders*.
3. **Metrics** → `webfrontend`: ASP.NET Core / Kestrel / runtime meters. **Structured logs**: framework logs only.
4. Question for the next step: *why did that order fail, and how long did the payment take?* - you can't tell yet.

## Look at

* `NoBeard.Labs.Observability.AppHost/AppHost.cs`
* `NoBeard.Labs.Observability.ServiceDefaults/Extensions.cs`
* `NoBeard.Labs.Observability.Web/Services/OrderService.cs`

## Run it

Visual Studio 2026 (or 2022 17.14+) with .NET 10 and Docker Desktop running: open `NoBeard.Labs.Observability.slnx`,
set **NoBeard.Labs.Observability.AppHost** as startup project, **F5**. Or from a terminal:
`dotnet run --project NoBeard.Labs.Observability.AppHost --launch-profile http`.

## Learning path

Each step is one branch building on the previous one. See exactly what a step adds with `git diff <previous> <step>`, e.g. `git diff 03-local-grafana 04-alloy`.

| Step | Branch | What it adds |
|---|---|---|
| 01 | [`01-init`](../../tree/01-init) | Aspire + a plain Blazor Server shop **← you are here** |
| 02 | [`02-custom-telemetry`](../../tree/02-custom-telemetry) | Own spans, metrics and logs + .NET 10 Blazor telemetry |
| 03 | [`03-local-grafana`](../../tree/03-local-grafana) | Send the same telemetry to a local Grafana (otel-lgtm) |
| 04 | [`04-alloy`](../../tree/04-alloy) | Put Grafana Alloy (collector) in the middle |
| 05 | [`05-dashboards-chaos`](../../tree/05-dashboards-chaos) | Provisioned dashboard, exemplars and chaos |
| 06 | [`06-prometheus-api`](../../tree/06-prometheus-api) | Read metrics back via the Prometheus HTTP API in Blazor |
| 07 | [`07-grafana-cloud`](../../tree/07-grafana-cloud) | Fan out to Grafana Cloud with a launch profile |
| 08 | [`08-multi-tenant`](../../tree/08-multi-tenant) | Tenants end to end: telemetry and data access |
| — | [`main`](../../tree/main) | = step 08 + presenter README (demo script, stage checklist) |

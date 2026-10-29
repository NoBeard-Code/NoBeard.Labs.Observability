# NoBeard.Labs.Observability - step 02: Own spans, metrics and logs + .NET 10 Blazor telemetry

Companion code for the session **.NET Observability End-to-End: OpenTelemetry with Aspire and Grafana Cloud**.

## What's new in this step

* Own **spans** via `ActivitySource` (`NoBeard.Labs.Observability.Shop`): `Place order` → `Reserve inventory` / `Charge payment`, with attributes, events, error status and the exception.
* Own **metrics** via `IMeterFactory`: `demo.orders.placed`, `.failed`, `.revenue` and the `demo.orders.processing.duration` histogram.
* **Structured logs** with message templates (`Order {OrderId} placed ...`) - automatically correlated with the trace.
* **.NET 10 Blazor telemetry**: `Microsoft.AspNetCore.Components*` sources and meters, and global `InteractiveServer` - a click becomes the **root span** (`Event onclick`).
* A background **traffic generator** (2 orders/s), so there is always data. *Burst ×20* button in the shop.
* One wildcard (`NoBeard.Labs.Observability.*`) registers all demo sources/meters; `service.namespace` resource attribute.

## Try it

1. Shop → *Buy* → Aspire dashboard → **Traces** → the `Event onclick` trace: one click, one tree of spans.
2. *Burst ×20* → one trace with 20 `Place order` children. Find a failed one (red) and look at its exception.
3. **Metrics** → `NoBeard.Labs.Observability.Orders`; `Microsoft.AspNetCore.Components.Server.Circuits` → active circuits.
4. **Structured logs** → filter `OrderId` - each log line links to its trace.

## Look at

* `NoBeard.Labs.Observability.Web/Services/OrderService.cs`
* `NoBeard.Labs.Observability.ServiceDefaults/Extensions.cs`
* `NoBeard.Labs.Observability.Web/Services/TrafficGenerator.cs`
* `NoBeard.Labs.Observability.Web/Components/App.razor`

## Run it

Visual Studio 2026 (or 2022 17.14+) with .NET 10 and Docker Desktop running: open `NoBeard.Labs.Observability.slnx`,
set **NoBeard.Labs.Observability.AppHost** as startup project, **F5**. Or from a terminal:
`dotnet run --project NoBeard.Labs.Observability.AppHost --launch-profile http`.

## Learning path

Each step is one branch building on the previous one. See exactly what a step adds with `git diff <previous> <step>`, e.g. `git diff 03-local-grafana 04-alloy`.

| Step | Branch | What it adds |
|---|---|---|
| 01 | [`01-init`](../../tree/01-init) | Aspire + a plain Blazor Server shop |
| 02 | [`02-custom-telemetry`](../../tree/02-custom-telemetry) | Own spans, metrics and logs + .NET 10 Blazor telemetry **← you are here** |
| 03 | [`03-local-grafana`](../../tree/03-local-grafana) | Send the same telemetry to a local Grafana (otel-lgtm) |
| 04 | [`04-alloy`](../../tree/04-alloy) | Put Grafana Alloy (collector) in the middle |
| 05 | [`05-dashboards-chaos`](../../tree/05-dashboards-chaos) | Provisioned dashboard, exemplars and chaos |
| 06 | [`06-prometheus-api`](../../tree/06-prometheus-api) | Read metrics back via the Prometheus HTTP API in Blazor |
| 07 | [`07-grafana-cloud`](../../tree/07-grafana-cloud) | Fan out to Grafana Cloud with a launch profile |
| 08 | [`08-multi-tenant`](../../tree/08-multi-tenant) | Tenants end to end: telemetry and data access |
| — | [`main`](../../tree/main) | = step 08 + presenter README (demo script, stage checklist) |

# NoBeard.Labs.Observability - step 05: Provisioned dashboard, exemplars and chaos

Companion code for the session **.NET Observability End-to-End: OpenTelemetry with Aspire and Grafana Cloud**.

## What's new in this step

* Provisioned Grafana dashboard **.NET O11y Demo** (folder *NoBeard Labs*): business stats, orders by product, failures by error type, p95, Blazor platform panels, logs and failed traces ([`grafana/dashboards`](grafana/dashboards)).
* **Exemplars** (`SetExemplarFilter(TraceBased)`): the p95 panel shows dots that jump to the exact trace.
* **Traffic & chaos** page: orders/s slider, payment-gateway failure rate and extra latency at runtime.

## Try it

1. Grafana → Dashboards → *NoBeard Labs* → **.NET O11y Demo**.
2. Traffic & chaos → 40% failures + 1500 ms → *Apply* → watch *Failed orders %* and p95 climb.
3. p95 panel → click an exemplar → the slow trace → *Logs for this span*.

## Look at

* `grafana/dashboards/nobeard-o11y-demo.json`
* `grafana/provisioning/demo-dashboards.yaml`
* `NoBeard.Labs.Observability.Web/Components/Pages/Control.razor`
* `NoBeard.Labs.Observability.Web/Services/OrderService.cs` (`ChaosSettings`)

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
| 05 | [`05-dashboards-chaos`](../../tree/05-dashboards-chaos) | Provisioned dashboard, exemplars and chaos **← you are here** |
| 06 | [`06-prometheus-api`](../../tree/06-prometheus-api) | Read metrics back via the Prometheus HTTP API in Blazor |
| 07 | [`07-grafana-cloud`](../../tree/07-grafana-cloud) | Fan out to Grafana Cloud with a launch profile |
| 08 | [`08-multi-tenant`](../../tree/08-multi-tenant) | Tenants end to end: telemetry and data access |
| — | [`main`](../../tree/main) | = step 08 + presenter README (demo script, stage checklist) |

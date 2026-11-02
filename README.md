# NoBeard.Labs.Observability - step 08: Tenants end to end: telemetry and data access

Companion code for the session **.NET Observability End-to-End: OpenTelemetry with Aspire and Grafana Cloud**.

## What's new in this step

* Three tenants (`contoso`, `fabrikam`, `northwind`): a **tenant picker** per Blazor circuit (two tabs = two tenants) and a weighted tenant mix in the traffic generator.
* Tenant as **W3C baggage** on the root span; a **span processor** and a **log processor** stamp `tenant.id` on every child span and log line.
* `tenant_id` **label** on the order metrics (low cardinality: tenant yes, order id no).
* Chaos for **one tenant** (noisy neighbour); `fabrikam` is a bit slower by default.
* Dashboard: **Tenant** variable, per-tenant panels, tenant-filtered logs and TraceQL.
* Live insights: PromQL built **server-side** with an enforced `tenant_id` matcher - *My tenant* vs *Operator view*, per-tenant table, PromQL console for operators only.

## Try it

1. Open the shop in two browser tabs, pick a different tenant in each and buy something: every order's trace carries its own `tenant.id`.
2. Aspire dashboard → **Traces** → open a `Place order` trace: `tenant.id` is on every span. **Structured logs** → the log lines have it too.
3. Traffic & chaos → 40% failures + 1500 ms for **fabrikam** → *Apply*.
4. Live insights → *Operator view*: the fabrikam row turns red while the other tenants stay healthy. *My tenant*: every query is limited to the selected tenant.
5. Grafana → **.NET O11y Demo** → use the **Tenant** variable. Explore → Tempo → TraceQL: `{ span.tenant.id = "fabrikam" && status = error }`.
6. Using Grafana Cloud? Re-import the dashboard: the copy from step 07 has no Tenant variable. The setup guide is in the [`07-grafana-cloud`](../../tree/07-grafana-cloud) README.

## Look at

* `NoBeard.Labs.Observability.ServiceDefaults/TenantTelemetry.cs`
* `NoBeard.Labs.Observability.Web/Services/TenantState.cs`
* `NoBeard.Labs.Observability.Web/Services/InsightQueries.cs`
* `NoBeard.Labs.Observability.Web/Services/OrderService.cs`
* `NoBeard.Labs.Observability.Web/Components/Pages/Insights.razor`
* `grafana/dashboards/nobeard-o11y-demo.json`

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
| 06 | [`06-prometheus-api`](../../tree/06-prometheus-api) | Read metrics back via the Prometheus HTTP API in Blazor |
| 07 | [`07-grafana-cloud`](../../tree/07-grafana-cloud) | Fan out to Grafana Cloud with a launch profile |
| 08 | [`08-multi-tenant`](../../tree/08-multi-tenant) | Tenants end to end: telemetry and data access **← you are here** |
| — | [`main`](../../tree/main) | = step 08 + presenter README (demo script, stage checklist) |

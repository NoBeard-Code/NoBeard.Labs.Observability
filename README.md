# NoBeard.Labs.Observability

**.NET Observability End‑to‑End: OpenTelemetry with Aspire and Grafana Cloud** — conference demo.

A single Blazor **Server** shop, instrumented with OpenTelemetry through Aspire service defaults.
Telemetry goes to the Aspire dashboard (dev loop) **and** through Grafana Alloy to Grafana (local LGTM container,
optionally Grafana Cloud). The Blazor app then reads the data back through the **Prometheus HTTP API** and renders
live, tenant-aware insights in the UI.

```
webfrontend (Blazor Server, .NET 10) — spans · metrics · logs, tenant.id on everything
        │
        │ OTLP
        ├──────────────────────────────────┐
        ▼                                  ▼
Aspire dashboard                     Grafana Alloy ── redact · batch ──┬──▶ otel-lgtm (Grafana, Prometheus, Loki, Tempo)
                                                                       └──▶ Grafana Cloud (optional)
                                                                                 │
     Blazor "Live insights" page  ◀──────── Prometheus HTTP API ─────────────────┘
```

## Prerequisites

* Visual Studio 2026 (or 2022 17.14+) with the ASP.NET workload, .NET 10 SDK
* Docker Desktop running (Linux containers)
* Free ports: **3000** (Grafana is pinned there so the URL is predictable on stage)

## Run it (local only — works offline)

1. Open `NoBeard.Labs.Observability.slnx`, set **NoBeard.Labs.Observability.AppHost** as the startup project, **F5**.
2. The Aspire dashboard opens. Wait until `grafana` (≈20 s on first start) and `alloy` are *Running*.
3. Click the `webfrontend` URL → the shop. The traffic generator starts placing orders after ~5 s.
4. `grafana` resource → **Grafana** URL (http://localhost:3000) → Dashboards → *NoBeard Labs* → **.NET O11y Demo**.
5. `alloy` resource → **Alloy pipeline** URL → *Graph* shows the live pipeline.

| Resource | What it is |
|---|---|
| `webfrontend` | Blazor Server app (global `InteractiveServer` render mode, no WebAssembly): shop, live insights, traffic & chaos |
| `alloy` | `grafana/alloy` (container `nobeard-demo-alloy`) — OTLP receiver → PII redaction → batch → exporters ([grafana/alloy](grafana/alloy)) |
| `grafana` | `grafana/otel-lgtm` (container `nobeard-demo-grafana`) — Grafana + Prometheus + Loki + Tempo, data kept in volume `nobeard-demo-lgtm-data` |

All Docker containers and volumes use the `nobeard-demo-` prefix so they don't collide with other local Docker resources.

## Grafana Cloud

Local vs. cloud is a **launch profile** — pick it in Visual Studio's start-button dropdown:

| Profile | What happens |
|---|---|
| `https` | Local only: Alloy → otel-lgtm; *Live insights* reads local Prometheus. Works offline. |
| `https-cloud` | Alloy fans out to otel-lgtm **and** Grafana Cloud ([`cloud.alloy`](grafana/alloy/cloud.alloy)); *Live insights* reads **Grafana Cloud Metrics (Mimir)** — same code, only `BaseUrl` + basic auth differ. |

### 1. Collect the values

These live in the **Cloud Portal** — `https://grafana.com/orgs/<your-org>` (grafana.com → sign in → *My Account*) —
**not** in your Grafana instance (`https://<stack>.grafana.net`). In the portal, select your stack in the left
menu: the stack page shows tiles for Grafana, Prometheus, Loki, Tempo, OpenTelemetry, ….

| User secret | Cloud Portal (stack page) | Alternative inside Grafana (`<stack>.grafana.net`) |
|---|---|---|
| `grafana-cloud-grafana-url` | Grafana tile → URL, e.g. `https://<stack>.grafana.net` | the address in your browser |
| `grafana-cloud-otlp-endpoint` | **OpenTelemetry** tile → *Configure* → endpoint, e.g. `https://otlp-gateway-prod-eu-west-2.grafana.net/otlp` | *Connections* → *Add new connection* → search **OpenTelemetry** → *OpenTelemetry (OTLP)* |
| `grafana-cloud-instance-id` | Same OpenTelemetry page → *Instance ID* (a number; also the user in its `Authorization: Basic` header) | same connection page |
| `grafana-cloud-prometheus-url` | **Prometheus** tile → *Details* → **query endpoint**, e.g. `https://prometheus-prod-24-prod-eu-west-2.grafana.net/api/prom` | *Connections* → *Data sources* → `grafanacloud-<stack>-prom` → *Connection* URL |
| `grafana-cloud-prometheus-user` | Same Prometheus details page → *User* / instance ID (a **different** number than the OTLP one) | same data source → *Authentication* → *User* |
| `grafana-cloud-api-token` | An **access policy token** — see below | *Administration* → *Users and access* → *Cloud access policies* |

**Token:** Cloud Portal → *Security* → **Access Policies** → *Create access policy* for your stack with scopes
`metrics:read`, `metrics:write`, `logs:write`, `traces:write` → *Add token* (set an expiry *after* your talk) → copy it
(shown once). ⚠️ The token the OpenTelemetry tile generates has **write scopes only** — Alloy would work, but
*Live insights* would get `401` because it also needs `metrics:read`.

### 2. Store them as user secrets (never in appsettings / git)

Right-click the AppHost → **Manage User Secrets**:

```json
{
  "Parameters": {
    "grafana-cloud-grafana-url": "https://<stack>.grafana.net",
    "grafana-cloud-otlp-endpoint": "https://otlp-gateway-prod-<region>.grafana.net/otlp",
    "grafana-cloud-instance-id": "<OTLP instance ID>",
    "grafana-cloud-prometheus-url": "https://prometheus-prod-<nn>-prod-<region>.grafana.net/api/prom",
    "grafana-cloud-prometheus-user": "<Prometheus instance ID>",
    "grafana-cloud-api-token": "<access policy token>"
  }
}
```

The Aspire dashboard's **Parameters** tab shows the values in use (token masked) — handy to spot a leftover
placeholder. Optional: `"GrafanaCloud": { "TempoDatasourceUid": "…" }` if your stack's Tempo data source UID isn't
`grafanacloud-traces` (check *Connections → Data sources → Tempo*, the UID is in the URL); it's used for the
trace links on the Shop page.

### 3. Run and verify

1. Start with the **`https-cloud`** profile. *Live insights* shows the badge **Grafana Cloud Metrics**.
2. Grafana Cloud → *Explore*: Prometheus `demo_orders_placed_total` · Loki `{service_name="webfrontend"}` ·
   Tempo → Search → service `webfrontend`. Data arrives within ~30 s.
3. *Dashboards → New → Import* → upload [`grafana/dashboards/nobeard-o11y-demo.json`](grafana/dashboards/nobeard-o11y-demo.json).
   At the top pick the `grafanacloud-<stack>-prom` / `-logs` / `-traces` data sources and **save** the dashboard so
   they stick.
4. *Observability → Application* (Application Observability): enable it once; `webfrontend` shows up after a few
   minutes (traces + `host_info` from Alloy).

Cost: the app produces ≈ 250–400 active series at a 10 s interval — far inside the free tier (10k series,
50 GB logs, 50 GB traces, 14-day retention).

### 4. Make it stage-proof

* **Warm it up**: run `https-cloud` with traffic for 30+ min the day before — dashboards and App Observability have
  history, and you've seen it work on that network.
* **Log in** to Grafana Cloud in the browser you'll present from; open the dashboard, Explore and App Observability in
  tabs beforehand (the Shop's trace links open there too).
* **Plan B**: Wi-Fi dies → stop, switch the profile to `https`, F5 — everything works offline against otel-lgtm
  (images are already pulled). Only outbound HTTPS (443) is needed for the cloud path.
* **Token expiry**: double-check the access policy token is still valid on the day.
* Optional: tether to your phone instead of venue Wi-Fi for the cloud part.

## Where to look in the code (talk track)

| Topic | File |
|---|---|
| Dual OTLP export (Aspire dashboard + Alloy), Blazor .NET 10 meters/sources, exemplars, 10 s interval for DPM cost | [ServiceDefaults/Extensions.cs](NoBeard.Labs.Observability.ServiceDefaults/Extensions.cs) |
| Multi-tenant telemetry: baggage → span attribute + log attribute processors | [ServiceDefaults/TenantTelemetry.cs](NoBeard.Labs.Observability.ServiceDefaults/TenantTelemetry.cs) |
| Custom spans, metrics (`demo.orders.*`), structured logs, chaos | [Web/Services/OrderService.cs](NoBeard.Labs.Observability.Web/Services/OrderService.cs) |
| Orchestration: LGTM, Alloy, local vs cloud switch | [AppHost/AppHost.cs](NoBeard.Labs.Observability.AppHost/AppHost.cs) |
| Collector pipeline | [grafana/alloy/local.alloy](grafana/alloy/local.alloy), [cloud.alloy](grafana/alloy/cloud.alloy) |
| Prometheus HTTP API client (`/api/v1/query`, `/query_range`) | [Web/Services/PrometheusClient.cs](NoBeard.Labs.Observability.Web/Services/PrometheusClient.cs) |
| Server-side PromQL with enforced tenant matcher | [Web/Services/InsightQueries.cs](NoBeard.Labs.Observability.Web/Services/InsightQueries.cs) |
| Live UI pushed over the Blazor circuit | [Web/Components/Pages/Insights.razor](NoBeard.Labs.Observability.Web/Components/Pages/Insights.razor) |

### Metric names (OTel → Prometheus)

| OpenTelemetry instrument | Prometheus series |
|---|---|
| `demo.orders.placed` `{order}` | `demo_orders_placed_total{tenant_id, product_id}` |
| `demo.orders.failed` | `demo_orders_failed_total{tenant_id, error_type}` |
| `demo.orders.revenue` | `demo_orders_revenue_total{tenant_id, product_id}` |
| `demo.orders.processing.duration` `s` | `demo_orders_processing_duration_seconds_bucket{tenant_id, outcome, le}` |
| `aspnetcore.components.circuit.active` (.NET 10) | `aspnetcore_components_circuit_active` |
| `aspnetcore.components.handle_event.duration` (.NET 10) | `aspnetcore_components_handle_event_duration_seconds_bucket` |

## Demo script (≈10 min)

1. **Aspire dashboard** — resources, structured logs, a trace: Blazor `Event onclick` → `Place order`
   → `Reserve inventory` / `Charge payment`. Point out `tenant.id` on every span and log line (baggage + processors).
2. **Alloy UI** — the pipeline graph; `cloud.alloy` fan-out; PII redaction lives in the collector, not in the app.
3. **Grafana** — *.NET O11y Demo* dashboard, tenant variable; Explore → Tempo → TraceQL
   `{ span.tenant.id = "fabrikam" && status = error }`; trace → logs.
4. **Blazor Live insights** — the same numbers, fetched via the Prometheus API and pushed over SignalR every 5 s.
   Toggle *My tenant* vs *Operator view*: the server injects `tenant_id="…"` into every query; tenants never send PromQL.
5. **Break it** — *Traffic & chaos*: 40 % failures + 1500 ms for **fabrikam**. Watch the per-tenant row go red in
   the Blazor UI, the stat panels in Grafana; click an **exemplar** on the p95 panel → the slow trace → its error log.
   * *Shop → Burst ×20 as fabrikam*: all 20 orders share **one trace** — its root is the .NET 10 Blazor
     `Event onclick` span. Click the trace id → Grafana Tempo.
6. **Cloud** (`https-cloud` profile, warmed up beforehand) — same dashboard in Grafana Cloud, *Live insights* badge now
   says *Grafana Cloud Metrics*, Application Observability service view, cost note: 10 s
   export interval and low-cardinality labels (tenant yes, order id no).

## Troubleshooting

* **Launch profile** → the default `https` profile is what Visual Studio uses. The `http` profile works too
  (`ASPIRE_ALLOW_UNSECURED_TRANSPORT=true` is set for it), e.g. `dotnet run --project NoBeard.Labs.Observability.AppHost --launch-profile http`.
* **Tenant resets to contoso after a page reload** → by design: the tenant lives in a scoped service, i.e. per Blazor
  circuit. Open two browser tabs to act as two tenants side by side.
* **"Blazor event p95" shows –** → it only moves when someone clicks in the UI (the traffic generator doesn't raise
  Blazor events); click a few *Buy* buttons.
* **Port 3000 in use** → change `port: 3000` in `AppHost.cs`.
* **No data in Grafana** → check the `alloy` console logs in the Aspire dashboard; then Prometheus URL of `grafana`
  → `/api/v1/label/__name__/values`.
* **Reset local data** → stop the AppHost, `docker volume rm nobeard-demo-lgtm-data`.
* **Leftover containers after a crash / hard stop** → harmless: Aspire replaces `nobeard-demo-*` containers on the
  next start. To clean up manually: `docker rm -f nobeard-demo-grafana nobeard-demo-alloy`.

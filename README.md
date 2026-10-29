# NoBeard.Labs.Observability - step 07: Fan out to Grafana Cloud with a launch profile

Companion code for the session **.NET Observability End-to-End: OpenTelemetry with Aspire and Grafana Cloud**.

## What's new in this step

* Launch profile **`https-cloud`** (`GrafanaCloud__Enabled=true`): Alloy loads [`cloud.alloy`](grafana/alloy/cloud.alloy) and fans out to local Grafana **and** Grafana Cloud (plus `host_info` for Application Observability).
* Credentials as **Aspire parameters** from user secrets (token marked secret); the Live insights page reads **Grafana Cloud Metrics (Mimir)** - same `PrometheusClient`, only base URL + basic auth differ.
* Dashboard data-source pickers filtered by name, so the import into Grafana Cloud picks the right sources.

## Try it

1. Set the user secrets (below), start with **`https-cloud`** → Live insights badge says *Grafana Cloud Metrics*.
2. Import the dashboard into Grafana Cloud; check Explore and Application Observability.
3. Switch back to the `https` profile → everything local again (the offline fallback).

## Look at

* `grafana/alloy/cloud.alloy`
* `NoBeard.Labs.Observability.AppHost/AppHost.cs`
* `NoBeard.Labs.Observability.AppHost/Properties/launchSettings.json`

## Run it

Visual Studio 2026 (or 2022 17.14+) with .NET 10 and Docker Desktop running: open `NoBeard.Labs.Observability.slnx`,
set **NoBeard.Labs.Observability.AppHost** as startup project, **F5**. Or from a terminal:
`dotnet run --project NoBeard.Labs.Observability.AppHost --launch-profile http`.

## Grafana Cloud setup

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
   The *Metrics / Logs / Traces* pickers auto-select `grafanacloud-<stack>-prom` / `-logs` / `-traces` (they're
   filtered by name, so the stack's usage / ML / alert-history data sources never get picked by accident).
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
| 07 | [`07-grafana-cloud`](../../tree/07-grafana-cloud) | Fan out to Grafana Cloud with a launch profile **← you are here** |
| 08 | [`08-multi-tenant`](../../tree/08-multi-tenant) | Tenants end to end: telemetry and data access |
| — | [`main`](../../tree/main) | = step 08 + presenter README (demo script, stage checklist) |

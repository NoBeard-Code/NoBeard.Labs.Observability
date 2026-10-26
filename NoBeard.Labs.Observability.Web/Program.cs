using NoBeard.Labs.Observability.Web.Components;
using NoBeard.Labs.Observability.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Blazor Server: all interactivity runs on the server over the SignalR circuit (no WebAssembly).
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<TenantState>();

builder.Services.AddHttpClient<ShopApiClient>(client =>
    {
        // This URL uses "https+http://" to indicate HTTPS is preferred over HTTP.
        // Learn more about service discovery scheme resolution at https://aka.ms/dotnet/sdschemes.
        client.BaseAddress = new("https+http://apiservice");
    });

// Prometheus-compatible query API: local LGTM Prometheus or Grafana Cloud Metrics (Mimir), set by the AppHost.
builder.Services.Configure<PrometheusOptions>(builder.Configuration.GetSection("Prometheus"));
builder.Services.AddHttpClient<PrometheusClient>(PrometheusClient.Configure);

builder.Services.Configure<GrafanaOptions>(builder.Configuration.GetSection("Grafana"));

builder.Services.AddSingleton<TrafficGenerator>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<TrafficGenerator>());

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();

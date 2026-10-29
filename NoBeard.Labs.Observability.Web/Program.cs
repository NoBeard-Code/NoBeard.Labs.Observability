using NoBeard.Labs.Observability.Web.Components;
using NoBeard.Labs.Observability.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Blazor Server: all interactivity runs on the server over the SignalR circuit (no WebAssembly).
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// The shop itself: in-process order handling with custom spans, metrics and logs.
builder.Services.AddSingleton<OrderMetrics>();
builder.Services.AddSingleton<OrderService>();

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

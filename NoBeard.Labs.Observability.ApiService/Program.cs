using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using NoBeard.Labs.Observability.ApiService;
using NoBeard.Labs.Observability.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();
builder.AddRedisDistributedCache("cache"); // brings Redis tracing + health checks for free

builder.Services.AddProblemDetails();
builder.Services.AddSingleton<OrderMetrics>();
builder.Services.AddSingleton<ChaosSettings>();
builder.Services.AddSingleton<OrderProcessor>();

var app = builder.Build();

app.UseExceptionHandler();

// Resolve the tenant once per request. The header is the business contract; the baggage is for telemetry.
app.Use(async (context, next) =>
{
    var tenant = context.Request.Headers[TenantTelemetry.HeaderName].FirstOrDefault()
                 ?? TenantTelemetry.CurrentTenant
                 ?? "unknown";
    context.Items[TenantTelemetry.AttributeName] = tenant;
    Activity.Current.SetTenant(tenant);
    await next(context);
});

app.MapGet("/", () => "Observability demo API. Try /products, POST /orders, /chaos.");

app.MapGet("/products", async (IDistributedCache cache, ILogger<Program> logger, CancellationToken ct) =>
{
    const string cacheKey = "catalog:v1";

    var json = await cache.GetStringAsync(cacheKey, ct);
    if (json is null)
    {
        using (var activity = ApiTelemetry.Source.StartActivity("Load catalog from database"))
        {
            activity?.SetTag("db.system.name", "postgresql"); // simulated
            await Task.Delay(Random.Shared.Next(80, 220), ct);
            json = JsonSerializer.Serialize(Catalog.Products, JsonSerializerOptions.Web);
        }

        await cache.SetStringAsync(cacheKey, json,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30) }, ct);
        logger.LogInformation("Catalog cache miss - loaded {ProductCount} products from database", Catalog.Products.Length);
    }

    return Results.Text(json, "application/json");
});

app.MapPost("/orders", async (OrderRequest request, HttpContext context, OrderProcessor processor, CancellationToken ct) =>
{
    var tenant = (string)context.Items[TenantTelemetry.AttributeName]!;
    var outcome = await processor.PlaceOrderAsync(tenant, request, ct);

    return outcome switch
    {
        { Order: { } order } => Results.Ok(order),
        { Error: "unknown_product" } => Results.NotFound(),
        _ => Results.Problem(title: "Payment provider unavailable", statusCode: StatusCodes.Status502BadGateway),
    };
});

app.MapGet("/chaos", (ChaosSettings chaos) => chaos.Snapshot());
app.MapPost("/chaos", (ChaosState state, ChaosSettings chaos, ILogger<Program> logger) =>
{
    chaos.Apply(state);
    logger.LogWarning("Chaos settings changed: failure rate {FailureRate:P0}, extra latency {ExtraLatencyMs} ms, tenant {ChaosTenant}",
        state.FailureRate, state.ExtraLatencyMs, state.Tenant ?? "all");
    return chaos.Snapshot();
});

app.MapDefaultEndpoints();

app.Run();

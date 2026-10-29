namespace NoBeard.Labs.Observability.Web.Components.Shared;

public static class SeriesColors
{
    public static string For(string? series) => series switch
    {
        "otel-hoodie" => "#2a78d6",
        "aspire-mug" => "#7b4fd6",
        "grafana-socks" => "#e8871e",
        "blazor-sticker" => "#1f9e74",
        "prom-tshirt" => "#d6455f",
        "failed" => "#d6455f",
        _ => "#8a8f98",
    };
}

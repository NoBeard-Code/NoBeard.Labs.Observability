namespace NoBeard.Labs.Observability.Web.Components.Shared;

public static class TenantColors
{
    public static string For(string? tenant) => tenant switch
    {
        "contoso" => "#2a78d6",
        "fabrikam" => "#e8871e",
        "northwind" => "#1f9e74",
        _ => "#8a8f98",
    };
}

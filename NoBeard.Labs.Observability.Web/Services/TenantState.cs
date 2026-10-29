using NoBeard.Labs.Observability.ServiceDefaults;

namespace NoBeard.Labs.Observability.Web.Services;

/// <summary>
/// The tenant the current user acts as. Registered as scoped, which in Blazor Server means one instance per circuit
/// (browser tab) - so two tabs can impersonate two different tenants side by side during the demo.
/// </summary>
public sealed class TenantState
{
    public string Current { get; private set; } = TenantTelemetry.KnownTenants[0];

    public event Action? Changed;

    public void Set(string tenant)
    {
        if (!TenantTelemetry.KnownTenants.Contains(tenant) || tenant == Current) return;
        Current = tenant;
        Changed?.Invoke();
    }
}

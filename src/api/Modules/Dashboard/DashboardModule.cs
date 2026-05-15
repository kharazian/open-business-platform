using OpenBusinessPlatform.Api.Platform;

namespace OpenBusinessPlatform.Api.Modules.Dashboard;

public sealed class DashboardModule : IPlatformApiModule
{
    public string Id => "core.dashboard";

    public string Name => "Dashboard";

    public ModuleOwner Owner => ModuleOwner.Core;

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapDashboardEndpoints();
    }
}

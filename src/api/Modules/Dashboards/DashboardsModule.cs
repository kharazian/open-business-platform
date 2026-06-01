using OpenBusinessPlatform.Api.Platform;

namespace OpenBusinessPlatform.Api.Modules.Dashboards;

public sealed class DashboardsModule : IPlatformApiModule
{
    public string Id => "app.dashboards";

    public string Name => "Dashboards";

    public ModuleOwner Owner => ModuleOwner.App;

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapDashboardsEndpoints();
    }
}

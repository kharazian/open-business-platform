using OpenBusinessPlatform.Api.Platform;

namespace OpenBusinessPlatform.Api.Modules.Reports;

public sealed class ReportsModule : IPlatformApiModule
{
    public string Id => "app.reports";

    public string Name => "Reports";

    public ModuleOwner Owner => ModuleOwner.App;

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapReportsEndpoints();
    }
}

using OpenBusinessPlatform.Api.Platform;

namespace OpenBusinessPlatform.Api.Modules.Records;

public sealed class RecordsModule : IPlatformApiModule
{
    public string Id => "app.records";

    public string Name => "Records";

    public ModuleOwner Owner => ModuleOwner.App;

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapRecordsEndpoints();
    }
}

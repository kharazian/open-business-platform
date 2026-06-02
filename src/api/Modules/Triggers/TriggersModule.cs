using OpenBusinessPlatform.Api.Platform;

namespace OpenBusinessPlatform.Api.Modules.Triggers;

public sealed class TriggersModule : IPlatformApiModule
{
    public string Id => "app.triggers";

    public string Name => "Triggers";

    public ModuleOwner Owner => ModuleOwner.App;

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapTriggersEndpoints();
    }
}

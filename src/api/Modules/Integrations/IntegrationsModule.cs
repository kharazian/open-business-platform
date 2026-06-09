using OpenBusinessPlatform.Api.Platform;

namespace OpenBusinessPlatform.Api.Modules.Integrations;

public sealed class IntegrationsModule : IPlatformApiModule
{
    public string Id => "app.integrations";

    public string Name => "Integrations";

    public ModuleOwner Owner => ModuleOwner.App;

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapIntegrationsEndpoints();
    }
}

using OpenBusinessPlatform.Api.Platform;

namespace OpenBusinessPlatform.Api.Modules.Identity;

public sealed class IdentityModule : IPlatformApiModule
{
    public string Id => "core.identity";

    public string Name => "Identity";

    public ModuleOwner Owner => ModuleOwner.Core;

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapIdentityEndpoints();
    }
}

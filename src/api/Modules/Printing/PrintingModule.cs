using OpenBusinessPlatform.Api.Platform;

namespace OpenBusinessPlatform.Api.Modules.Printing;

public sealed class PrintingModule : IPlatformApiModule
{
    public string Id => "app.printing";

    public string Name => "Printing";

    public ModuleOwner Owner => ModuleOwner.App;

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPrintingEndpoints();
    }
}

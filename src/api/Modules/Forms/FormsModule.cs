using OpenBusinessPlatform.Api.Platform;

namespace OpenBusinessPlatform.Api.Modules.Forms;

public sealed class FormsModule : IPlatformApiModule
{
    public string Id => "app.forms";

    public string Name => "Forms";

    public ModuleOwner Owner => ModuleOwner.App;

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapFormsEndpoints();
    }
}

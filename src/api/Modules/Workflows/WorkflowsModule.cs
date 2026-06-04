using OpenBusinessPlatform.Api.Platform;

namespace OpenBusinessPlatform.Api.Modules.Workflows;

public sealed class WorkflowsModule : IPlatformApiModule
{
    public string Id => "app.workflows";

    public string Name => "Workflows";

    public ModuleOwner Owner => ModuleOwner.App;

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapWorkflowsEndpoints();
    }
}

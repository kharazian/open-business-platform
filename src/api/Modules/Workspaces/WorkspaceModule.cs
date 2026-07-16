using OpenBusinessPlatform.Api.Platform;

namespace OpenBusinessPlatform.Api.Modules.Workspaces;

public sealed class WorkspaceModule : IPlatformApiModule
{
    public string Id => "enterprise.workspaces";

    public string Name => "Workspaces";

    public ModuleOwner Owner => ModuleOwner.Core;

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/workspaces/current", async (
            WorkspaceContextService workspaceContextService,
            CancellationToken cancellationToken) =>
        {
            var current = await workspaceContextService.GetCurrentAsync(cancellationToken);
            return current is null ? Results.NotFound() : Results.Ok(current);
        })
        .WithTags("Workspaces")
        .RequireAuthorization();
    }
}

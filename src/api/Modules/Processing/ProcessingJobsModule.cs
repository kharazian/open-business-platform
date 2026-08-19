using OpenBusinessPlatform.Api.Modules.Identity;
using OpenBusinessPlatform.Api.Modules.Workspaces;
using OpenBusinessPlatform.Api.Platform;
using Microsoft.AspNetCore.Mvc;

namespace OpenBusinessPlatform.Api.Modules.Processing;

public sealed class ProcessingJobsModule : IPlatformApiModule
{
    public string Id => "processing-jobs";
    public string Name => "Processing jobs";
    public ModuleOwner Owner => ModuleOwner.App;

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/processing-jobs").WithTags("Processing jobs").RequireAuthorization();
        group.MapGet("", async (int? page, int? pageSize, ProcessingJobService service, PermissionService permissions, HttpContext context, CancellationToken ct) =>
            !await Allowed(permissions, context, ct) ? Results.Forbid() : Results.Ok(await service.ListAsync(page ?? 1, pageSize ?? 25, ct)));
        group.MapGet("/{id:guid}", async (Guid id, ProcessingJobService service, PermissionService permissions, HttpContext context, CancellationToken ct) =>
            !await Allowed(permissions, context, ct) ? Results.Forbid() : await Found(service.GetAsync(id, ct)));
        group.MapPost("", async (CreateProcessingJobRequest request, ProcessingJobService service, PermissionService permissions, HttpContext context, CancellationToken ct) =>
            !await Allowed(permissions, context, ct) ? Results.Forbid() : await Handle(async () =>
            {
                var created = await service.CreateAsync(request, Actor(context), ct);
                return Results.Created($"/api/processing-jobs/{created.Id}", created);
            }));
        group.MapPut("/{id:guid}", async (Guid id, UpdateProcessingJobRequest request, ProcessingJobService service, PermissionService permissions, HttpContext context, CancellationToken ct) =>
            !await Allowed(permissions, context, ct) ? Results.Forbid() : await Handle(async () => Results.Ok(await service.UpdateAsync(id, request, Actor(context), ct))));
        group.MapDelete("/{id:guid}", async (Guid id, [FromBody] ProcessingJobStateRequest request, ProcessingJobService service, PermissionService permissions, HttpContext context, CancellationToken ct) =>
            !await Allowed(permissions, context, ct) ? Results.Forbid() : await Handle(async () => { await service.DeleteAsync(id, request, Actor(context), ct); return Results.NoContent(); }));
        group.MapPost("/{id:guid}/enable", async (Guid id, ProcessingJobStateRequest request, ProcessingJobService service, PermissionService permissions, HttpContext context, CancellationToken ct) =>
            !await Allowed(permissions, context, ct) ? Results.Forbid() : await Handle(async () => Results.Ok(await service.SetEnabledAsync(id, request, true, Actor(context), ct))));
        group.MapPost("/{id:guid}/disable", async (Guid id, ProcessingJobStateRequest request, ProcessingJobService service, PermissionService permissions, HttpContext context, CancellationToken ct) =>
            !await Allowed(permissions, context, ct) ? Results.Forbid() : await Handle(async () => Results.Ok(await service.SetEnabledAsync(id, request, false, Actor(context), ct))));
        group.MapGet("/{id:guid}/runs", async (Guid id, int? page, int? pageSize, ProcessingJobService service, PermissionService permissions, HttpContext context, CancellationToken ct) =>
            !await Allowed(permissions, context, ct) ? Results.Forbid() : await Handle(async () => Results.Ok(await service.ListRunsAsync(id, page ?? 1, pageSize ?? 25, ct))));
        group.MapGet("/{id:guid}/runs/{runId:guid}", async (Guid id, Guid runId, ProcessingJobService service, PermissionService permissions, HttpContext context, CancellationToken ct) =>
            !await Allowed(permissions, context, ct) ? Results.Forbid() : await Found(service.GetRunAsync(id, runId, ct)));
        group.MapPost("/{id:guid}/runs", async (Guid id, CreateProcessingJobRunRequest request, ProcessingJobService service, PermissionService permissions, HttpContext context, CancellationToken ct) =>
            !await Allowed(permissions, context, ct) ? Results.Forbid() : await Handle(async () => Results.Accepted($"/api/processing-jobs/{id}/runs", await service.QueueManualAsync(id, request, Actor(context), ct))));
        group.MapPost("/{id:guid}/runs/{runId:guid}/retry", async (Guid id, Guid runId, ProcessingJobService service, PermissionService permissions, HttpContext context, CancellationToken ct) =>
            !await Allowed(permissions, context, ct) ? Results.Forbid() : await Handle(async () => Results.Accepted($"/api/processing-jobs/{id}/runs", await service.RetryAsync(id, runId, Actor(context), ct))));
    }

    private static async Task<bool> Allowed(PermissionService permissions, HttpContext context, CancellationToken ct) =>
        await permissions.CanAsync(context.User, PlatformPermissions.Integrations.Manage, ct);
    private static Guid Actor(HttpContext context) => WorkspaceMembershipService.GetUserId(context.User) ?? Guid.Empty;
    private static async Task<IResult> Found<T>(Task<T?> task) where T : class => await task is { } value ? Results.Ok(value) : Results.NotFound();
    private static async Task<IResult> Handle(Func<Task<IResult>> action)
    {
        try { return await action(); }
        catch (ProcessingJobException ex)
        {
            return Results.Json(new ProcessingJobErrorResponse(ex.Message, ex.Errors.Count == 0 ? null : ex.Errors), statusCode: ex.StatusCode);
        }
    }
}

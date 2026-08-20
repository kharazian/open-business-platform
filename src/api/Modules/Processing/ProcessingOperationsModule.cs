using OpenBusinessPlatform.Api.Modules.Identity;
using OpenBusinessPlatform.Api.Platform;

namespace OpenBusinessPlatform.Api.Modules.Processing;

public sealed class ProcessingOperationsModule : IPlatformApiModule
{
    public string Id => "processing-operations";
    public string Name => "Processing operations";
    public ModuleOwner Owner => ModuleOwner.App;

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/processing-operations").WithTags("Processing operations").RequireAuthorization();
        group.MapGet("/logs", async (int? page, int? pageSize, Guid? definitionId, Guid? runId, string? kind,
            string? severity, string? eventCode, string? errorCode, DateTimeOffset? from, DateTimeOffset? to,
            ProcessingOperationsService service, PermissionService permissions, HttpContext context, CancellationToken ct) =>
            !await Allowed(permissions, context, ct) ? Results.Forbid() : await Handle(async () => Results.Ok(
                await service.ListAsync(page ?? 1, pageSize ?? 25, definitionId, runId, kind, severity, eventCode, errorCode, from, to, ct))));
        group.MapGet("/summary", async (DateTimeOffset? from, DateTimeOffset? to, ProcessingOperationsService service,
            PermissionService permissions, HttpContext context, CancellationToken ct) =>
            !await Allowed(permissions, context, ct) ? Results.Forbid() : await Handle(async () => Results.Ok(await service.SummaryAsync(from, to, ct))));
        group.MapGet("/notification-recipients", async (int? page, int? pageSize, string? search,
            ProcessingOperationsService service, PermissionService permissions, HttpContext context, CancellationToken ct) =>
            !await Allowed(permissions, context, ct) ? Results.Forbid() : await Handle(async () => Results.Ok(
                await service.ListRecipientsAsync(page ?? 1, pageSize ?? 25, search, ct))));
    }

    private static Task<bool> Allowed(PermissionService permissions, HttpContext context, CancellationToken ct) =>
        permissions.CanAsync(context.User, PlatformPermissions.Integrations.Manage, ct);
    private static async Task<IResult> Handle(Func<Task<IResult>> action)
    {
        try { return await action(); }
        catch (ProcessingJobException exception)
        {
            return Results.Json(new ProcessingJobErrorResponse(exception.Message, exception.Errors.Count == 0 ? null : exception.Errors), statusCode: exception.StatusCode);
        }
    }
}

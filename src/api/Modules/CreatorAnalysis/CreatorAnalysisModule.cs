using Microsoft.AspNetCore.Mvc;
using OpenBusinessPlatform.Api.Modules.Identity;
using OpenBusinessPlatform.Api.Modules.Workspaces;
using OpenBusinessPlatform.Api.Platform;

namespace OpenBusinessPlatform.Api.Modules.CreatorAnalysis;

public sealed class CreatorAnalysisModule : IPlatformApiModule
{
    public string Id => "creator-analysis";
    public string Name => "Creator analysis";
    public ModuleOwner Owner => ModuleOwner.App;

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/creator-analysis", AnalyzeAsync)
            .WithTags("Creator analysis")
            .RequireAuthorization()
            .DisableAntiforgery()
            .WithMetadata(new RequestSizeLimitAttribute(CreatorAnalysisLimits.MaxSourceBytes + 64 * 1024))
            .WithMetadata(new RequestFormLimitsAttribute
            {
                MultipartBodyLengthLimit = CreatorAnalysisLimits.MaxSourceBytes + 64 * 1024,
                MemoryBufferThreshold = CreatorAnalysisLimits.MaxSourceBytes + 64 * 1024,
                ValueCountLimit = 1
            });
    }

    private static async Task<IResult> AnalyzeAsync(
        HttpContext context,
        CreatorAnalysisService service,
        PermissionService permissions,
        CancellationToken ct)
    {
        if (!await permissions.CanAsync(context.User, PlatformPermissions.Forms.ManageAll, ct)
            || !await permissions.CanAsync(context.User, PlatformPermissions.Integrations.Manage, ct)) return Results.Forbid();
        if (!context.Request.HasFormContentType)
            return Error(StatusCodes.Status400BadRequest, "Creator analysis requires multipart form data.");
        try
        {
            var form = await context.Request.ReadFormAsync(ct);
            if (form.Count != 0 || form.Files.Count != 1 || form.Files[0].Name != "source")
                return Error(StatusCodes.Status400BadRequest, "Creator analysis requires exactly one source file.");
            return Results.Ok(await service.AnalyzeAsync(form.Files[0], WorkspaceMembershipService.GetUserId(context.User), ct));
        }
        catch (BadHttpRequestException exception) when (exception.StatusCode == StatusCodes.Status413PayloadTooLarge)
        {
            return Error(StatusCodes.Status413PayloadTooLarge, "Creator analysis source exceeds the size limit.");
        }
        catch (InvalidDataException)
        {
            return Error(StatusCodes.Status400BadRequest, "Creator analysis request is invalid.");
        }
        catch (CreatorAnalysisException exception) { return Error(exception.StatusCode, exception.Message); }
    }

    private static IResult Error(int statusCode, string message) => Results.Json(new { message }, statusCode: statusCode);
}

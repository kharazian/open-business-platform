using System.Security.Claims;
using OpenBusinessPlatform.Api.Modules.Identity;

namespace OpenBusinessPlatform.Api.Modules.Records;

public static class RecordsEndpoints
{
    public static IEndpointRouteBuilder MapRecordsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/forms/{formId:guid}/records").WithTags("Records").RequireAuthorization();

        group.MapPost("", async (
            Guid formId,
            SubmitRecordRequest request,
            RecordSubmissionService recordSubmission,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await permissionService.CanAccessFormAsync(httpContext.User, formId, PlatformPermissions.Form.Submit, cancellationToken))
            {
                return Results.Forbid();
            }

            return await HandleRecordRequestAsync(async () =>
            {
                var record = await recordSubmission.SubmitRecordAsync(formId, request, GetCurrentUserId(httpContext), cancellationToken);
                return Results.Created($"/api/records/{record.Id}", record);
            });
        });

        return endpoints;
    }

    private static async Task<IResult> HandleRecordRequestAsync(Func<Task<IResult>> action)
    {
        try
        {
            return await action();
        }
        catch (RecordSubmissionException exception)
        {
            var errors = exception.Errors.Count == 0 ? null : exception.Errors;
            return Results.Json(new RecordErrorResponse(exception.Message, errors), statusCode: exception.StatusCode);
        }
    }

    private static Guid? GetCurrentUserId(HttpContext httpContext)
    {
        var value = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}

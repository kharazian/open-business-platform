using System.Security.Claims;
using OpenBusinessPlatform.Api.Modules.Identity;

namespace OpenBusinessPlatform.Api.Modules.Records;

public static class RecordsEndpoints
{
    public static IEndpointRouteBuilder MapRecordsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/forms/{formId:guid}/records").WithTags("Records").RequireAuthorization();

        group.MapGet("", async (
            Guid formId,
            int? page,
            int? pageSize,
            string? search,
            RecordQueryService recordQuery,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await permissionService.CanAccessFormAsync(httpContext.User, formId, PlatformPermissions.Form.View, cancellationToken))
            {
                return Results.Forbid();
            }

            return await HandleRecordRequestAsync(async () =>
            {
                var records = await recordQuery.ListRecordsAsync(
                    formId,
                    new ListRecordsRequest(page ?? 1, pageSize ?? 25, search),
                    cancellationToken);

                return Results.Ok(records);
            });
        });

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

        var detailGroup = endpoints.MapGroup("/api/records").WithTags("Records").RequireAuthorization();

        detailGroup.MapGet("/{recordId:guid}", async (
            Guid recordId,
            RecordQueryService recordQuery,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var formId = await recordQuery.GetRecordFormIdAsync(recordId, cancellationToken);
            if (formId is null)
            {
                return Results.NotFound(new RecordErrorResponse("Record was not found."));
            }

            if (!await permissionService.CanAccessFormAsync(httpContext.User, formId.Value, PlatformPermissions.Form.View, cancellationToken))
            {
                return Results.Forbid();
            }

            return await HandleRecordRequestAsync(async () =>
            {
                var record = await recordQuery.GetRecordAsync(recordId, cancellationToken);
                return Results.Ok(record);
            });
        });

        detailGroup.MapPut("/{recordId:guid}", async (
            Guid recordId,
            UpdateRecordRequest request,
            RecordQueryService recordQuery,
            RecordMutationService recordMutation,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var formId = await recordQuery.GetRecordFormIdAsync(recordId, cancellationToken);
            if (formId is null)
            {
                return Results.NotFound(new RecordErrorResponse("Record was not found."));
            }

            if (!await permissionService.CanAccessFormAsync(httpContext.User, formId.Value, PlatformPermissions.Form.Edit, cancellationToken))
            {
                return Results.Forbid();
            }

            return await HandleRecordRequestAsync(async () =>
            {
                var record = await recordMutation.UpdateRecordAsync(recordId, request, GetCurrentUserId(httpContext), cancellationToken);
                return Results.Ok(record);
            });
        });

        detailGroup.MapDelete("/{recordId:guid}", async (
            Guid recordId,
            RecordQueryService recordQuery,
            RecordMutationService recordMutation,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var formId = await recordQuery.GetRecordFormIdAsync(recordId, cancellationToken);
            if (formId is null)
            {
                return Results.NotFound(new RecordErrorResponse("Record was not found."));
            }

            if (!await permissionService.CanAccessFormAsync(httpContext.User, formId.Value, PlatformPermissions.Form.Delete, cancellationToken))
            {
                return Results.Forbid();
            }

            return await HandleRecordRequestAsync(async () =>
                await recordMutation.DeleteRecordAsync(recordId, GetCurrentUserId(httpContext), cancellationToken)
                    ? Results.NoContent()
                    : Results.NotFound(new RecordErrorResponse("Record was not found.")));
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
        catch (RecordQueryException exception)
        {
            var errors = exception.Errors.Count == 0 ? null : exception.Errors;
            return Results.Json(new RecordErrorResponse(exception.Message, errors), statusCode: exception.StatusCode);
        }
        catch (RecordMutationException exception)
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

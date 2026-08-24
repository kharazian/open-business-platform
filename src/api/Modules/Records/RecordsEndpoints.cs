using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;
using OpenBusinessPlatform.Api.Modules.Forms;
using OpenBusinessPlatform.Api.Modules.Identity;

namespace OpenBusinessPlatform.Api.Modules.Records;

public static class RecordsEndpoints
{
    public static IEndpointRouteBuilder MapRecordsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/forms/{formId:guid}/records").WithTags("Records").RequireAuthorization();

        endpoints.MapGet("/api/forms/{formId:guid}/fields/{fieldId}/lookup-options", async (
            Guid formId,
            string fieldId,
            int? page,
            int? pageSize,
            string? search,
            RecordLookupService recordLookup,
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
                var options = await recordLookup.ListOptionsAsync(
                    httpContext.User,
                    formId,
                    fieldId,
                    new RecordLookupOptionsRequest(page ?? 1, pageSize ?? 25, search, GetLookupDependencyValues(httpContext)),
                    permissionService,
                    cancellationToken);

                return Results.Ok(options);
            });
        }).WithTags("Records").RequireAuthorization();

        endpoints.MapPost("/api/forms/{formId:guid}/fields/{fieldId}/attachments", async (
            Guid formId,
            string fieldId,
            Guid? recordId,
            IFormFile file,
            FileAttachmentService attachments,
            OpenBusinessPlatformDbContext dbContext,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (recordId is null)
            {
                if (!await permissionService.CanAccessFormAsync(httpContext.User, formId, PlatformPermissions.Form.Submit, cancellationToken)) return Results.Forbid();
            }
            else
            {
                var record = await dbContext.Records.AsNoTracking().FirstOrDefaultAsync(item => item.Id == recordId && item.FormId == formId && !item.IsDeleted, cancellationToken);
                if (record is null) return Results.NotFound();
                if (!await permissionService.CanAccessRecordAsync(httpContext.User, record, PlatformPermissions.Form.Edit, cancellationToken)) return Results.Forbid();
            }
            var fieldAccess = await permissionService.GetFieldAccessAsync(httpContext.User, formId, cancellationToken);
            if (fieldAccess.HiddenFieldIds.Contains(fieldId) || fieldAccess.ReadOnlyFieldIds.Contains(fieldId)) return Results.Forbid();
            return await HandleRecordRequestAsync(async () =>
            {
                var attachment = await attachments.UploadAsync(formId, fieldId, recordId, file, GetCurrentUserId(httpContext), cancellationToken);
                return Results.Created($"/api/attachments/{attachment.Id}", attachment);
            });
        })
        .WithTags("Records")
        .RequireAuthorization()
        .DisableAntiforgery()
        .WithMetadata(new RequestSizeLimitAttribute(FormFileUploadLimits.MaxSizeBytes + 64 * 1024));

        var attachmentGroup = endpoints.MapGroup("/api/attachments").WithTags("Records").RequireAuthorization();
        attachmentGroup.MapGet("/{attachmentId:guid}", async (Guid attachmentId, FileAttachmentService attachments, PermissionService permissions, HttpContext context, CancellationToken ct) =>
            await HandleRecordRequestAsync(async () => Results.Ok(await attachments.GetMetadataAsync(attachmentId, context.User, GetCurrentUserId(context), permissions, ct))));
        attachmentGroup.MapGet("/{attachmentId:guid}/content", async (Guid attachmentId, FileAttachmentService attachments, PermissionService permissions, HttpContext context, CancellationToken ct) =>
            await HandleRecordRequestAsync(async () => { var file = await attachments.DownloadAsync(attachmentId, context.User, GetCurrentUserId(context), permissions, ct); return Results.File(file.Content, file.ContentType, file.FileName); }));
        attachmentGroup.MapDelete("/{attachmentId:guid}", async (Guid attachmentId, FileAttachmentService attachments, HttpContext context, CancellationToken ct) =>
            await HandleRecordRequestAsync(async () => { await attachments.DeletePendingAsync(attachmentId, GetCurrentUserId(context), ct); return Results.NoContent(); }));

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
                    httpContext.User,
                    formId,
                    new ListRecordsRequest(page ?? 1, pageSize ?? 25, search, GetSubTableFilterValues(httpContext)),
                    permissionService,
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
                var record = await recordSubmission.SubmitRecordAsync(
                    formId,
                    request,
                    httpContext.User,
                    GetCurrentUserId(httpContext),
                    permissionService,
                    cancellationToken);
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
                var record = await recordQuery.GetRecordAsync(httpContext.User, recordId, permissionService, cancellationToken);
                return Results.Ok(record);
            });
        });

        detailGroup.MapGet("/{recordId:guid}/subtables/{fieldId}/rows", async (
            Guid recordId,
            string fieldId,
            int? page,
            int? pageSize,
            string? sortFieldId,
            string? sortDirection,
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
                var rows = await recordQuery.ListSubTableRowsAsync(
                    httpContext.User,
                    recordId,
                    fieldId,
                    new ListSubTableRowsRequest(page ?? 1, pageSize ?? 25, sortFieldId, sortDirection, GetSubTableFilterValues(httpContext)),
                    permissionService,
                    cancellationToken);
                return Results.Ok(rows);
            });
        });

        detailGroup.MapGet("/{recordId:guid}/timeline", async (
            Guid recordId,
            int? limit,
            RecordQueryService recordQuery,
            RecordTimelineService recordTimeline,
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
                var timeline = await recordTimeline.ListTimelineAsync(
                    httpContext.User,
                    recordId,
                    limit ?? 25,
                    permissionService,
                    cancellationToken);
                return Results.Ok(timeline);
            });
        });

        detailGroup.MapGet("/{recordId:guid}/related", async (
            Guid recordId,
            int? page,
            int? pageSize,
            RecordQueryService recordQuery,
            RelatedRecordService relatedRecords,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var formId = await recordQuery.GetRecordFormIdAsync(recordId, cancellationToken);
            if (formId is null)
                return Results.NotFound(new RecordErrorResponse("Record was not found."));
            if (!await permissionService.CanAccessFormAsync(httpContext.User, formId.Value, PlatformPermissions.Form.View, cancellationToken))
                return Results.Forbid();

            return await HandleRecordRequestAsync(async () => Results.Ok(await relatedRecords.ListPanelsAsync(
                httpContext.User,
                recordId,
                page ?? 1,
                pageSize ?? 10,
                permissionService,
                cancellationToken)));
        });

        detailGroup.MapGet("/{recordId:guid}/related/{sourceFormId:guid}/{sourceFieldId}", async (
            Guid recordId,
            Guid sourceFormId,
            string sourceFieldId,
            int? page,
            int? pageSize,
            RecordQueryService recordQuery,
            RelatedRecordService relatedRecords,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var formId = await recordQuery.GetRecordFormIdAsync(recordId, cancellationToken);
            if (formId is null)
                return Results.NotFound(new RecordErrorResponse("Record was not found."));
            if (!await permissionService.CanAccessFormAsync(httpContext.User, formId.Value, PlatformPermissions.Form.View, cancellationToken))
                return Results.Forbid();

            return await HandleRecordRequestAsync(async () => Results.Ok(await relatedRecords.ListRowsAsync(
                httpContext.User,
                recordId,
                sourceFormId,
                sourceFieldId,
                page ?? 1,
                pageSize ?? 10,
                permissionService,
                cancellationToken)));
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
                var record = await recordMutation.UpdateRecordAsync(recordId, request, httpContext.User, GetCurrentUserId(httpContext), permissionService, cancellationToken);
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
                await recordMutation.DeleteRecordAsync(recordId, httpContext.User, GetCurrentUserId(httpContext), permissionService, cancellationToken)
                    ? Results.NoContent()
                    : Results.NotFound(new RecordErrorResponse("Record was not found.")));
        });

        detailGroup.MapPost("/{recordId:guid}/assign", async (
            Guid recordId,
            AssignRecordRequest request,
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

            if (!await permissionService.CanAccessFormAsync(httpContext.User, formId.Value, PlatformPermissions.Form.Assign, cancellationToken))
            {
                return Results.Forbid();
            }

            return await HandleRecordRequestAsync(async () =>
            {
                var record = await recordMutation.AssignRecordAsync(recordId, request, httpContext.User, GetCurrentUserId(httpContext), permissionService, cancellationToken);
                return Results.Ok(record);
            });
        });

        detailGroup.MapPost("/{recordId:guid}/status", async (
            Guid recordId,
            ChangeRecordStatusRequest request,
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

            if (!await permissionService.CanAccessFormAsync(httpContext.User, formId.Value, PlatformPermissions.Form.ChangeStatus, cancellationToken))
            {
                return Results.Forbid();
            }

            return await HandleRecordRequestAsync(async () =>
            {
                var record = await recordMutation.ChangeStatusAsync(recordId, request, httpContext.User, GetCurrentUserId(httpContext), permissionService, cancellationToken);
                return Results.Ok(record);
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
        catch (FileAttachmentException exception)
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

    private static IReadOnlyDictionary<string, string?> GetLookupDependencyValues(HttpContext httpContext)
    {
        const string Prefix = "dependency.";
        return httpContext.Request.Query
            .Where(pair => pair.Key.StartsWith(Prefix, StringComparison.Ordinal))
            .ToDictionary(
                pair => pair.Key[Prefix.Length..],
                pair => string.IsNullOrWhiteSpace(pair.Value.ToString()) ? null : pair.Value.ToString(),
                StringComparer.Ordinal);
    }

    private static IReadOnlyDictionary<string, string?> GetSubTableFilterValues(HttpContext httpContext)
    {
        const string Prefix = "filter.";
        return httpContext.Request.Query
            .Where(pair => pair.Key.StartsWith(Prefix, StringComparison.Ordinal))
            .ToDictionary(
                pair => pair.Key[Prefix.Length..],
                pair => string.IsNullOrWhiteSpace(pair.Value.ToString()) ? null : pair.Value.ToString(),
                StringComparer.Ordinal);
    }
}

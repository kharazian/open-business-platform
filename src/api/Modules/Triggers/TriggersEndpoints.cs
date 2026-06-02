using System.Security.Claims;
using OpenBusinessPlatform.Api.Modules.Identity;

namespace OpenBusinessPlatform.Api.Modules.Triggers;

public static class TriggersEndpoints
{
    public static IEndpointRouteBuilder MapTriggersEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var formGroup = endpoints.MapGroup("/api/forms/{formId:guid}/triggers").WithTags("Triggers").RequireAuthorization();

        formGroup.MapGet("", async (
            Guid formId,
            TriggerDefinitionService triggerDefinitions,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await CanManageFormAsync(permissionService, httpContext, formId, cancellationToken))
            {
                return Results.Forbid();
            }

            return await HandleTriggerRequestAsync(async () =>
            {
                var triggers = await triggerDefinitions.ListTriggersAsync(formId, cancellationToken);
                return Results.Ok(new { items = triggers });
            });
        });

        formGroup.MapPost("", async (
            Guid formId,
            CreateTriggerRequest request,
            TriggerDefinitionService triggerDefinitions,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await CanManageFormAsync(permissionService, httpContext, formId, cancellationToken))
            {
                return Results.Forbid();
            }

            return await HandleTriggerRequestAsync(async () =>
            {
                var trigger = await triggerDefinitions.CreateTriggerAsync(formId, request, GetCurrentUserId(httpContext), cancellationToken);
                return Results.Created($"/api/triggers/{trigger.Id}", trigger);
            });
        });

        var triggerGroup = endpoints.MapGroup("/api/triggers").WithTags("Triggers").RequireAuthorization();

        triggerGroup.MapGet("/{triggerId:guid}", async (
            Guid triggerId,
            TriggerDefinitionService triggerDefinitions,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var formId = await triggerDefinitions.GetTriggerFormIdAsync(triggerId, cancellationToken);

            if (formId is null)
            {
                return Results.NotFound(new TriggerErrorResponse("Trigger was not found."));
            }

            if (!await CanManageFormAsync(permissionService, httpContext, formId.Value, cancellationToken))
            {
                return Results.Forbid();
            }

            return await HandleTriggerRequestAsync(async () =>
            {
                var trigger = await triggerDefinitions.GetTriggerAsync(triggerId, cancellationToken);
                return Results.Ok(trigger);
            });
        });

        triggerGroup.MapPut("/{triggerId:guid}", async (
            Guid triggerId,
            UpdateTriggerRequest request,
            TriggerDefinitionService triggerDefinitions,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var formId = await triggerDefinitions.GetTriggerFormIdAsync(triggerId, cancellationToken);

            if (formId is null)
            {
                return Results.NotFound(new TriggerErrorResponse("Trigger was not found."));
            }

            if (!await CanManageFormAsync(permissionService, httpContext, formId.Value, cancellationToken))
            {
                return Results.Forbid();
            }

            return await HandleTriggerRequestAsync(async () =>
            {
                var trigger = await triggerDefinitions.UpdateTriggerAsync(triggerId, request, GetCurrentUserId(httpContext), cancellationToken);
                return Results.Ok(trigger);
            });
        });

        triggerGroup.MapGet("/{triggerId:guid}/logs", async (
            Guid triggerId,
            TriggerDefinitionService triggerDefinitions,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var formId = await triggerDefinitions.GetTriggerFormIdAsync(triggerId, cancellationToken);

            if (formId is null)
            {
                return Results.NotFound(new TriggerErrorResponse("Trigger was not found."));
            }

            if (!await CanManageFormAsync(permissionService, httpContext, formId.Value, cancellationToken))
            {
                return Results.Forbid();
            }

            return await HandleTriggerRequestAsync(async () =>
            {
                var logs = await triggerDefinitions.ListTriggerLogsAsync(triggerId, cancellationToken);
                return Results.Ok(new { items = logs });
            });
        });

        return endpoints;
    }

    private static async Task<bool> CanManageFormAsync(
        PermissionService permissionService,
        HttpContext httpContext,
        Guid formId,
        CancellationToken cancellationToken)
    {
        return await permissionService.CanAccessFormAsync(httpContext.User, formId, PlatformPermissions.Form.Manage, cancellationToken);
    }

    private static async Task<IResult> HandleTriggerRequestAsync(Func<Task<IResult>> action)
    {
        try
        {
            return await action();
        }
        catch (TriggerManagementException exception)
        {
            var errors = exception.Errors.Count == 0 ? null : exception.Errors;
            return Results.Json(new TriggerErrorResponse(exception.Message, errors), statusCode: exception.StatusCode);
        }
    }

    private static Guid? GetCurrentUserId(HttpContext httpContext)
    {
        var value = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}

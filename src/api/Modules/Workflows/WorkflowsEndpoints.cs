using System.Security.Claims;
using OpenBusinessPlatform.Api.Modules.Identity;

namespace OpenBusinessPlatform.Api.Modules.Workflows;

public static class WorkflowsEndpoints
{
    public static IEndpointRouteBuilder MapWorkflowsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var formGroup = endpoints.MapGroup("/api/forms/{formId:guid}/workflows").WithTags("Workflows").RequireAuthorization();

        formGroup.MapGet("", async (
            Guid formId,
            WorkflowDefinitionService workflows,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await CanManageFormAsync(permissionService, httpContext, formId, cancellationToken))
            {
                return Results.Forbid();
            }

            return await HandleWorkflowRequestAsync(async () =>
            {
                var items = await workflows.ListWorkflowsAsync(formId, cancellationToken);
                return Results.Ok(new { items });
            });
        });

        formGroup.MapPost("", async (
            Guid formId,
            CreateWorkflowDefinitionRequest request,
            WorkflowDefinitionService workflows,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await CanManageFormAsync(permissionService, httpContext, formId, cancellationToken))
            {
                return Results.Forbid();
            }

            return await HandleWorkflowRequestAsync(async () =>
            {
                var workflow = await workflows.CreateWorkflowAsync(formId, request, GetCurrentUserId(httpContext), cancellationToken);
                return Results.Created($"/api/workflows/{workflow.Id}", workflow);
            });
        });

        var workflowGroup = endpoints.MapGroup("/api/workflows").WithTags("Workflows").RequireAuthorization();

        workflowGroup.MapGet("/{workflowId:guid}", async (
            Guid workflowId,
            WorkflowDefinitionService workflows,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var formId = await workflows.GetWorkflowFormIdAsync(workflowId, cancellationToken);

            if (formId is null)
            {
                return Results.NotFound(new WorkflowErrorResponse("Workflow was not found."));
            }

            if (!await CanManageFormAsync(permissionService, httpContext, formId.Value, cancellationToken))
            {
                return Results.Forbid();
            }

            return await HandleWorkflowRequestAsync(async () =>
            {
                var workflow = await workflows.GetWorkflowAsync(workflowId, cancellationToken);
                return Results.Ok(workflow);
            });
        });

        workflowGroup.MapPut("/{workflowId:guid}", async (
            Guid workflowId,
            UpdateWorkflowDefinitionRequest request,
            WorkflowDefinitionService workflows,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var formId = await workflows.GetWorkflowFormIdAsync(workflowId, cancellationToken);

            if (formId is null)
            {
                return Results.NotFound(new WorkflowErrorResponse("Workflow was not found."));
            }

            if (!await CanManageFormAsync(permissionService, httpContext, formId.Value, cancellationToken))
            {
                return Results.Forbid();
            }

            return await HandleWorkflowRequestAsync(async () =>
            {
                var workflow = await workflows.UpdateWorkflowAsync(workflowId, request, GetCurrentUserId(httpContext), cancellationToken);
                return Results.Ok(workflow);
            });
        });

        workflowGroup.MapPost("/{workflowId:guid}/publish", async (
            Guid workflowId,
            WorkflowStateChangeRequest request,
            WorkflowDefinitionService workflows,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var formId = await workflows.GetWorkflowFormIdAsync(workflowId, cancellationToken);

            if (formId is null)
            {
                return Results.NotFound(new WorkflowErrorResponse("Workflow was not found."));
            }

            if (!await CanManageFormAsync(permissionService, httpContext, formId.Value, cancellationToken))
            {
                return Results.Forbid();
            }

            return await HandleWorkflowRequestAsync(async () =>
            {
                var workflow = await workflows.PublishWorkflowAsync(workflowId, request, GetCurrentUserId(httpContext), cancellationToken);
                return Results.Ok(workflow);
            });
        });

        workflowGroup.MapPost("/{workflowId:guid}/enable", async (
            Guid workflowId,
            WorkflowStateChangeRequest request,
            WorkflowDefinitionService workflows,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var formId = await workflows.GetWorkflowFormIdAsync(workflowId, cancellationToken);

            if (formId is null)
            {
                return Results.NotFound(new WorkflowErrorResponse("Workflow was not found."));
            }

            if (!await CanManageFormAsync(permissionService, httpContext, formId.Value, cancellationToken))
            {
                return Results.Forbid();
            }

            return await HandleWorkflowRequestAsync(async () =>
            {
                var workflow = await workflows.EnableWorkflowAsync(workflowId, request, GetCurrentUserId(httpContext), cancellationToken);
                return Results.Ok(workflow);
            });
        });

        workflowGroup.MapPost("/{workflowId:guid}/disable", async (
            Guid workflowId,
            WorkflowStateChangeRequest request,
            WorkflowDefinitionService workflows,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var formId = await workflows.GetWorkflowFormIdAsync(workflowId, cancellationToken);

            if (formId is null)
            {
                return Results.NotFound(new WorkflowErrorResponse("Workflow was not found."));
            }

            if (!await CanManageFormAsync(permissionService, httpContext, formId.Value, cancellationToken))
            {
                return Results.Forbid();
            }

            return await HandleWorkflowRequestAsync(async () =>
            {
                var workflow = await workflows.DisableWorkflowAsync(workflowId, request, GetCurrentUserId(httpContext), cancellationToken);
                return Results.Ok(workflow);
            });
        });

        var recordWorkflowGroup = endpoints.MapGroup("/api/records/{recordId:guid}/workflow").WithTags("Workflows").RequireAuthorization();

        recordWorkflowGroup.MapGet("", async (
            Guid recordId,
            RecordWorkflowService recordWorkflows,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            return await HandleRecordWorkflowRequestAsync(async () =>
            {
                var state = await recordWorkflows.GetRecordWorkflowAsync(recordId, httpContext.User, permissionService, cancellationToken);
                return Results.Ok(state);
            });
        });

        recordWorkflowGroup.MapPost("/start", async (
            Guid recordId,
            StartRecordWorkflowRequest request,
            RecordWorkflowService recordWorkflows,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            return await HandleRecordWorkflowRequestAsync(async () =>
            {
                var state = await recordWorkflows.StartRecordWorkflowAsync(
                    recordId,
                    request,
                    httpContext.User,
                    GetCurrentUserId(httpContext),
                    permissionService,
                    cancellationToken);

                return Results.Ok(state);
            });
        });

        recordWorkflowGroup.MapPost("/transitions/{transitionKey}", async (
            Guid recordId,
            string transitionKey,
            ExecuteRecordWorkflowTransitionRequest request,
            RecordWorkflowService recordWorkflows,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            return await HandleRecordWorkflowRequestAsync(async () =>
            {
                var state = await recordWorkflows.ExecuteTransitionAsync(
                    recordId,
                    transitionKey,
                    request,
                    httpContext.User,
                    GetCurrentUserId(httpContext),
                    permissionService,
                    cancellationToken);

                return Results.Ok(state);
            });
        });

        var approvalGroup = endpoints.MapGroup("/api/workflow-approvals").WithTags("Workflows").RequireAuthorization();

        approvalGroup.MapGet("", async (
            WorkflowApprovalService approvals,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var userId = GetCurrentUserId(httpContext);
            if (userId is null)
            {
                return Results.Ok(new { items = Array.Empty<WorkflowApprovalTaskDto>() });
            }

            var items = await approvals.ListForCurrentUserAsync(userId.Value, cancellationToken);
            return Results.Ok(new { items });
        });

        approvalGroup.MapPost("/{approvalTaskId:guid}/approve", async (
            Guid approvalTaskId,
            RespondWorkflowApprovalRequest request,
            WorkflowApprovalService approvals,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var userId = GetCurrentUserId(httpContext);
            if (userId is null)
            {
                return Results.NotFound(new WorkflowErrorResponse("Workflow approval task was not found."));
            }

            return await HandleRecordWorkflowRequestAsync(async () =>
            {
                var task = await approvals.ApproveAsync(approvalTaskId, userId.Value, request, cancellationToken);
                return Results.Ok(task);
            });
        });

        approvalGroup.MapPost("/{approvalTaskId:guid}/reject", async (
            Guid approvalTaskId,
            RespondWorkflowApprovalRequest request,
            WorkflowApprovalService approvals,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var userId = GetCurrentUserId(httpContext);
            if (userId is null)
            {
                return Results.NotFound(new WorkflowErrorResponse("Workflow approval task was not found."));
            }

            return await HandleRecordWorkflowRequestAsync(async () =>
            {
                var task = await approvals.RejectAsync(approvalTaskId, userId.Value, request, cancellationToken);
                return Results.Ok(task);
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

    private static async Task<IResult> HandleWorkflowRequestAsync(Func<Task<IResult>> action)
    {
        try
        {
            return await action();
        }
        catch (WorkflowManagementException exception)
        {
            var errors = exception.Errors.Count == 0 ? null : exception.Errors;
            return Results.Json(new WorkflowErrorResponse(exception.Message, errors), statusCode: exception.StatusCode);
        }
    }

    private static async Task<IResult> HandleRecordWorkflowRequestAsync(Func<Task<IResult>> action)
    {
        try
        {
            return await action();
        }
        catch (RecordWorkflowException exception)
        {
            return Results.Json(new WorkflowErrorResponse(exception.Message), statusCode: exception.StatusCode);
        }
    }

    private static Guid? GetCurrentUserId(HttpContext httpContext)
    {
        var value = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}

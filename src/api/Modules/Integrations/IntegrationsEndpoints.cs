using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using OpenBusinessPlatform.Api.Modules.Identity;
using OpenBusinessPlatform.Api.Modules.Records;

namespace OpenBusinessPlatform.Api.Modules.Integrations;

public static class IntegrationsEndpoints
{
    public static IEndpointRouteBuilder MapIntegrationsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/integrations/api-keys")
            .WithTags("Integrations")
            .RequireAuthorization();

        group.MapGet("", async (
            IntegrationApiKeyService apiKeys,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await CanManageIntegrationsAsync(permissionService, httpContext, cancellationToken))
            {
                return Results.Forbid();
            }

            return Results.Ok(new { items = await apiKeys.ListAsync(cancellationToken) });
        });

        group.MapGet("/{apiKeyId:guid}", async (
            Guid apiKeyId,
            IntegrationApiKeyService apiKeys,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await CanManageIntegrationsAsync(permissionService, httpContext, cancellationToken))
            {
                return Results.Forbid();
            }

            var apiKey = await apiKeys.GetAsync(apiKeyId, cancellationToken);
            return apiKey is null ? Results.NotFound() : Results.Ok(apiKey);
        });

        group.MapPost("", async (
            CreateIntegrationApiKeyRequest request,
            IntegrationApiKeyService apiKeys,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await CanManageIntegrationsAsync(permissionService, httpContext, cancellationToken))
            {
                return Results.Forbid();
            }

            return await HandleIntegrationRequestAsync(async () =>
            {
                var created = await apiKeys.CreateAsync(request, GetCurrentUserId(httpContext), cancellationToken);
                return Results.Created($"/api/integrations/api-keys/{created.ApiKey.Id}", created);
            });
        });

        group.MapPost("/{apiKeyId:guid}/revoke", async (
            Guid apiKeyId,
            RevokeIntegrationApiKeyRequest request,
            IntegrationApiKeyService apiKeys,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await CanManageIntegrationsAsync(permissionService, httpContext, cancellationToken))
            {
                return Results.Forbid();
            }

            return await HandleIntegrationRequestAsync(async () =>
            {
                var revoked = await apiKeys.RevokeAsync(apiKeyId, request, GetCurrentUserId(httpContext), cancellationToken);
                return revoked is null ? Results.NotFound() : Results.Ok(revoked);
            });
        });

        group.MapPost("/{apiKeyId:guid}/rotate", async (
            Guid apiKeyId,
            RotateIntegrationApiKeyRequest request,
            IntegrationApiKeyService apiKeys,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await CanManageIntegrationsAsync(permissionService, httpContext, cancellationToken))
            {
                return Results.Forbid();
            }

            return await HandleIntegrationRequestAsync(async () =>
            {
                var rotated = await apiKeys.RotateAsync(apiKeyId, request, GetCurrentUserId(httpContext), cancellationToken);
                return rotated is null ? Results.NotFound() : Results.Ok(rotated);
            });
        });

        var logs = endpoints.MapGroup("/api/integrations/logs")
            .WithTags("Integrations")
            .RequireAuthorization();

        logs.MapGet("", async (
            IntegrationLogService integrationLogs,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await CanManageIntegrationsAsync(permissionService, httpContext, cancellationToken))
            {
                return Results.Forbid();
            }

            return Results.Ok(new { items = await integrationLogs.ListAsync(cancellationToken) });
        });

        logs.MapGet("/{logId:guid}", async (
            Guid logId,
            IntegrationLogService integrationLogs,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await CanManageIntegrationsAsync(permissionService, httpContext, cancellationToken))
            {
                return Results.Forbid();
            }

            var log = await integrationLogs.GetAsync(logId, cancellationToken);
            return log is null ? Results.NotFound() : Results.Ok(log);
        });

        logs.MapPost("/{logId:guid}/retry-request", async (
            Guid logId,
            RequestIntegrationRetryRequest request,
            IntegrationLogService integrationLogs,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await CanManageIntegrationsAsync(permissionService, httpContext, cancellationToken))
            {
                return Results.Forbid();
            }

            return await HandleIntegrationRequestAsync(async () =>
            {
                var log = await integrationLogs.RequestRetryAsync(logId, request, GetCurrentUserId(httpContext), cancellationToken);
                return log is null ? Results.NotFound() : Results.Ok(log);
            });
        });

        var publicRecords = endpoints.MapGroup($"/api/integration/{PublicRecordApiVersions.V1}")
            .WithTags("Integration Records")
            .RequireAuthorization(new AuthorizeAttribute
            {
                AuthenticationSchemes = IntegrationApiKeyAuthenticationDefaults.AuthenticationScheme
            });

        publicRecords.MapGet("/forms/{formId:guid}/records", async (
            Guid formId,
            int? page,
            int? pageSize,
            string? search,
            PublicRecordApiService publicRecordApi,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            return await HandleIntegrationRequestAsync(async () =>
            {
                var records = await publicRecordApi.ListRecordsAsync(
                    httpContext.User,
                    formId,
                    new ListRecordsRequest(page ?? 1, pageSize ?? 25, search),
                    cancellationToken);

                return Results.Ok(records);
            });
        });

        publicRecords.MapPost("/forms/{formId:guid}/records", async (
            Guid formId,
            PublicCreateRecordRequest request,
            PublicRecordApiService publicRecordApi,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            return await HandleIntegrationRequestAsync(async () =>
            {
                var record = await publicRecordApi.CreateRecordAsync(httpContext.User, formId, request, cancellationToken);
                return Results.Created($"/api/integration/{PublicRecordApiVersions.V1}/records/{record.Id}", record);
            });
        });

        publicRecords.MapGet("/records/{recordId:guid}", async (
            Guid recordId,
            PublicRecordApiService publicRecordApi,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            return await HandleIntegrationRequestAsync(async () => Results.Ok(await publicRecordApi.GetRecordAsync(httpContext.User, recordId, cancellationToken)));
        });

        return endpoints;
    }

    private static async Task<bool> CanManageIntegrationsAsync(
        PermissionService permissionService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        return await permissionService.CanAsync(httpContext.User, PlatformPermissions.Integrations.Manage, cancellationToken);
    }

    private static async Task<IResult> HandleIntegrationRequestAsync(Func<Task<IResult>> action)
    {
        try
        {
            return await action();
        }
        catch (IntegrationApiKeyException exception)
        {
            return Results.Json(new IntegrationApiKeyErrorResponse(exception.Message), statusCode: exception.StatusCode);
        }
        catch (RecordSubmissionException exception)
        {
            return Results.Json(new RecordErrorResponse(exception.Message, exception.Errors), statusCode: exception.StatusCode);
        }
        catch (RecordQueryException exception)
        {
            return Results.Json(new RecordErrorResponse(exception.Message, exception.Errors), statusCode: exception.StatusCode);
        }
    }

    private static Guid? GetCurrentUserId(HttpContext httpContext)
    {
        var value = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}

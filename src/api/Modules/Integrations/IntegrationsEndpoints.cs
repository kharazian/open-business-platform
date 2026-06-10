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

        var webhookListeners = endpoints.MapGroup("/api/integrations/webhooks")
            .WithTags("Integrations")
            .RequireAuthorization();

        webhookListeners.MapGet("", async (
            IncomingWebhookListenerService listeners,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await CanManageIntegrationsAsync(permissionService, httpContext, cancellationToken))
            {
                return Results.Forbid();
            }

            return Results.Ok(new { items = await listeners.ListAsync(cancellationToken) });
        });

        webhookListeners.MapGet("/{listenerId:guid}", async (
            Guid listenerId,
            IncomingWebhookListenerService listeners,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await CanManageIntegrationsAsync(permissionService, httpContext, cancellationToken))
            {
                return Results.Forbid();
            }

            var listener = await listeners.GetAsync(listenerId, cancellationToken);
            return listener is null ? Results.NotFound() : Results.Ok(listener);
        });

        webhookListeners.MapPost("", async (
            UpsertIncomingWebhookListenerRequest request,
            IncomingWebhookListenerService listeners,
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
                var created = await listeners.CreateAsync(request, GetCurrentUserId(httpContext), cancellationToken);
                return Results.Created($"/api/integrations/webhooks/{created.Listener.Id}", created);
            });
        });

        webhookListeners.MapPut("/{listenerId:guid}", async (
            Guid listenerId,
            UpsertIncomingWebhookListenerRequest request,
            IncomingWebhookListenerService listeners,
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
                var updated = await listeners.UpdateAsync(listenerId, request, GetCurrentUserId(httpContext), cancellationToken);
                return updated is null ? Results.NotFound() : Results.Ok(updated);
            });
        });

        webhookListeners.MapPost("/{listenerId:guid}/rotate-secret", async (
            Guid listenerId,
            IncomingWebhookListenerService listeners,
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
                var rotated = await listeners.RotateSecretAsync(listenerId, GetCurrentUserId(httpContext), cancellationToken);
                return rotated is null ? Results.NotFound() : Results.Ok(rotated);
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

        var incomingWebhooks = endpoints.MapGroup($"/api/integration/{PublicRecordApiVersions.V1}/webhooks")
            .WithTags("Integration Webhooks");

        incomingWebhooks.MapPost("/{listenerKey}", async (
            string listenerKey,
            Dictionary<string, object?> payload,
            IncomingWebhookExecutionService incomingWebhooks,
            IntegrationApiKeyService apiKeys,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            return await HandleIntegrationRequestAsync(async () =>
            {
                var principal = await AuthenticateOptionalApiKeyAsync(apiKeys, httpContext, cancellationToken);
                var rawListenerSecret = httpContext.Request.Headers["X-OBP-Webhook-Secret"].FirstOrDefault()?.Trim();
                var response = await incomingWebhooks.ReceiveAsync(
                    principal,
                    listenerKey,
                    payload,
                    rawListenerSecret,
                    cancellationToken);

                return Results.Ok(response);
            });
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
        catch (RecordMutationException exception)
        {
            return Results.Json(new RecordErrorResponse(exception.Message, exception.Errors), statusCode: exception.StatusCode);
        }
        catch (IncomingWebhookException exception)
        {
            return Results.Json(new IntegrationApiKeyErrorResponse(exception.Message), statusCode: exception.StatusCode);
        }
    }

    private static async Task<ClaimsPrincipal> AuthenticateOptionalApiKeyAsync(
        IntegrationApiKeyService apiKeys,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var rawKey = GetRawApiKey(httpContext);
        if (string.IsNullOrWhiteSpace(rawKey))
        {
            return new ClaimsPrincipal(new ClaimsIdentity());
        }

        var result = await apiKeys.AuthenticateAsync(
            rawKey,
            new IntegrationApiKeyUsageContext(
                httpContext.Connection.RemoteIpAddress?.ToString(),
                httpContext.Request.Headers.UserAgent.FirstOrDefault()),
            cancellationToken);

        if (!result.Succeeded || result.Principal is null)
        {
            throw new IntegrationApiKeyException(StatusCodes.Status401Unauthorized, result.FailureReason ?? "API key authentication failed.");
        }

        return result.Principal;
    }

    private static string? GetRawApiKey(HttpContext httpContext)
    {
        const string bearerPrefix = "Bearer ";
        var authorization = httpContext.Request.Headers.Authorization.FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(authorization)
            && authorization.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return authorization[bearerPrefix.Length..].Trim();
        }

        return httpContext.Request.Headers["X-OBP-API-Key"].FirstOrDefault()?.Trim();
    }

    private static Guid? GetCurrentUserId(HttpContext httpContext)
    {
        var value = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}

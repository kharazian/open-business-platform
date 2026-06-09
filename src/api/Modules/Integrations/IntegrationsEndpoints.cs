using System.Security.Claims;
using OpenBusinessPlatform.Api.Modules.Identity;

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
    }

    private static Guid? GetCurrentUserId(HttpContext httpContext)
    {
        var value = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}

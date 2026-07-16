using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using OpenBusinessPlatform.Api.Modules.Identity;

namespace OpenBusinessPlatform.Api.Modules.Workspaces;

public sealed class WorkspaceMembershipMiddleware(
    RequestDelegate next,
    ILogger<WorkspaceMembershipMiddleware> logger)
{
    public async Task InvokeAsync(
        HttpContext httpContext,
        WorkspaceMembershipService memberships,
        IWorkspaceContext workspaceContext)
    {
        var principal = httpContext.User;
        var isCookieSession = principal.Identity?.IsAuthenticated == true
            && string.Equals(principal.Identity.AuthenticationType, CookieAuthenticationDefaults.AuthenticationScheme, StringComparison.Ordinal);

        if (isCookieSession)
        {
            var subject = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var allowed = subject == BootstrapAdminUserDirectory.BootstrapAdminId
                ? workspaceContext.WorkspaceId == WorkspaceDefaults.WorkspaceId
                : WorkspaceMembershipService.GetUserId(principal) is { } userId
                    && await memberships.IsActiveMemberAsync(userId, workspaceContext.WorkspaceId, httpContext.RequestAborted);

            if (!allowed)
            {
                logger.LogWarning("Rejected cookie session without active workspace membership for workspace {WorkspaceId}.", workspaceContext.WorkspaceId);
                httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                await httpContext.Response.WriteAsJsonAsync(new { message = "Active workspace membership is required." }, httpContext.RequestAborted);
                return;
            }
        }

        await next(httpContext);
    }
}

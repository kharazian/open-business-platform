using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;

namespace OpenBusinessPlatform.Api.Modules.Workspaces;

public sealed class CustomDomainResolutionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, OpenBusinessPlatformDbContext dbContext)
    {
        var host = context.Request.Host.Host.Trim().TrimEnd('.').ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(host))
        {
            var registration = await dbContext.WorkspaceCustomDomains.IgnoreQueryFilters().AsNoTracking()
                .Where(item => item.Hostname == host)
                .Select(item => new { item.WorkspaceId, item.Status, item.IsEnabled }).SingleOrDefaultAsync(context.RequestAborted);
            if (registration is not null && (registration.Status != CustomDomainStatuses.Verified || !registration.IsEnabled))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }
            if (registration is not null)
            {
                var resolved = registration.WorkspaceId;
                var claim = context.User.FindFirstValue(WorkspaceClaims.WorkspaceId);
                if (Guid.TryParse(claim, out var claimed) && claimed != resolved)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsJsonAsync(new { message = "The signed workspace does not match this custom domain." }, context.RequestAborted);
                    return;
                }
                context.Items[HttpContextWorkspaceContext.ResolvedDomainWorkspaceItemKey] = resolved;
            }
        }
        await next(context);
    }
}

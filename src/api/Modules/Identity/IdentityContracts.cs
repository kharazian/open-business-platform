using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using OpenBusinessPlatform.Api.Modules.Workspaces;

namespace OpenBusinessPlatform.Api.Modules.Identity;

public static class PlatformRoles
{
    public const string Admin = "Admin";
    public const string Builder = "Builder";
    public const string User = "User";
    public const string Viewer = "Viewer";
}

public sealed record LoginRequest(string Email, string Password);

public sealed record RequestPasswordResetRequest(string Email);

public sealed record PasswordResetRequestedResponse(string Message);

public sealed record CompletePasswordResetRequest(string Token, string NewPassword);

public sealed record AuthenticatedUser(
    string Id,
    string Name,
    string Email,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string>? Permissions = null)
{
    public AuthenticatedUserResponse ToResponse(Guid workspaceId)
    {
        return new AuthenticatedUserResponse(Id, Name, Email, Roles, Permissions ?? Array.Empty<string>(), workspaceId);
    }
}

public sealed record AuthenticatedUserResponse(
    string Id,
    string Name,
    string Email,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions,
    Guid WorkspaceId);

public sealed record AuthSessionResponse(AuthenticatedUserResponse User);

public sealed record AuthErrorResponse(string Message);

public static class IdentityPrincipalFactory
{
    public static ClaimsPrincipal Create(AuthenticatedUser user, Guid workspaceId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email),
            new(WorkspaceClaims.WorkspaceId, workspaceId.ToString())
        };

        claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }
}

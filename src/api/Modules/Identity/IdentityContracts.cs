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
    public AuthenticatedUserResponse ToResponse()
    {
        return new AuthenticatedUserResponse(Id, Name, Email, Roles, Permissions ?? Array.Empty<string>());
    }
}

public sealed record AuthenticatedUserResponse(
    string Id,
    string Name,
    string Email,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);

public sealed record AuthSessionResponse(AuthenticatedUserResponse User);

public sealed record AuthErrorResponse(string Message);

namespace OpenBusinessPlatform.Api.Modules.Identity;

public static class PlatformRoles
{
    public const string Admin = "Admin";
    public const string Builder = "Builder";
    public const string User = "User";
    public const string Viewer = "Viewer";
}

public sealed record LoginRequest(string Email, string Password);

public sealed record AuthenticatedUser(
    string Id,
    string Name,
    string Email,
    IReadOnlyCollection<string> Roles)
{
    public AuthenticatedUserResponse ToResponse()
    {
        return new AuthenticatedUserResponse(Id, Name, Email, Roles);
    }
}

public sealed record AuthenticatedUserResponse(
    string Id,
    string Name,
    string Email,
    IReadOnlyCollection<string> Roles);

public sealed record AuthSessionResponse(AuthenticatedUserResponse User);

public sealed record AuthErrorResponse(string Message);

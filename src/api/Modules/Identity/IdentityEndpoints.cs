using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace OpenBusinessPlatform.Api.Modules.Identity;

public static class IdentityEndpoints
{
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/login", async (
            LoginRequest request,
            BootstrapAdminUserDirectory userDirectory,
            HttpContext httpContext) => await SignInAsync(request, userDirectory, httpContext));
        group.MapGet("/me", GetCurrentUser).RequireAuthorization();
        group.MapPost("/logout", async (HttpContext httpContext) => await SignOutAsync(httpContext)).RequireAuthorization();

        return endpoints;
    }

    private static async Task<IResult> SignInAsync(
        LoginRequest request,
        BootstrapAdminUserDirectory userDirectory,
        HttpContext httpContext)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest(new AuthErrorResponse("Email and password are required."));
        }

        var user = userDirectory.ValidateCredentials(request);

        if (user is null)
        {
            return Results.Json(new AuthErrorResponse("Invalid email or password."), statusCode: StatusCodes.Status401Unauthorized);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email)
        };

        claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IssuedUtc = DateTimeOffset.UtcNow,
                IsPersistent = false
            });

        return Results.Ok(new AuthSessionResponse(user.ToResponse()));
    }

    private static IResult GetCurrentUser(ClaimsPrincipal principal)
    {
        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var name = principal.FindFirst(ClaimTypes.Name)?.Value;
        var email = principal.FindFirst(ClaimTypes.Email)?.Value;

        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
        {
            return Results.Json(new AuthErrorResponse("Authentication required."), statusCode: StatusCodes.Status401Unauthorized);
        }

        var user = new AuthenticatedUserResponse(
            userId,
            name,
            email,
            principal.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray());

        return Results.Ok(new AuthSessionResponse(user));
    }

    private static async Task<IResult> SignOutAsync(HttpContext httpContext)
    {
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return Results.NoContent();
    }
}

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
            IdentityManagementService identityManagement,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) => await SignInAsync(request, userDirectory, identityManagement, permissionService, httpContext, cancellationToken));
        group.MapPost("/forgot-password", async (
            RequestPasswordResetRequest request,
            IPasswordRecoveryService passwordRecovery,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            return await HandleIdentityRequestAsync(async () =>
            {
                await passwordRecovery.RequestResetAsync(request, httpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
                return Results.Ok(PasswordRecoveryService.CreateGenericResponse());
            });
        });
        group.MapPost("/reset-password", async (
            CompletePasswordResetRequest request,
            IPasswordRecoveryService passwordRecovery,
            CancellationToken cancellationToken) =>
        {
            return await HandleIdentityRequestAsync(async () =>
            {
                await passwordRecovery.CompleteResetAsync(request, cancellationToken);
                return Results.NoContent();
            });
        });
        group.MapGet("/me", GetCurrentUserAsync).RequireAuthorization();
        group.MapPost("/logout", async (HttpContext httpContext) => await SignOutAsync(httpContext)).RequireAuthorization();

        var users = endpoints.MapGroup("/api/users").WithTags("Users").RequireAuthorization();
        users.MapGet("/", async (
            IdentityManagementService identityManagement,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await permissionService.CanAsync(httpContext.User, PlatformPermissions.Users.Manage, cancellationToken))
            {
                return Results.Forbid();
            }

            return Results.Ok(new { items = await identityManagement.ListUsersAsync(cancellationToken) });
        });
        users.MapPost("/", async (
            CreateUserRequest request,
            IdentityManagementService identityManagement,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await permissionService.CanAsync(httpContext.User, PlatformPermissions.Users.Manage, cancellationToken))
            {
                return Results.Forbid();
            }

            return await HandleIdentityRequestAsync(async () => Results.Created(
                "/api/users",
                await identityManagement.CreateUserAsync(request, cancellationToken)));
        });
        users.MapGet("/{userId:guid}", async (
            Guid userId,
            IdentityManagementService identityManagement,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await permissionService.CanAsync(httpContext.User, PlatformPermissions.Users.Manage, cancellationToken))
            {
                return Results.Forbid();
            }

            var user = await identityManagement.GetUserAsync(userId, cancellationToken);
            return user is null ? Results.NotFound() : Results.Ok(user);
        });
        users.MapPut("/{userId:guid}", async (
            Guid userId,
            UpdateUserRequest request,
            IdentityManagementService identityManagement,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await permissionService.CanAsync(httpContext.User, PlatformPermissions.Users.Manage, cancellationToken))
            {
                return Results.Forbid();
            }

            return await HandleIdentityRequestAsync(async () =>
            {
                var user = await identityManagement.UpdateUserAsync(userId, request, cancellationToken);
                return user is null ? Results.NotFound() : Results.Ok(user);
            });
        });
        users.MapPost("/{userId:guid}/reset-password", async (
            Guid userId,
            ResetUserPasswordRequest request,
            IdentityManagementService identityManagement,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await permissionService.CanAsync(httpContext.User, PlatformPermissions.Users.Manage, cancellationToken))
            {
                return Results.Forbid();
            }

            return await HandleIdentityRequestAsync(async () =>
                await identityManagement.ResetPasswordAsync(userId, request, cancellationToken)
                    ? Results.NoContent()
                    : Results.NotFound());
        });

        var roles = endpoints.MapGroup("/api/roles").WithTags("Roles").RequireAuthorization();
        roles.MapGet("/", async (
            IdentityManagementService identityManagement,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await permissionService.CanAsync(httpContext.User, PlatformPermissions.Roles.Manage, cancellationToken))
            {
                return Results.Forbid();
            }

            return Results.Ok(new { items = await identityManagement.ListRolesAsync(cancellationToken) });
        });
        roles.MapPost("/", async (
            CreateRoleRequest request,
            IdentityManagementService identityManagement,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await permissionService.CanAsync(httpContext.User, PlatformPermissions.Roles.Manage, cancellationToken))
            {
                return Results.Forbid();
            }

            return await HandleIdentityRequestAsync(async () => Results.Created(
                "/api/roles",
                await identityManagement.CreateRoleAsync(request, cancellationToken)));
        });
        roles.MapGet("/{roleId:guid}", async (
            Guid roleId,
            IdentityManagementService identityManagement,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await permissionService.CanAsync(httpContext.User, PlatformPermissions.Roles.Manage, cancellationToken))
            {
                return Results.Forbid();
            }

            var role = await identityManagement.GetRoleAsync(roleId, cancellationToken);
            return role is null ? Results.NotFound() : Results.Ok(role);
        });
        roles.MapPut("/{roleId:guid}", async (
            Guid roleId,
            UpdateRoleRequest request,
            IdentityManagementService identityManagement,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await permissionService.CanAsync(httpContext.User, PlatformPermissions.Roles.Manage, cancellationToken))
            {
                return Results.Forbid();
            }

            return await HandleIdentityRequestAsync(async () =>
            {
                var role = await identityManagement.UpdateRoleAsync(roleId, request, cancellationToken);
                return role is null ? Results.NotFound() : Results.Ok(role);
            });
        });
        roles.MapGet("/{roleId:guid}/permissions", async (
            Guid roleId,
            IdentityManagementService identityManagement,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await permissionService.CanAsync(httpContext.User, PlatformPermissions.Roles.Manage, cancellationToken))
            {
                return Results.Forbid();
            }

            var permissions = await identityManagement.GetRolePermissionsAsync(roleId, cancellationToken);
            return permissions is null ? Results.NotFound() : Results.Ok(permissions);
        });
        roles.MapPut("/{roleId:guid}/permissions", async (
            Guid roleId,
            UpdateRolePermissionsRequest request,
            IdentityManagementService identityManagement,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await permissionService.CanAsync(httpContext.User, PlatformPermissions.Roles.Manage, cancellationToken))
            {
                return Results.Forbid();
            }

            return await HandleIdentityRequestAsync(async () =>
            {
                var permissions = await identityManagement.UpdateRolePermissionsAsync(roleId, request, cancellationToken);
                return permissions is null ? Results.NotFound() : Results.Ok(permissions);
            });
        });

        var groups = endpoints.MapGroup("/api/groups").WithTags("Groups").RequireAuthorization();
        groups.MapGet("/", async (
            IdentityManagementService identityManagement,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await permissionService.CanAsync(httpContext.User, PlatformPermissions.Users.Manage, cancellationToken))
            {
                return Results.Forbid();
            }

            return Results.Ok(new { items = await identityManagement.ListGroupsAsync(cancellationToken) });
        });
        groups.MapPost("/", async (
            CreateGroupRequest request,
            IdentityManagementService identityManagement,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await permissionService.CanAsync(httpContext.User, PlatformPermissions.Users.Manage, cancellationToken))
            {
                return Results.Forbid();
            }

            return await HandleIdentityRequestAsync(async () => Results.Created(
                "/api/groups",
                await identityManagement.CreateGroupAsync(request, cancellationToken)));
        });
        groups.MapGet("/{groupId:guid}", async (
            Guid groupId,
            IdentityManagementService identityManagement,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await permissionService.CanAsync(httpContext.User, PlatformPermissions.Users.Manage, cancellationToken))
            {
                return Results.Forbid();
            }

            var group = await identityManagement.GetGroupAsync(groupId, cancellationToken);
            return group is null ? Results.NotFound() : Results.Ok(group);
        });
        groups.MapPut("/{groupId:guid}", async (
            Guid groupId,
            UpdateGroupRequest request,
            IdentityManagementService identityManagement,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await permissionService.CanAsync(httpContext.User, PlatformPermissions.Users.Manage, cancellationToken))
            {
                return Results.Forbid();
            }

            return await HandleIdentityRequestAsync(async () =>
            {
                var group = await identityManagement.UpdateGroupAsync(groupId, request, cancellationToken);
                return group is null ? Results.NotFound() : Results.Ok(group);
            });
        });

        var departments = endpoints.MapGroup("/api/departments").WithTags("Departments").RequireAuthorization();
        departments.MapGet("/", async (
            IdentityManagementService identityManagement,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await permissionService.CanAsync(httpContext.User, PlatformPermissions.Users.Manage, cancellationToken))
            {
                return Results.Forbid();
            }

            return Results.Ok(new { items = await identityManagement.ListDepartmentsAsync(cancellationToken) });
        });
        departments.MapPost("/", async (
            CreateDepartmentRequest request,
            IdentityManagementService identityManagement,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await permissionService.CanAsync(httpContext.User, PlatformPermissions.Users.Manage, cancellationToken))
            {
                return Results.Forbid();
            }

            return await HandleIdentityRequestAsync(async () => Results.Created(
                "/api/departments",
                await identityManagement.CreateDepartmentAsync(request, cancellationToken)));
        });
        departments.MapGet("/{departmentId:guid}", async (
            Guid departmentId,
            IdentityManagementService identityManagement,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await permissionService.CanAsync(httpContext.User, PlatformPermissions.Users.Manage, cancellationToken))
            {
                return Results.Forbid();
            }

            var department = await identityManagement.GetDepartmentAsync(departmentId, cancellationToken);
            return department is null ? Results.NotFound() : Results.Ok(department);
        });
        departments.MapPut("/{departmentId:guid}", async (
            Guid departmentId,
            UpdateDepartmentRequest request,
            IdentityManagementService identityManagement,
            PermissionService permissionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!await permissionService.CanAsync(httpContext.User, PlatformPermissions.Users.Manage, cancellationToken))
            {
                return Results.Forbid();
            }

            return await HandleIdentityRequestAsync(async () =>
            {
                var department = await identityManagement.UpdateDepartmentAsync(departmentId, request, cancellationToken);
                return department is null ? Results.NotFound() : Results.Ok(department);
            });
        });

        return endpoints;
    }

    private static async Task<IResult> SignInAsync(
        LoginRequest request,
        BootstrapAdminUserDirectory userDirectory,
        IdentityManagementService identityManagement,
        PermissionService permissionService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest(new AuthErrorResponse("Email and password are required."));
        }

        var user = await identityManagement.ValidateLocalCredentialsAsync(request, cancellationToken)
            ?? userDirectory.ValidateCredentials(request);

        if (user is null)
        {
            return Results.Json(new AuthErrorResponse("Invalid email or password."), statusCode: StatusCodes.Status401Unauthorized);
        }

        var principal = CreatePrincipal(user);
        var permissions = await permissionService.GetEffectivePermissionsAsync(principal, cancellationToken);

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IssuedUtc = DateTimeOffset.UtcNow,
                IsPersistent = false
            });

        return Results.Ok(new AuthSessionResponse((user with { Permissions = permissions }).ToResponse()));
    }

    private static async Task<IResult> GetCurrentUserAsync(
        ClaimsPrincipal principal,
        PermissionService permissionService,
        CancellationToken cancellationToken)
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
            principal.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray(),
            await permissionService.GetEffectivePermissionsAsync(principal, cancellationToken));

        return Results.Ok(new AuthSessionResponse(user));
    }

    private static async Task<IResult> SignOutAsync(HttpContext httpContext)
    {
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return Results.NoContent();
    }

    private static ClaimsPrincipal CreatePrincipal(AuthenticatedUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email)
        };

        claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }

    private static async Task<IResult> HandleIdentityRequestAsync(Func<Task<IResult>> action)
    {
        try
        {
            return await action();
        }
        catch (IdentityManagementException exception)
        {
            return Results.Json(new AuthErrorResponse(exception.Message), statusCode: exception.StatusCode);
        }
    }
}

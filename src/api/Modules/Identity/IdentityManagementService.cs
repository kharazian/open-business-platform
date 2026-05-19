using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;

namespace OpenBusinessPlatform.Api.Modules.Identity;

public sealed class IdentityManagementService
{
    private readonly OpenBusinessPlatformDbContext dbContext;
    private readonly LocalPasswordHasher passwordHasher;

    public IdentityManagementService(OpenBusinessPlatformDbContext dbContext, LocalPasswordHasher passwordHasher)
    {
        this.dbContext = dbContext;
        this.passwordHasher = passwordHasher;
    }

    public async Task<AuthenticatedUser?> ValidateLocalCredentialsAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);

        var user = await dbContext.Users
            .AsNoTracking()
            .Include(item => item.Roles)
            .ThenInclude(userRole => userRole.Role)
            .SingleOrDefaultAsync(item => item.Email == email, cancellationToken);

        if (user is null || !user.IsActive || !passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            return null;
        }

        return new AuthenticatedUser(
            user.Id.ToString(),
            user.Name,
            user.Email,
            user.Roles
                .Where(userRole => userRole.Role is { IsActive: true })
                .Select(userRole => userRole.Role!.Name)
                .OrderBy(role => role)
                .ToArray());
    }

    public async Task<IReadOnlyCollection<UserDto>> ListUsersAsync(CancellationToken cancellationToken)
    {
        var users = await UserQuery()
            .OrderBy(user => user.Name)
            .ToArrayAsync(cancellationToken);

        return users.Select(ToUserDto).ToArray();
    }

    public async Task<UserDto?> GetUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await UserQuery().SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);

        return user is null ? null : ToUserDto(user);
    }

    public async Task<UserDto> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        ValidateUserName(request.Name);
        ValidatePassword(request.Password);

        if (await dbContext.Users.AnyAsync(user => user.Email == email, cancellationToken))
        {
            throw new IdentityManagementException(StatusCodes.Status400BadRequest, "A user with this email already exists.");
        }

        var roleIds = DistinctIds(request.RoleIds);
        var departmentIds = DistinctIds(request.DepartmentIds);
        await EnsureRolesExistAsync(roleIds, cancellationToken);
        await EnsureDepartmentsExistAsync(departmentIds, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Email = email,
            IsActive = request.IsActive,
            PasswordHash = passwordHasher.HashPassword(request.Password),
            PasswordUpdatedAt = now
        };

        foreach (var roleId in roleIds)
        {
            user.Roles.Add(new UserRole { RoleId = roleId });
        }

        foreach (var departmentId in departmentIds)
        {
            user.Departments.Add(new UserDepartment { DepartmentId = departmentId, IsPrimary = false });
        }

        dbContext.Users.Add(user);
        AddAudit("User", user.Id, "user_created");
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetUserAsync(user.Id, cancellationToken))!;
    }

    public async Task<UserDto?> UpdateUserAsync(Guid userId, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        ValidateUserName(request.Name);
        var roleIds = DistinctIds(request.RoleIds);
        var departmentIds = DistinctIds(request.DepartmentIds);
        await EnsureRolesExistAsync(roleIds, cancellationToken);
        await EnsureDepartmentsExistAsync(departmentIds, cancellationToken);

        var user = await dbContext.Users
            .Include(item => item.Roles)
            .Include(item => item.Departments)
            .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);

        if (user is null)
        {
            return null;
        }

        EnsureConcurrencyStamp(user.ConcurrencyStamp, request.ConcurrencyStamp);
        user.Name = request.Name.Trim();
        user.IsActive = request.IsActive;

        ReplaceUserRoles(user, roleIds);
        ReplaceUserDepartments(user, departmentIds);
        AddAudit("User", user.Id, request.IsActive ? "user_updated" : "user_disabled");

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetUserAsync(user.Id, cancellationToken);
    }

    public async Task<bool> ResetPasswordAsync(Guid userId, ResetUserPasswordRequest request, CancellationToken cancellationToken)
    {
        ValidatePassword(request.NewPassword);

        var user = await dbContext.Users.SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);

        if (user is null)
        {
            return false;
        }

        user.PasswordHash = passwordHasher.HashPassword(request.NewPassword);
        user.PasswordUpdatedAt = DateTimeOffset.UtcNow;
        AddAudit("User", user.Id, "user_password_reset");

        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<IReadOnlyCollection<RoleDto>> ListRolesAsync(CancellationToken cancellationToken)
    {
        var roles = await RoleQuery()
            .OrderBy(role => role.Name)
            .ToArrayAsync(cancellationToken);

        return roles.Select(ToRoleDto).ToArray();
    }

    public async Task<RoleDto?> GetRoleAsync(Guid roleId, CancellationToken cancellationToken)
    {
        var role = await RoleQuery().SingleOrDefaultAsync(item => item.Id == roleId, cancellationToken);

        return role is null ? null : ToRoleDto(role);
    }

    public async Task<RoleDto> CreateRoleAsync(CreateRoleRequest request, CancellationToken cancellationToken)
    {
        ValidateRoleName(request.Name);

        if (await dbContext.Roles.AnyAsync(role => role.Name == request.Name.Trim(), cancellationToken))
        {
            throw new IdentityManagementException(StatusCodes.Status400BadRequest, "A role with this name already exists.");
        }

        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = NormalizeOptionalText(request.Description),
            IsActive = request.IsActive
        };

        dbContext.Roles.Add(role);
        AddAudit("Role", role.Id, "role_created");
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetRoleAsync(role.Id, cancellationToken))!;
    }

    public async Task<RoleDto?> UpdateRoleAsync(Guid roleId, UpdateRoleRequest request, CancellationToken cancellationToken)
    {
        ValidateRoleName(request.Name);

        var role = await dbContext.Roles.SingleOrDefaultAsync(item => item.Id == roleId, cancellationToken);

        if (role is null)
        {
            return null;
        }

        EnsureConcurrencyStamp(role.ConcurrencyStamp, request.ConcurrencyStamp);
        role.Name = request.Name.Trim();
        role.Description = NormalizeOptionalText(request.Description);
        role.IsActive = request.IsActive;
        AddAudit("Role", role.Id, request.IsActive ? "role_updated" : "role_disabled");

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetRoleAsync(role.Id, cancellationToken);
    }

    public async Task<RolePermissionsDto?> GetRolePermissionsAsync(Guid roleId, CancellationToken cancellationToken)
    {
        var role = await dbContext.Roles
            .AsNoTracking()
            .Include(item => item.Permissions)
            .Include(item => item.FormPermissions)
            .SingleOrDefaultAsync(item => item.Id == roleId, cancellationToken);

        return role is null
            ? null
            : ToRolePermissionsDto(role);
    }

    public async Task<RolePermissionsDto?> UpdateRolePermissionsAsync(
        Guid roleId,
        UpdateRolePermissionsRequest request,
        CancellationToken cancellationToken)
    {
        var permissions = NormalizePermissions(request.Permissions);
        var formPermissions = NormalizeFormPermissions(request.FormPermissions);
        await EnsureFormsExistAsync(formPermissions.Select(permission => permission.FormId).Distinct().ToArray(), cancellationToken);

        var role = await dbContext.Roles
            .Include(item => item.Permissions)
            .Include(item => item.FormPermissions)
            .SingleOrDefaultAsync(item => item.Id == roleId, cancellationToken);

        if (role is null)
        {
            return null;
        }

        role.Permissions.Clear();
        role.FormPermissions.Clear();

        foreach (var permission in permissions)
        {
            role.Permissions.Add(new RolePermission { Permission = permission });
        }

        foreach (var formPermission in formPermissions)
        {
            role.FormPermissions.Add(new RoleFormPermission
            {
                FormId = formPermission.FormId,
                Action = formPermission.Action
            });
        }

        AddAudit("Role", role.Id, "role_permissions_changed");
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetRolePermissionsAsync(role.Id, cancellationToken);
    }

    private IQueryable<User> UserQuery()
    {
        return dbContext.Users
            .AsNoTracking()
            .Include(user => user.Roles)
            .ThenInclude(userRole => userRole.Role)
            .Include(user => user.Departments)
            .ThenInclude(userDepartment => userDepartment.Department);
    }

    private IQueryable<Role> RoleQuery()
    {
        return dbContext.Roles
            .AsNoTracking()
            .Include(role => role.Users);
    }

    private async Task EnsureRolesExistAsync(IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken)
    {
        if (roleIds.Count == 0)
        {
            return;
        }

        var existingCount = await dbContext.Roles.CountAsync(role => roleIds.Contains(role.Id), cancellationToken);

        if (existingCount != roleIds.Count)
        {
            throw new IdentityManagementException(StatusCodes.Status400BadRequest, "One or more roles were not found.");
        }
    }

    private async Task EnsureDepartmentsExistAsync(IReadOnlyCollection<Guid> departmentIds, CancellationToken cancellationToken)
    {
        if (departmentIds.Count == 0)
        {
            return;
        }

        var existingCount = await dbContext.Departments.CountAsync(department => departmentIds.Contains(department.Id), cancellationToken);

        if (existingCount != departmentIds.Count)
        {
            throw new IdentityManagementException(StatusCodes.Status400BadRequest, "One or more departments were not found.");
        }
    }

    private async Task EnsureFormsExistAsync(IReadOnlyCollection<Guid> formIds, CancellationToken cancellationToken)
    {
        if (formIds.Count == 0)
        {
            return;
        }

        var existingCount = await dbContext.Forms.CountAsync(form => formIds.Contains(form.Id), cancellationToken);

        if (existingCount != formIds.Count)
        {
            throw new IdentityManagementException(StatusCodes.Status400BadRequest, "One or more forms were not found.");
        }
    }

    private static void ReplaceUserRoles(User user, IReadOnlyCollection<Guid> roleIds)
    {
        user.Roles.Clear();

        foreach (var roleId in roleIds)
        {
            user.Roles.Add(new UserRole { UserId = user.Id, RoleId = roleId });
        }
    }

    private static void ReplaceUserDepartments(User user, IReadOnlyCollection<Guid> departmentIds)
    {
        user.Departments.Clear();

        foreach (var departmentId in departmentIds)
        {
            user.Departments.Add(new UserDepartment { UserId = user.Id, DepartmentId = departmentId, IsPrimary = false });
        }
    }

    private static UserDto ToUserDto(User user)
    {
        return new UserDto(
            user.Id,
            user.Name,
            user.Email,
            user.IsActive,
            user.ExternalProvider,
            user.ExternalUserId,
            user.Roles
                .Where(userRole => userRole.Role is not null)
                .Select(userRole => new UserRoleDto(userRole.Role!.Id, userRole.Role.Name))
                .OrderBy(role => role.Name)
                .ToArray(),
            user.Departments
                .Where(userDepartment => userDepartment.Department is not null)
                .Select(userDepartment => new UserDepartmentDto(
                    userDepartment.Department!.Id,
                    userDepartment.Department.Name,
                    userDepartment.IsPrimary))
                .OrderBy(department => department.Name)
                .ToArray(),
            user.ConcurrencyStamp,
            user.CreatedAt,
            user.CreatedById,
            user.UpdatedAt,
            user.UpdatedById);
    }

    private static RoleDto ToRoleDto(Role role)
    {
        return new RoleDto(
            role.Id,
            role.Name,
            role.Description,
            role.IsActive,
            role.Users.Count,
            role.ConcurrencyStamp,
            role.CreatedAt,
            role.CreatedById,
            role.UpdatedAt,
            role.UpdatedById);
    }

    private static RolePermissionsDto ToRolePermissionsDto(Role role)
    {
        return new RolePermissionsDto(
            role.Id,
            role.Permissions.Select(permission => permission.Permission).OrderBy(permission => permission).ToArray(),
            role.FormPermissions
                .Select(permission => new RoleFormPermissionDto(permission.FormId, permission.Action))
                .OrderBy(permission => permission.FormId)
                .ThenBy(permission => permission.Action)
                .ToArray());
    }

    private static IReadOnlyCollection<string> NormalizePermissions(IReadOnlyCollection<string> permissions)
    {
        var normalized = permissions
            .Select(permission => permission.Trim())
            .Where(permission => permission.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(permission => permission)
            .ToArray();

        if (normalized.Any(permission => !PlatformPermissions.AllBuiltInPermissions.Contains(permission)))
        {
            throw new IdentityManagementException(StatusCodes.Status400BadRequest, "One or more permissions are not valid.");
        }

        return normalized;
    }

    private static IReadOnlyCollection<RoleFormPermissionDto> NormalizeFormPermissions(IReadOnlyCollection<RoleFormPermissionDto> formPermissions)
    {
        var normalized = formPermissions
            .Where(permission => permission.FormId != Guid.Empty)
            .Select(permission => new RoleFormPermissionDto(permission.FormId, permission.Action.Trim()))
            .Where(permission => permission.Action.Length > 0)
            .Distinct()
            .OrderBy(permission => permission.FormId)
            .ThenBy(permission => permission.Action)
            .ToArray();

        if (normalized.Any(permission => !PlatformPermissions.FormActions.Contains(permission.Action)))
        {
            throw new IdentityManagementException(StatusCodes.Status400BadRequest, "One or more form actions are not valid.");
        }

        return normalized;
    }

    private static IReadOnlyCollection<Guid> DistinctIds(IReadOnlyCollection<Guid> ids)
    {
        return ids.Where(id => id != Guid.Empty).Distinct().ToArray();
    }

    private static void ValidateUserName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new IdentityManagementException(StatusCodes.Status400BadRequest, "User name is required.");
        }
    }

    private static void ValidateRoleName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new IdentityManagementException(StatusCodes.Status400BadRequest, "Role name is required.");
        }
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            throw new IdentityManagementException(StatusCodes.Status400BadRequest, "Password must be at least 8 characters.");
        }
    }

    private static void EnsureConcurrencyStamp(string currentStamp, string requestedStamp)
    {
        if (!string.Equals(currentStamp, requestedStamp, StringComparison.Ordinal))
        {
            throw new IdentityManagementException(StatusCodes.Status409Conflict, "The record was changed by another user.");
        }
    }

    private static string NormalizeEmail(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new IdentityManagementException(StatusCodes.Status400BadRequest, "Email is required.");
        }

        return value.Trim().ToLowerInvariant();
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private void AddAudit(string entityType, Guid entityId, string action)
    {
        dbContext.AuditLogs.Add(new AuditLogEntry
        {
            EntityType = entityType,
            EntityId = entityId,
            Action = action
        });
    }
}

public sealed class IdentityManagementException : Exception
{
    public IdentityManagementException(int statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public int StatusCode { get; }
}

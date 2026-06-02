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

        var roleIds = DistinctIds(request.RoleIds ?? Array.Empty<Guid>());
        var departmentIds = DistinctIds(request.DepartmentIds ?? Array.Empty<Guid>());
        var groupIds = DistinctIds(request.GroupIds ?? Array.Empty<Guid>());
        await EnsureRolesExistAsync(roleIds, cancellationToken);
        await EnsureDepartmentsExistAsync(departmentIds, cancellationToken);
        await EnsureGroupsExistAsync(groupIds, cancellationToken);

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

        foreach (var groupId in groupIds)
        {
            user.Groups.Add(new UserGroup { GroupId = groupId });
        }

        dbContext.Users.Add(user);
        AddAudit("User", user.Id, "user_created");
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetUserAsync(user.Id, cancellationToken))!;
    }

    public async Task<UserDto?> UpdateUserAsync(Guid userId, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        ValidateUserName(request.Name);
        var roleIds = DistinctIds(request.RoleIds ?? Array.Empty<Guid>());
        var departmentIds = DistinctIds(request.DepartmentIds ?? Array.Empty<Guid>());
        var groupIds = DistinctIds(request.GroupIds ?? Array.Empty<Guid>());
        await EnsureRolesExistAsync(roleIds, cancellationToken);
        await EnsureDepartmentsExistAsync(departmentIds, cancellationToken);
        await EnsureGroupsExistAsync(groupIds, cancellationToken);

        var user = await dbContext.Users
            .Include(item => item.Roles)
            .Include(item => item.Departments)
            .Include(item => item.Groups)
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
        ReplaceUserGroups(user, groupIds);
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

    public async Task<IReadOnlyCollection<GroupDto>> ListGroupsAsync(CancellationToken cancellationToken)
    {
        var groups = await GroupQuery()
            .OrderBy(group => group.Name)
            .ToArrayAsync(cancellationToken);

        return groups.Select(ToGroupDto).ToArray();
    }

    public async Task<GroupDto?> GetGroupAsync(Guid groupId, CancellationToken cancellationToken)
    {
        var group = await GroupQuery().SingleOrDefaultAsync(item => item.Id == groupId, cancellationToken);

        return group is null ? null : ToGroupDto(group);
    }

    public async Task<GroupDto> CreateGroupAsync(CreateGroupRequest request, CancellationToken cancellationToken)
    {
        var name = ValidateGroupName(request.Name);

        if (await dbContext.Groups.AnyAsync(group => group.Name == name, cancellationToken))
        {
            throw new IdentityManagementException(StatusCodes.Status400BadRequest, "A group with this name already exists.");
        }

        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = NormalizeOptionalText(request.Description),
            IsActive = request.IsActive
        };

        dbContext.Groups.Add(group);
        AddAudit("Group", group.Id, "group_created");
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetGroupAsync(group.Id, cancellationToken))!;
    }

    public async Task<GroupDto?> UpdateGroupAsync(Guid groupId, UpdateGroupRequest request, CancellationToken cancellationToken)
    {
        var name = ValidateGroupName(request.Name);

        var group = await dbContext.Groups.SingleOrDefaultAsync(item => item.Id == groupId, cancellationToken);

        if (group is null)
        {
            return null;
        }

        if (await dbContext.Groups.AnyAsync(item => item.Id != groupId && item.Name == name, cancellationToken))
        {
            throw new IdentityManagementException(StatusCodes.Status400BadRequest, "A group with this name already exists.");
        }

        EnsureConcurrencyStamp(group.ConcurrencyStamp, request.ConcurrencyStamp);
        group.Name = name;
        group.Description = NormalizeOptionalText(request.Description);
        group.IsActive = request.IsActive;
        AddAudit("Group", group.Id, request.IsActive ? "group_updated" : "group_disabled");

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetGroupAsync(group.Id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<DepartmentDto>> ListDepartmentsAsync(CancellationToken cancellationToken)
    {
        var departments = await DepartmentQuery()
            .OrderBy(department => department.Name)
            .ToArrayAsync(cancellationToken);

        return departments.Select(ToDepartmentDto).ToArray();
    }

    public async Task<DepartmentDto?> GetDepartmentAsync(Guid departmentId, CancellationToken cancellationToken)
    {
        var department = await DepartmentQuery().SingleOrDefaultAsync(item => item.Id == departmentId, cancellationToken);

        return department is null ? null : ToDepartmentDto(department);
    }

    public async Task<DepartmentDto> CreateDepartmentAsync(CreateDepartmentRequest request, CancellationToken cancellationToken)
    {
        var name = ValidateDepartmentName(request.Name);
        await EnsureDepartmentReferencesAsync(null, request.ParentDepartmentId, request.ManagerUserId, cancellationToken);

        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = name,
            ParentDepartmentId = NormalizeNullableId(request.ParentDepartmentId),
            ManagerUserId = NormalizeNullableId(request.ManagerUserId),
            IsActive = request.IsActive
        };

        dbContext.Departments.Add(department);
        AddAudit("Department", department.Id, "department_created");
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetDepartmentAsync(department.Id, cancellationToken))!;
    }

    public async Task<DepartmentDto?> UpdateDepartmentAsync(
        Guid departmentId,
        UpdateDepartmentRequest request,
        CancellationToken cancellationToken)
    {
        var name = ValidateDepartmentName(request.Name);
        await EnsureDepartmentReferencesAsync(departmentId, request.ParentDepartmentId, request.ManagerUserId, cancellationToken);

        var department = await dbContext.Departments.SingleOrDefaultAsync(item => item.Id == departmentId, cancellationToken);

        if (department is null)
        {
            return null;
        }

        EnsureConcurrencyStamp(department.ConcurrencyStamp, request.ConcurrencyStamp);
        department.Name = name;
        department.ParentDepartmentId = NormalizeNullableId(request.ParentDepartmentId);
        department.ManagerUserId = NormalizeNullableId(request.ManagerUserId);
        department.IsActive = request.IsActive;
        AddAudit("Department", department.Id, request.IsActive ? "department_updated" : "department_disabled");

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetDepartmentAsync(department.Id, cancellationToken);
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
            .Include(item => item.ReportPermissions)
            .Include(item => item.FieldPermissions)
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
        var reportPermissions = NormalizeReportPermissions(request.ReportPermissions);
        var fieldPermissions = NormalizeFieldPermissions(request.FieldPermissions);
        await EnsureFormsExistAsync(formPermissions.Select(permission => permission.FormId).Distinct().ToArray(), cancellationToken);
        await EnsureFormsExistAsync(fieldPermissions.Select(permission => permission.FormId).Distinct().ToArray(), cancellationToken);
        await EnsureReportsExistAsync(reportPermissions.Select(permission => permission.ReportId).Distinct().ToArray(), cancellationToken);

        var role = await dbContext.Roles
            .Include(item => item.Permissions)
            .Include(item => item.FormPermissions)
            .Include(item => item.ReportPermissions)
            .Include(item => item.FieldPermissions)
            .SingleOrDefaultAsync(item => item.Id == roleId, cancellationToken);

        if (role is null)
        {
            return null;
        }

        role.Permissions.Clear();
        role.FormPermissions.Clear();
        role.ReportPermissions.Clear();
        role.FieldPermissions.Clear();

        foreach (var permission in permissions)
        {
            role.Permissions.Add(new RolePermission { Permission = permission });
        }

        foreach (var formPermission in formPermissions)
        {
            role.FormPermissions.Add(new RoleFormPermission
            {
                FormId = formPermission.FormId,
                Action = formPermission.Action,
                Scope = formPermission.Scope
            });
        }

        foreach (var reportPermission in reportPermissions)
        {
            role.ReportPermissions.Add(new RoleReportPermission
            {
                ReportId = reportPermission.ReportId,
                Action = reportPermission.Action
            });
        }

        foreach (var fieldPermission in fieldPermissions)
        {
            role.FieldPermissions.Add(new RoleFieldPermission
            {
                FormId = fieldPermission.FormId,
                FieldId = fieldPermission.FieldId,
                Access = fieldPermission.Access
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
            .ThenInclude(userDepartment => userDepartment.Department)
            .Include(user => user.Groups)
            .ThenInclude(userGroup => userGroup.Group);
    }

    private IQueryable<Role> RoleQuery()
    {
        return dbContext.Roles
            .AsNoTracking()
            .Include(role => role.Users);
    }

    private IQueryable<Group> GroupQuery()
    {
        return dbContext.Groups
            .AsNoTracking()
            .Include(group => group.Users);
    }

    private IQueryable<Department> DepartmentQuery()
    {
        return dbContext.Departments
            .AsNoTracking()
            .Include(department => department.Users);
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

    private async Task EnsureGroupsExistAsync(IReadOnlyCollection<Guid> groupIds, CancellationToken cancellationToken)
    {
        if (groupIds.Count == 0)
        {
            return;
        }

        var existingCount = await dbContext.Groups.CountAsync(group => groupIds.Contains(group.Id), cancellationToken);

        if (existingCount != groupIds.Count)
        {
            throw new IdentityManagementException(StatusCodes.Status400BadRequest, "One or more groups were not found.");
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

    private async Task EnsureReportsExistAsync(IReadOnlyCollection<Guid> reportIds, CancellationToken cancellationToken)
    {
        if (reportIds.Count == 0)
        {
            return;
        }

        var existingCount = await dbContext.Reports.CountAsync(report => reportIds.Contains(report.Id), cancellationToken);

        if (existingCount != reportIds.Count)
        {
            throw new IdentityManagementException(StatusCodes.Status400BadRequest, "One or more reports were not found.");
        }
    }

    private async Task EnsureDepartmentReferencesAsync(
        Guid? departmentId,
        Guid? parentDepartmentId,
        Guid? managerUserId,
        CancellationToken cancellationToken)
    {
        var normalizedParentDepartmentId = NormalizeNullableId(parentDepartmentId);
        var normalizedManagerUserId = NormalizeNullableId(managerUserId);

        if (departmentId is not null && normalizedParentDepartmentId == departmentId)
        {
            throw new IdentityManagementException(StatusCodes.Status400BadRequest, "A department cannot be its own parent.");
        }

        if (normalizedParentDepartmentId is not null
            && !await dbContext.Departments.AnyAsync(department => department.Id == normalizedParentDepartmentId.Value, cancellationToken))
        {
            throw new IdentityManagementException(StatusCodes.Status400BadRequest, "Parent department was not found.");
        }

        if (normalizedManagerUserId is not null
            && !await dbContext.Users.AnyAsync(user => user.Id == normalizedManagerUserId.Value, cancellationToken))
        {
            throw new IdentityManagementException(StatusCodes.Status400BadRequest, "Manager user was not found.");
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

    private static void ReplaceUserGroups(User user, IReadOnlyCollection<Guid> groupIds)
    {
        user.Groups.Clear();

        foreach (var groupId in groupIds)
        {
            user.Groups.Add(new UserGroup { UserId = user.Id, GroupId = groupId });
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
            user.Groups
                .Where(userGroup => userGroup.Group is not null)
                .Select(userGroup => new UserGroupDto(userGroup.Group!.Id, userGroup.Group.Name))
                .OrderBy(group => group.Name)
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

    private static GroupDto ToGroupDto(Group group)
    {
        return new GroupDto(
            group.Id,
            group.Name,
            group.Description,
            group.IsActive,
            group.Users.Count,
            group.ConcurrencyStamp,
            group.CreatedAt,
            group.CreatedById,
            group.UpdatedAt,
            group.UpdatedById);
    }

    private static DepartmentDto ToDepartmentDto(Department department)
    {
        return new DepartmentDto(
            department.Id,
            department.Name,
            department.ParentDepartmentId,
            department.ManagerUserId,
            department.IsActive,
            department.Users.Count,
            department.ConcurrencyStamp,
            department.CreatedAt,
            department.CreatedById,
            department.UpdatedAt,
            department.UpdatedById);
    }

    private static RolePermissionsDto ToRolePermissionsDto(Role role)
    {
        return new RolePermissionsDto(
            role.Id,
            role.Permissions.Select(permission => permission.Permission).OrderBy(permission => permission).ToArray(),
            role.FormPermissions
                .Select(permission => new RoleFormPermissionDto(permission.FormId, permission.Action, permission.Scope))
                .OrderBy(permission => permission.FormId)
                .ThenBy(permission => permission.Action)
                .ThenBy(permission => permission.Scope)
                .ToArray(),
            role.ReportPermissions
                .Select(permission => new RoleReportPermissionDto(permission.ReportId, permission.Action))
                .OrderBy(permission => permission.ReportId)
                .ThenBy(permission => permission.Action)
                .ToArray(),
            role.FieldPermissions
                .Select(permission => new RoleFieldPermissionDto(permission.FormId, permission.FieldId, permission.Access))
                .OrderBy(permission => permission.FormId)
                .ThenBy(permission => permission.FieldId)
                .ToArray());
    }

    private static IReadOnlyCollection<string> NormalizePermissions(IReadOnlyCollection<string>? permissions)
    {
        var normalized = (permissions ?? Array.Empty<string>())
            .Select(permission => (permission ?? string.Empty).Trim())
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

    private static IReadOnlyCollection<RoleFormPermissionDto> NormalizeFormPermissions(IReadOnlyCollection<RoleFormPermissionDto>? formPermissions)
    {
        var candidates = (formPermissions ?? Array.Empty<RoleFormPermissionDto>())
            .Where(permission => permission.FormId != Guid.Empty)
            .Select(permission => new RoleFormPermissionDto(
                permission.FormId,
                (permission.Action ?? string.Empty).Trim(),
                string.IsNullOrWhiteSpace(permission.Scope) ? PlatformPermissions.RecordScopes.All : permission.Scope.Trim()))
            .Where(permission => permission.Action.Length > 0)
            .ToArray();

        if (candidates.Any(permission =>
            !PlatformPermissions.FormActions.Contains(permission.Action)
            || !PlatformPermissions.RecordScopes.Supported.Contains(permission.Scope)))
        {
            throw new IdentityManagementException(StatusCodes.Status400BadRequest, "One or more form actions are not valid.");
        }

        var normalized = candidates
            .GroupBy(permission => new { permission.FormId, permission.Action })
            .Select(group => new RoleFormPermissionDto(
                group.Key.FormId,
                group.Key.Action,
                ChooseBroadestScope(group.Select(permission => permission.Scope))))
            .OrderBy(permission => permission.FormId)
            .ThenBy(permission => permission.Action)
            .ToArray();

        return normalized;
    }

    private static IReadOnlyCollection<RoleReportPermissionDto> NormalizeReportPermissions(IReadOnlyCollection<RoleReportPermissionDto>? reportPermissions)
    {
        var normalized = (reportPermissions ?? Array.Empty<RoleReportPermissionDto>())
            .Where(permission => permission.ReportId != Guid.Empty)
            .Select(permission => new RoleReportPermissionDto(permission.ReportId, (permission.Action ?? string.Empty).Trim()))
            .Where(permission => permission.Action.Length > 0)
            .Distinct()
            .OrderBy(permission => permission.ReportId)
            .ThenBy(permission => permission.Action)
            .ToArray();

        if (normalized.Any(permission => !PlatformPermissions.ReportActions.Contains(permission.Action)))
        {
            throw new IdentityManagementException(StatusCodes.Status400BadRequest, "One or more report actions are not valid.");
        }

        return normalized;
    }

    private static IReadOnlyCollection<RoleFieldPermissionDto> NormalizeFieldPermissions(IReadOnlyCollection<RoleFieldPermissionDto>? fieldPermissions)
    {
        var candidates = (fieldPermissions ?? Array.Empty<RoleFieldPermissionDto>())
            .Where(permission => permission.FormId != Guid.Empty)
            .Select(permission => new RoleFieldPermissionDto(
                permission.FormId,
                (permission.FieldId ?? string.Empty).Trim(),
                (permission.Access ?? string.Empty).Trim()))
            .Where(permission => permission.FieldId.Length > 0 && permission.Access.Length > 0)
            .ToArray();

        if (candidates.Any(permission => !PlatformPermissions.FieldAccess.Supported.Contains(permission.Access)))
        {
            throw new IdentityManagementException(StatusCodes.Status400BadRequest, "One or more field access rules are not valid.");
        }

        var normalized = candidates
            .GroupBy(permission => new { permission.FormId, permission.FieldId })
            .Select(group => new RoleFieldPermissionDto(
                group.Key.FormId,
                group.Key.FieldId,
                group.Any(permission => permission.Access == PlatformPermissions.FieldAccess.Hidden)
                    ? PlatformPermissions.FieldAccess.Hidden
                    : PlatformPermissions.FieldAccess.ReadOnly))
            .OrderBy(permission => permission.FormId)
            .ThenBy(permission => permission.FieldId)
            .ToArray();

        return normalized;
    }

    private static IReadOnlyCollection<Guid> DistinctIds(IReadOnlyCollection<Guid> ids)
    {
        return ids.Where(id => id != Guid.Empty).Distinct().ToArray();
    }

    private static string ChooseBroadestScope(IEnumerable<string> scopes)
    {
        return scopes
            .OrderBy(scope => scope switch
            {
                PlatformPermissions.RecordScopes.All => 0,
                PlatformPermissions.RecordScopes.ManagedDepartment => 1,
                PlatformPermissions.RecordScopes.Department => 2,
                PlatformPermissions.RecordScopes.Group => 3,
                PlatformPermissions.RecordScopes.Assigned => 4,
                PlatformPermissions.RecordScopes.Own => 5,
                _ => 6
            })
            .First();
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

    private static string ValidateGroupName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new IdentityManagementException(StatusCodes.Status400BadRequest, "Group name is required.");
        }

        return name.Trim();
    }

    private static string ValidateDepartmentName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new IdentityManagementException(StatusCodes.Status400BadRequest, "Department name is required.");
        }

        return name.Trim();
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

    private static Guid? NormalizeNullableId(Guid? value)
    {
        return value is null || value == Guid.Empty ? null : value;
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

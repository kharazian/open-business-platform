namespace OpenBusinessPlatform.Api.Modules.Identity;

public sealed record UserRoleDto(Guid Id, string Name);

public sealed record UserDepartmentDto(Guid Id, string Name, bool IsPrimary);

public sealed record UserDto(
    Guid Id,
    string Name,
    string Email,
    bool IsActive,
    string? ExternalProvider,
    string? ExternalUserId,
    IReadOnlyCollection<UserRoleDto> Roles,
    IReadOnlyCollection<UserDepartmentDto> Departments,
    string ConcurrencyStamp,
    DateTimeOffset CreatedAt,
    Guid? CreatedById,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedById);

public sealed record CreateUserRequest(
    string Name,
    string Email,
    string Password,
    IReadOnlyCollection<Guid> RoleIds,
    IReadOnlyCollection<Guid> DepartmentIds,
    bool IsActive = true);

public sealed record UpdateUserRequest(
    string Name,
    bool IsActive,
    IReadOnlyCollection<Guid> RoleIds,
    IReadOnlyCollection<Guid> DepartmentIds,
    string ConcurrencyStamp);

public sealed record ResetUserPasswordRequest(string NewPassword);

public sealed record RoleDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    int UserCount,
    string ConcurrencyStamp,
    DateTimeOffset CreatedAt,
    Guid? CreatedById,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedById);

public sealed record CreateRoleRequest(
    string Name,
    string? Description,
    bool IsActive = true);

public sealed record UpdateRoleRequest(
    string Name,
    string? Description,
    bool IsActive,
    string ConcurrencyStamp);

public sealed record RoleFormPermissionDto(Guid FormId, string Action);

public sealed record RolePermissionsDto(
    Guid RoleId,
    IReadOnlyCollection<string> Permissions,
    IReadOnlyCollection<RoleFormPermissionDto> FormPermissions);

public sealed record UpdateRolePermissionsRequest(
    IReadOnlyCollection<string> Permissions,
    IReadOnlyCollection<RoleFormPermissionDto> FormPermissions);

public sealed record DepartmentDto(
    Guid Id,
    string Name,
    Guid? ParentDepartmentId,
    Guid? ManagerUserId,
    bool IsActive,
    int UserCount,
    string ConcurrencyStamp,
    DateTimeOffset CreatedAt,
    Guid? CreatedById,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedById);

public sealed record CreateDepartmentRequest(
    string Name,
    Guid? ParentDepartmentId,
    Guid? ManagerUserId,
    bool IsActive = true);

public sealed record UpdateDepartmentRequest(
    string Name,
    Guid? ParentDepartmentId,
    Guid? ManagerUserId,
    bool IsActive,
    string ConcurrencyStamp);

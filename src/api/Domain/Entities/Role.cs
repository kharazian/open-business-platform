using System.Text.Json;
using OpenBusinessPlatform.Api.Domain.Common;

namespace OpenBusinessPlatform.Api.Domain.Entities;

public sealed class Role : AuditedAggregateRoot<Guid>, IHasConcurrencyStamp, IHasExtraProperties, IIsActive
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString("N");

    public JsonDocument? ExtraPropertiesJson { get; set; }

    public ICollection<UserRole> Users { get; } = new List<UserRole>();

    public ICollection<RolePermission> Permissions { get; } = new List<RolePermission>();

    public ICollection<RoleFormPermission> FormPermissions { get; } = new List<RoleFormPermission>();

    public ICollection<RoleReportPermission> ReportPermissions { get; } = new List<RoleReportPermission>();

    public ICollection<RoleFieldPermission> FieldPermissions { get; } = new List<RoleFieldPermission>();
}

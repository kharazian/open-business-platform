using System.Text.Json;
using OpenBusinessPlatform.Api.Domain.Common;

namespace OpenBusinessPlatform.Api.Domain.Entities;

public sealed class User : AuditedAggregateRoot<Guid>, IHasConcurrencyStamp, IHasExtraProperties, IIsActive
{
    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public string? ExternalProvider { get; set; }

    public string? ExternalUserId { get; set; }

    public string? PasswordHash { get; set; }

    public DateTimeOffset? PasswordUpdatedAt { get; set; }

    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString("N");

    public JsonDocument? ExtraPropertiesJson { get; set; }

    public ICollection<UserRole> Roles { get; } = new List<UserRole>();

    public ICollection<UserDepartment> Departments { get; } = new List<UserDepartment>();

    public ICollection<UserGroup> Groups { get; } = new List<UserGroup>();
}

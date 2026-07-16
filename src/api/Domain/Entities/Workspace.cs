using System.Text.Json;
using OpenBusinessPlatform.Api.Domain.Common;

namespace OpenBusinessPlatform.Api.Domain.Entities;

public sealed class Workspace : AuditedAggregateRoot<Guid>, IHasConcurrencyStamp, IHasExtraProperties, IIsActive
{
    public Guid TenantId { get; set; }

    public Tenant? Tenant { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public bool IsDefault { get; set; }

    public bool IsActive { get; set; } = true;

    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString("N");

    public JsonDocument? ExtraPropertiesJson { get; set; }

    public ICollection<WorkspaceMembership> Memberships { get; } = new List<WorkspaceMembership>();
}

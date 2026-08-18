using System.Text.Json;
using OpenBusinessPlatform.Api.Domain.Common;

namespace OpenBusinessPlatform.Api.Domain.Entities;

public sealed class DashboardDefinition : WorkspaceFullAuditedAggregateRoot<Guid>, IHasConcurrencyStamp, IHasExtraProperties
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Status { get; set; } = "draft";

    public string? Slug { get; set; }

    public bool ShowInNavigation { get; set; }

    public string? MenuLabel { get; set; }

    public string? MenuIcon { get; set; }

    public int MenuOrder { get; set; }

    public string? ViewPermission { get; set; }

    public DateTimeOffset? PublishedAt { get; set; }

    public Guid? PublishedById { get; set; }

    public JsonDocument ConfigJson { get; set; } = null!;

    public JsonDocument LayoutJson { get; set; } = null!;

    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString("N");

    public JsonDocument? ExtraPropertiesJson { get; set; }
}

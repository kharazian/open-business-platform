using System.Text.Json;
using OpenBusinessPlatform.Api.Domain.Common;

namespace OpenBusinessPlatform.Api.Domain.Entities;

public sealed class WorkspaceLocalization : WorkspaceAuditedAggregateRoot<Guid>, IHasConcurrencyStamp, IHasExtraProperties
{
    public string DefaultLocale { get; set; } = string.Empty;
    public string DefaultTimeZone { get; set; } = string.Empty;
    public int FirstDayOfWeek { get; set; }
    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString("N");
    public JsonDocument? ExtraPropertiesJson { get; set; }
}

public sealed class UserLocalizationPreference : WorkspaceAuditedAggregateRoot<Guid>, IHasConcurrencyStamp, IHasExtraProperties
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string? Locale { get; set; }
    public string? TimeZone { get; set; }
    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString("N");
    public JsonDocument? ExtraPropertiesJson { get; set; }
}

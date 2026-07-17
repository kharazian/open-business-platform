using System.Text.Json;
using OpenBusinessPlatform.Api.Domain.Common;

namespace OpenBusinessPlatform.Api.Domain.Entities;

public sealed class WorkspaceCustomDomain : WorkspaceAuditedAggregateRoot<Guid>, IHasConcurrencyStamp, IHasExtraProperties
{
    public string Hostname { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string VerificationToken { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }
    public DateTimeOffset? LastCheckedAt { get; set; }
    public string? LastFailure { get; set; }
    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString("N");
    public JsonDocument? ExtraPropertiesJson { get; set; }
}

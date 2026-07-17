using OpenBusinessPlatform.Api.Domain.Common;
using System.Text.Json;

namespace OpenBusinessPlatform.Api.Domain.Entities;

public sealed class WorkspaceBranding : WorkspaceAuditedAggregateRoot<Guid>, IHasConcurrencyStamp, IHasExtraProperties
{
    public string AppName { get; set; } = string.Empty;
    public string LogoText { get; set; } = string.Empty;
    public string? LogoDataUrl { get; set; }
    public string PrimaryColor { get; set; } = string.Empty;
    public string? LoginMessage { get; set; }
    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString("N");
    public JsonDocument? ExtraPropertiesJson { get; set; }
}

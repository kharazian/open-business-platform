using OpenBusinessPlatform.Api.Domain.Common;

namespace OpenBusinessPlatform.Api.Domain.Entities;

public sealed class ExternalIdentity : WorkspaceCreationAuditedEntity<Guid>
{
    public Guid ProviderId { get; set; }
    public SsoProvider? Provider { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string EmailAtLink { get; set; } = string.Empty;
    public DateTimeOffset LastSignedInAt { get; set; }
}

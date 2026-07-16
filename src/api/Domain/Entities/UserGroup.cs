using OpenBusinessPlatform.Api.Domain.Common;

namespace OpenBusinessPlatform.Api.Domain.Entities;

public sealed class UserGroup : WorkspaceEntity<Guid>
{
    public Guid UserId { get; set; }

    public User? User { get; set; }

    public Guid GroupId { get; set; }

    public Group? Group { get; set; }
}

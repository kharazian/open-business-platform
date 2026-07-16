namespace OpenBusinessPlatform.Api.Domain.Entities;

public sealed class UserRole : OpenBusinessPlatform.Api.Domain.Common.IWorkspaceOwned
{
    public Guid WorkspaceId { get; set; }

    public Guid UserId { get; set; }

    public User? User { get; set; }

    public Guid RoleId { get; set; }

    public Role? Role { get; set; }
}

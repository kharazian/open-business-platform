namespace OpenBusinessPlatform.Api.Domain.Entities;

public sealed class UserDepartment : OpenBusinessPlatform.Api.Domain.Common.IWorkspaceOwned
{
    public Guid WorkspaceId { get; set; }

    public Guid UserId { get; set; }

    public User? User { get; set; }

    public Guid DepartmentId { get; set; }

    public Department? Department { get; set; }

    public bool IsPrimary { get; set; }
}

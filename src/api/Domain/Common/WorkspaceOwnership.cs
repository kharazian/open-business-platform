namespace OpenBusinessPlatform.Api.Domain.Common;

public interface IWorkspaceOwned
{
    Guid WorkspaceId { get; set; }
}

public abstract class WorkspaceEntity<TKey> : Entity<TKey>, IWorkspaceOwned
{
    public Guid WorkspaceId { get; set; }
}

public abstract class WorkspaceCreationAuditedEntity<TKey> : CreationAuditedEntity<TKey>, IWorkspaceOwned
{
    public Guid WorkspaceId { get; set; }
}

public abstract class WorkspaceAuditedEntity<TKey> : AuditedEntity<TKey>, IWorkspaceOwned
{
    public Guid WorkspaceId { get; set; }
}

public abstract class WorkspaceFullAuditedEntity<TKey> : FullAuditedEntity<TKey>, IWorkspaceOwned
{
    public Guid WorkspaceId { get; set; }
}

public abstract class WorkspaceCreationAuditedAggregateRoot<TKey> : CreationAuditedAggregateRoot<TKey>, IWorkspaceOwned
{
    public Guid WorkspaceId { get; set; }
}

public abstract class WorkspaceAuditedAggregateRoot<TKey> : AuditedAggregateRoot<TKey>, IWorkspaceOwned
{
    public Guid WorkspaceId { get; set; }
}

public abstract class WorkspaceFullAuditedAggregateRoot<TKey> : FullAuditedAggregateRoot<TKey>, IWorkspaceOwned
{
    public Guid WorkspaceId { get; set; }
}

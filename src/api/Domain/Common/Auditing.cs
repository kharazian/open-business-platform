namespace OpenBusinessPlatform.Api.Domain.Common;

public interface IHasCreationTime
{
    DateTimeOffset CreatedAt { get; set; }
}

public interface ICreationAudited : IHasCreationTime
{
    Guid? CreatedById { get; set; }
}

public interface IModificationAudited
{
    DateTimeOffset? UpdatedAt { get; set; }

    Guid? UpdatedById { get; set; }
}

public interface ISoftDelete
{
    bool IsDeleted { get; set; }
}

public interface IDeletionAudited : ISoftDelete
{
    DateTimeOffset? DeletedAt { get; set; }

    Guid? DeletedById { get; set; }
}

public interface IFullAudited : ICreationAudited, IModificationAudited, IDeletionAudited
{
}

public abstract class CreationAuditedEntity<TKey> : Entity<TKey>, ICreationAudited
{
    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedById { get; set; }
}

public abstract class AuditedEntity<TKey> : CreationAuditedEntity<TKey>, IModificationAudited
{
    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedById { get; set; }
}

public abstract class FullAuditedEntity<TKey> : AuditedEntity<TKey>, IFullAudited
{
    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public Guid? DeletedById { get; set; }
}

public abstract class CreationAuditedAggregateRoot<TKey> : AggregateRoot<TKey>, ICreationAudited
{
    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedById { get; set; }
}

public abstract class AuditedAggregateRoot<TKey> : CreationAuditedAggregateRoot<TKey>, IModificationAudited
{
    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedById { get; set; }
}

public abstract class FullAuditedAggregateRoot<TKey> : AuditedAggregateRoot<TKey>, IFullAudited
{
    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public Guid? DeletedById { get; set; }
}

namespace OpenBusinessPlatform.Api.Domain.Common;

public interface IEntity<TKey>
{
    TKey Id { get; set; }
}

public interface IAggregateRoot<TKey> : IEntity<TKey>
{
}

public abstract class Entity<TKey> : IEntity<TKey>
{
    public TKey Id { get; set; } = default!;
}

public abstract class AggregateRoot<TKey> : Entity<TKey>, IAggregateRoot<TKey>
{
}

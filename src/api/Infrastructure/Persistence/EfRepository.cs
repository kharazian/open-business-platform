using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Application.Common;
using OpenBusinessPlatform.Api.Domain.Common;

namespace OpenBusinessPlatform.Api.Infrastructure.Persistence;

public sealed class EfRepository<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
    private readonly OpenBusinessPlatformDbContext dbContext;

    public EfRepository(OpenBusinessPlatformDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public IQueryable<TEntity> Query()
    {
        return dbContext.Set<TEntity>();
    }

    public async Task<TEntity?> FindAsync(TKey id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<TEntity>().FindAsync(new object?[] { id }, cancellationToken);
    }

    public Task<List<TEntity>> ListAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        var query = Query();
        if (predicate is not null)
        {
            query = query.Where(predicate);
        }

        return query.ToListAsync(cancellationToken);
    }

    public Task<long> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        var query = Query();
        if (predicate is not null)
        {
            query = query.Where(predicate);
        }

        return query.LongCountAsync(cancellationToken);
    }

    public async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await dbContext.Set<TEntity>().AddAsync(entity, cancellationToken);
        return entity;
    }

    public TEntity Update(TEntity entity)
    {
        return dbContext.Set<TEntity>().Update(entity).Entity;
    }

    public void Remove(TEntity entity)
    {
        dbContext.Set<TEntity>().Remove(entity);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}

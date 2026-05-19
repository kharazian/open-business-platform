using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Domain.Common;

namespace OpenBusinessPlatform.Api.Application.Common;

public abstract class ReadOnlyCrudServiceBase<TEntity, TKey, TDto> : ApplicationServiceBase
    where TEntity : class, IEntity<TKey>
{
    private readonly IReadOnlyRepository<TEntity, TKey> repository;

    protected ReadOnlyCrudServiceBase(IReadOnlyRepository<TEntity, TKey> repository)
    {
        this.repository = repository;
    }

    protected IReadOnlyRepository<TEntity, TKey> Repository => repository;

    public virtual async Task<TDto?> GetAsync(TKey id, CancellationToken cancellationToken = default)
    {
        await CheckGetPermissionAsync(cancellationToken);

        var entity = await repository.FindAsync(id, cancellationToken);
        return entity is null ? default : MapToDto(entity);
    }

    public virtual async Task<PagedResultDto<TDto>> ListAsync(PagedRequestDto request, CancellationToken cancellationToken = default)
    {
        await CheckListPermissionAsync(cancellationToken);

        var query = ApplySorting(CreateFilteredQuery(repository.Query(), request), request);
        var totalCount = await query.LongCountAsync(cancellationToken);
        var items = await query
            .Skip(request.SkipCount)
            .Take(request.MaxResultCount)
            .ToListAsync(cancellationToken);

        return new PagedResultDto<TDto>(totalCount, items.Select(MapToDto));
    }

    protected virtual IQueryable<TEntity> CreateFilteredQuery(IQueryable<TEntity> query, PagedRequestDto request)
    {
        return query;
    }

    protected virtual IQueryable<TEntity> ApplySorting(IQueryable<TEntity> query, PagedRequestDto request)
    {
        return query;
    }

    protected abstract TDto MapToDto(TEntity entity);
}

public abstract class CrudServiceBase<TEntity, TKey, TDto, TCreateDto, TUpdateDto>
    : ReadOnlyCrudServiceBase<TEntity, TKey, TDto>
    where TEntity : class, IEntity<TKey>
{
    private readonly IRepository<TEntity, TKey> repository;

    protected CrudServiceBase(IRepository<TEntity, TKey> repository)
        : base(repository)
    {
        this.repository = repository;
    }

    public virtual async Task<TDto> CreateAsync(TCreateDto input, CancellationToken cancellationToken = default)
    {
        await CheckCreatePermissionAsync(cancellationToken);

        var entity = MapCreateToEntity(input);
        await BeforeCreateAsync(entity, input, cancellationToken);
        await repository.AddAsync(entity, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        await AfterCreateAsync(entity, input, cancellationToken);

        return MapToDto(entity);
    }

    public virtual async Task<TDto?> UpdateAsync(TKey id, TUpdateDto input, CancellationToken cancellationToken = default)
    {
        await CheckUpdatePermissionAsync(cancellationToken);

        var entity = await repository.FindAsync(id, cancellationToken);
        if (entity is null)
        {
            return default;
        }

        MapUpdateToEntity(input, entity);
        await BeforeUpdateAsync(entity, input, cancellationToken);
        repository.Update(entity);
        await repository.SaveChangesAsync(cancellationToken);
        await AfterUpdateAsync(entity, input, cancellationToken);

        return MapToDto(entity);
    }

    public virtual async Task<bool> DeleteAsync(TKey id, CancellationToken cancellationToken = default)
    {
        await CheckDeletePermissionAsync(cancellationToken);

        var entity = await repository.FindAsync(id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        await BeforeDeleteAsync(entity, cancellationToken);
        repository.Remove(entity);
        await repository.SaveChangesAsync(cancellationToken);
        await AfterDeleteAsync(entity, cancellationToken);

        return true;
    }

    protected abstract TEntity MapCreateToEntity(TCreateDto input);

    protected abstract void MapUpdateToEntity(TUpdateDto input, TEntity entity);

    protected virtual Task BeforeCreateAsync(TEntity entity, TCreateDto input, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected virtual Task AfterCreateAsync(TEntity entity, TCreateDto input, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected virtual Task BeforeUpdateAsync(TEntity entity, TUpdateDto input, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected virtual Task AfterUpdateAsync(TEntity entity, TUpdateDto input, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected virtual Task BeforeDeleteAsync(TEntity entity, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected virtual Task AfterDeleteAsync(TEntity entity, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

namespace OpenBusinessPlatform.Api.Application.Common;

public record EntityDto<TKey>(TKey Id);

public record CreationAuditedEntityDto<TKey>(
    TKey Id,
    DateTimeOffset CreatedAt,
    Guid? CreatedById) : EntityDto<TKey>(Id);

public record AuditedEntityDto<TKey>(
    TKey Id,
    DateTimeOffset CreatedAt,
    Guid? CreatedById,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedById) : CreationAuditedEntityDto<TKey>(Id, CreatedAt, CreatedById);

public record FullAuditedEntityDto<TKey>(
    TKey Id,
    DateTimeOffset CreatedAt,
    Guid? CreatedById,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedById,
    bool IsDeleted,
    DateTimeOffset? DeletedAt,
    Guid? DeletedById) : AuditedEntityDto<TKey>(Id, CreatedAt, CreatedById, UpdatedAt, UpdatedById);

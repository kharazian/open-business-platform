export type EntityId = string;

export interface EntityDto {
  id: EntityId;
}

export interface CreationAuditedEntityDto extends EntityDto {
  createdAt: string;
  createdById?: EntityId | null;
}

export interface AuditedEntityDto extends CreationAuditedEntityDto {
  updatedAt?: string | null;
  updatedById?: EntityId | null;
}

export interface FullAuditedEntityDto extends AuditedEntityDto {
  isDeleted: boolean;
  deletedAt?: string | null;
  deletedById?: EntityId | null;
}

export interface ConcurrencyStampedDto {
  concurrencyStamp: string;
}

export interface ActiveStateDto {
  isActive: boolean;
}

export type ExtraProperties = Record<string, unknown>;

export interface ExtraPropertiesDto {
  extraPropertiesJson?: ExtraProperties | null;
}

export interface PagedRequestDto {
  skipCount?: number;
  maxResultCount?: number;
  sorting?: string | null;
}

export interface PagedResultDto<TItem> {
  totalCount: number;
  items: TItem[];
}

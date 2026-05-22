import type { AuditedEntityDto, ConcurrencyStampedDto, EntityId } from "../../types/entities";

export const reportFilterOperators = ["equals", "contains", "is_empty", "is_not_empty"] as const;
export const reportSortDirections = ["asc", "desc"] as const;
export const reportSystemFields = [
  { id: "status", label: "Record status" },
  { id: "created_at", label: "Created date" },
  { id: "created_by_id", label: "Created by" }
] as const;

export type ReportFilterOperator = (typeof reportFilterOperators)[number];
export type ReportSortDirection = (typeof reportSortDirections)[number];

export type ListReportColumn = {
  fieldId: string;
  label: string;
  visible: boolean;
  width?: number | null;
};

export type ListReportFilter = {
  fieldId: string;
  operator: ReportFilterOperator;
  value?: string | null;
};

export type ListReportSort = {
  fieldId: string;
  direction: ReportSortDirection;
};

export type ListReportConfig = {
  schemaVersion: 1;
  columns: ListReportColumn[];
  filters: ListReportFilter[];
  sort: ListReportSort[];
};

export type CreateListReportRequest = {
  name: string;
  config: ListReportConfig;
};

export interface ListReportSummary extends AuditedEntityDto, ConcurrencyStampedDto {
  formId: EntityId;
  formName: string;
  name: string;
  type: "list";
  columnCount: number;
  filterCount: number;
  sortCount: number;
}

export interface ListReportDetail extends AuditedEntityDto, ConcurrencyStampedDto {
  formId: EntityId;
  formName: string;
  name: string;
  type: "list";
  config: ListReportConfig;
}

export type ReportValidationError = {
  path: string;
  code: string;
  message: string;
};

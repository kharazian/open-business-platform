import { reportableSystemFields } from "../forms/reportableFields";
import type { AuditedEntityDto, ConcurrencyStampedDto, EntityId } from "../../types/entities";

export const reportFilterOperators = ["equals", "contains", "is_empty", "is_not_empty"] as const;
export const reportSortDirections = ["asc", "desc"] as const;
export const reportSystemFields = reportableSystemFields;

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

export type ExecuteListReportOptions = {
  page?: number;
  pageSize?: number;
  search?: string;
};

export type ListReportExecutionColumn = {
  fieldId: string;
  label: string;
  type: string;
  source: "form" | "system";
  width?: number | null;
};

export type ListReportExecutionCell = {
  value: string | number | boolean | null;
  displayValue: string;
};

export type ListReportExecutionRow = {
  recordId: EntityId;
  status: string;
  cells: Record<string, ListReportExecutionCell | undefined>;
  createdAt: string;
};

export type ListReportExecution = {
  reportId: EntityId;
  formId: EntityId;
  reportName: string;
  formName: string;
  page: number;
  pageSize: number;
  totalCount: number;
  columns: ListReportExecutionColumn[];
  rows: ListReportExecutionRow[];
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

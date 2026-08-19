import { reportableSystemFields } from "../forms/reportableFields";
import type { AuditedEntityDto, ConcurrencyStampedDto, EntityId } from "../../types/entities";

export const reportFilterOperators = [
  "equals",
  "contains",
  "greater_than",
  "greater_or_equal",
  "less_than",
  "less_or_equal",
  "before",
  "after",
  "is_empty",
  "is_not_empty"
] as const;
export const reportSortDirections = ["asc", "desc"] as const;
export const reportRowOpenActions = ["detail", "edit", "none"] as const;
export const reportActionTypes = ["create_record", "print_report", "export_csv"] as const;
export const reportRowActionTypes = ["view_record", "edit_record", "delete_record"] as const;
export const reportSystemFields = reportableSystemFields;

export type ReportFilterOperator = (typeof reportFilterOperators)[number];
export type ReportSortDirection = (typeof reportSortDirections)[number];
export type ReportRowOpenAction = (typeof reportRowOpenActions)[number];
export type ReportActionType = (typeof reportActionTypes)[number];
export type ReportRowActionType = (typeof reportRowActionTypes)[number];
export type ReportOperationalActionType = ReportActionType | ReportRowActionType;
export type ReportFieldSource = "form" | "system" | "relationship";

export type ReportFieldCatalogItem = {
  id: string;
  label: string;
  type: string;
  source: ReportFieldSource;
  options: Array<{ id: string; label: string; value: string }>;
  filterable: boolean;
  sortable: boolean;
  searchable: boolean;
  supportsAggregation: boolean;
  supportsChoiceGrouping: boolean;
};

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

export type ListReportAction = {
  id: string;
  type: ReportOperationalActionType;
  label: string;
  enabled: boolean;
  confirmation?: string | null;
};

export type ResolvedReportAction = Pick<ListReportAction, "id" | "type" | "label" | "confirmation">;

export type ListReportConfig = {
  schemaVersion: 1;
  columns: ListReportColumn[];
  filters: ListReportFilter[];
  sort: ListReportSort[];
  rowOpenAction?: ReportRowOpenAction;
  reportActions?: ListReportAction[];
  rowActions?: ListReportAction[];
};

export type CreateListReportRequest = {
  name: string;
  config: ListReportConfig;
};

export type UpdateListReportRequest = CreateListReportRequest & {
  concurrencyStamp: string;
};

export type ExecuteListReportOptions = {
  page?: number;
  pageSize?: number;
  search?: string;
  sortFieldId?: string;
  sortDirection?: ReportSortDirection;
  filters?: Record<string, string | undefined>;
};

export type ListReportExecutionColumn = {
  fieldId: string;
  label: string;
  type: string;
  source: ReportFieldSource;
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
  actions: ResolvedReportAction[];
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
  reportActions: ResolvedReportAction[];
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

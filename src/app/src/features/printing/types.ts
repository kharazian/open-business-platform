import type { EntityId } from "../../types/entities";
import type { FormRecordValue, FormSchema } from "../forms/types";

export const printTemplateTypes = ["record", "report"] as const;
export const printTemplateSectionKinds = ["fields", "table", "signature"] as const;
export const printTemplatePageSizes = ["letter", "a4"] as const;
export const printTemplateOrientations = ["portrait", "landscape"] as const;
export const printTemplateMargins = ["narrow", "normal", "wide"] as const;
export const printTemplateConditionOperators = ["equals", "not_equals", "contains", "is_empty", "is_not_empty"] as const;

export type PrintTemplateType = (typeof printTemplateTypes)[number];
export type PrintTemplateSectionKind = (typeof printTemplateSectionKinds)[number];
export type PrintTemplatePageSize = (typeof printTemplatePageSizes)[number];
export type PrintTemplateOrientation = (typeof printTemplateOrientations)[number];
export type PrintTemplateMargin = (typeof printTemplateMargins)[number];
export type PrintTemplateConditionOperator = (typeof printTemplateConditionOperators)[number];

export type PrintTemplateHeaderConfig = {
  title: string;
  subtitle?: string | null;
  logoUrl?: string | null;
  showGeneratedAt: boolean;
};

export type PrintTemplateLayoutConfig = {
  pageSize: PrintTemplatePageSize;
  orientation: PrintTemplateOrientation;
  margin: PrintTemplateMargin;
  repeatTableHeaders: boolean;
};

export type PrintTemplateSectionPaginationConfig = {
  pageBreakBefore: boolean;
  avoidBreakInside: boolean;
};

export type PrintTemplateSectionConditionConfig = {
  fieldId: string;
  operator: PrintTemplateConditionOperator;
  value?: string | null;
};

export type PrintTemplateSectionConfig = {
  id: string;
  kind: PrintTemplateSectionKind;
  title: string;
  fieldIds: string[];
  signatureLabels?: string[];
  pagination?: PrintTemplateSectionPaginationConfig;
  conditions?: PrintTemplateSectionConditionConfig[];
};

export type PrintTemplateFooterConfig = {
  text?: string | null;
};

export type PrintTemplateConfig = {
  schemaVersion: 1;
  type: PrintTemplateType;
  layout: PrintTemplateLayoutConfig;
  header: PrintTemplateHeaderConfig;
  sections: PrintTemplateSectionConfig[];
  footer: PrintTemplateFooterConfig;
};

export type PrintTemplateSummary = {
  id: EntityId;
  formId: EntityId;
  reportId?: EntityId | null;
  name: string;
  description?: string | null;
  type: PrintTemplateType;
  sectionCount: number;
  concurrencyStamp: string;
  createdAt: string;
  createdById?: EntityId | null;
  updatedAt?: string | null;
  updatedById?: EntityId | null;
};

export type PrintTemplateDetail = Omit<PrintTemplateSummary, "sectionCount"> & {
  config: PrintTemplateConfig;
};

export type PrintTemplateDraft = {
  id?: EntityId;
  formId?: EntityId;
  reportId?: EntityId | null;
  name: string;
  description: string;
  type: PrintTemplateType;
  config: PrintTemplateConfig;
  concurrencyStamp?: string;
};

export type CreatePrintTemplateRequest = {
  name: string;
  description?: string | null;
  type: PrintTemplateType;
  reportId?: EntityId | null;
  config: PrintTemplateConfig;
};

export type UpdatePrintTemplateRequest = CreatePrintTemplateRequest & {
  concurrencyStamp: string;
};

export type PrintTemplateValidationError = {
  path: string;
  code: string;
  message: string;
};

export type PrintTemplateValidationResult = {
  valid: boolean;
  errors: PrintTemplateValidationError[];
};

export type PrintTemplateRecordRow = {
  fieldId: string;
  label: string;
  displayValue: string;
};

export type PrintTemplateRecordRows = {
  fields: PrintTemplateRecordRow[];
  signatures: string[];
};

export type PrintTemplateReportColumn = {
  fieldId: string;
  label: string;
};

export type PrintTemplateReportCell = {
  displayValue: string;
};

export type PrintTemplateReportRow = {
  id: EntityId;
  cells: Record<string, PrintTemplateReportCell>;
};

export type PrintTemplateReportRows = {
  columns: PrintTemplateReportColumn[];
  rows: PrintTemplateReportRow[];
};

export type ReportTemplateExecution = {
  columns: PrintTemplateReportColumn[];
  rows: PrintTemplateReportRow[];
};

export type RecordTemplateSource = {
  schema: FormSchema;
  values: Record<string, FormRecordValue>;
};

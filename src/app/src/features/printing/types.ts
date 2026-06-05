import type { EntityId } from "../../types/entities";
import type { FormRecordValue, FormSchema } from "../forms/types";

export const printTemplateTypes = ["record", "report"] as const;
export const printTemplateSectionKinds = ["fields", "table", "signature"] as const;

export type PrintTemplateType = (typeof printTemplateTypes)[number];
export type PrintTemplateSectionKind = (typeof printTemplateSectionKinds)[number];

export type PrintTemplateHeaderConfig = {
  title: string;
  subtitle?: string | null;
  logoUrl?: string | null;
  showGeneratedAt: boolean;
};

export type PrintTemplateSectionConfig = {
  id: string;
  kind: PrintTemplateSectionKind;
  title: string;
  fieldIds: string[];
  signatureLabels?: string[];
};

export type PrintTemplateFooterConfig = {
  text?: string | null;
};

export type PrintTemplateConfig = {
  schemaVersion: 1;
  type: PrintTemplateType;
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

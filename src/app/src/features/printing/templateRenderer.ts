import type { FormField, FormRecordValue, FormSchema } from "../forms/types";
import type {
  PrintTemplateConfig,
  PrintTemplateRecordRows,
  PrintTemplateReportRows,
  ReportTemplateExecution
} from "./types";

export function buildRecordTemplateRows(
  config: PrintTemplateConfig,
  schema: FormSchema,
  values: Record<string, FormRecordValue>
): PrintTemplateRecordRows {
  const fieldsById = new Map(schema.fields.map((field) => [field.id, field]));
  const selectedFieldIds = getSelectedFieldIds(config, schema.fields.map((field) => field.id));
  const fields = selectedFieldIds
    .map((fieldId) => fieldsById.get(fieldId))
    .filter((field): field is FormField => Boolean(field))
    .map((field) => ({
      fieldId: field.id,
      label: field.label,
      displayValue: formatRecordValue(values[field.id])
    }));
  const signatures = config.sections
    .filter((section) => section.kind === "signature")
    .flatMap((section) => section.signatureLabels ?? [])
    .map((label) => label.trim())
    .filter(Boolean);

  return { fields, signatures };
}

export function buildReportTemplateRows(config: PrintTemplateConfig, execution: ReportTemplateExecution): PrintTemplateReportRows {
  const selectedFieldIds = getSelectedFieldIds(config, execution.columns.map((column) => column.fieldId));
  const columns = execution.columns.filter((column) => selectedFieldIds.includes(column.fieldId));
  const allowed = new Set(columns.map((column) => column.fieldId));

  return {
    columns,
    rows: execution.rows.map((row) => ({
      id: row.id,
      cells: Object.fromEntries(Object.entries(row.cells).filter(([fieldId]) => allowed.has(fieldId)))
    }))
  };
}

export function getPrintTemplatePdfButtonLabel(_config?: PrintTemplateConfig | null): string {
  return "Generate PDF";
}

export function formatRecordValue(value: FormRecordValue): string {
  if (value === null || value === undefined || value === "") {
    return "-";
  }

  if (Array.isArray(value)) {
    return value.length > 0 ? value.join(", ") : "-";
  }

  if (typeof value === "boolean") {
    return value ? "Yes" : "No";
  }

  return String(value);
}

function getSelectedFieldIds(config: PrintTemplateConfig, fallbackFieldIds: string[]): string[] {
  const fieldIds = config.sections
    .filter((section) => section.kind === "fields" || section.kind === "table")
    .flatMap((section) => section.fieldIds)
    .filter(Boolean);

  return fieldIds.length > 0 ? fieldIds : fallbackFieldIds;
}

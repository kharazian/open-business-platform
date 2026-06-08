import type { FormField, FormRecordValue, FormSchema } from "../forms/types";
import type {
  PrintTemplateConfig,
  PrintTemplateRecordRows,
  PrintTemplateReportRows,
  PrintTemplateSectionConditionConfig,
  PrintTemplateSectionConfig,
  ReportTemplateExecution
} from "./types";

export type PrintTemplateConditionSource =
  | { type: "record"; values: Record<string, FormRecordValue> }
  | { type: "report"; report: ReportTemplateExecution };

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

export function getPrintTemplateDocumentClassName(config: PrintTemplateConfig): string {
  const layout = config.layout;
  const classNames = [
    "print-only",
    "print-template-document",
    `print-template-page-${layout.pageSize}-${layout.orientation}-${layout.margin}`
  ];

  if (!layout.repeatTableHeaders) {
    classNames.push("print-template-no-repeat-table-headers");
  }

  return classNames.join(" ");
}

export function getPrintTemplateSectionClassName(section: PrintTemplateSectionConfig): string {
  const pagination = section.pagination ?? { pageBreakBefore: false, avoidBreakInside: true };
  const classNames = ["print-template-section"];

  if (pagination.pageBreakBefore) {
    classNames.push("print-template-section-break-before");
  }

  if (!pagination.avoidBreakInside) {
    classNames.push("print-template-section-allow-breaks");
  }

  return classNames.join(" ");
}

export function shouldRenderPrintTemplateSection(
  section: PrintTemplateSectionConfig,
  source: PrintTemplateConditionSource | null
): boolean {
  const conditions = section.conditions ?? [];

  if (conditions.length === 0) {
    return true;
  }

  if (source === null) {
    return false;
  }

  if (source.type === "record") {
    return conditions.every((condition) => matchesCondition(recordConditionValue(source.values, condition.fieldId), condition));
  }

  return source.report.rows.some((row) =>
    conditions.every((condition) => matchesCondition(row.cells[condition.fieldId]?.displayValue ?? "", condition))
  );
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

function recordConditionValue(values: Record<string, FormRecordValue>, fieldId: string): string {
  const value = values[fieldId];

  if (value === null || value === undefined) {
    return "";
  }

  if (Array.isArray(value)) {
    return value.join(", ");
  }

  if (typeof value === "boolean") {
    return value ? "Yes" : "No";
  }

  return String(value);
}

function matchesCondition(value: string, condition: PrintTemplateSectionConditionConfig): boolean {
  const actual = value.trim();
  const expected = (condition.value ?? "").trim();
  const actualLower = actual.toLowerCase();
  const expectedLower = expected.toLowerCase();

  switch (condition.operator) {
    case "equals":
      return actualLower === expectedLower;
    case "not_equals":
      return actualLower !== expectedLower;
    case "contains":
      return expectedLower.length > 0 && actualLower.includes(expectedLower);
    case "is_empty":
      return actual.length === 0;
    case "is_not_empty":
      return actual.length > 0;
    default:
      return false;
  }
}

function getSelectedFieldIds(config: PrintTemplateConfig, fallbackFieldIds: string[]): string[] {
  const fieldIds = config.sections
    .filter((section) => section.kind === "fields" || section.kind === "table")
    .flatMap((section) => section.fieldIds)
    .filter(Boolean);

  return fieldIds.length > 0 ? fieldIds : fallbackFieldIds;
}

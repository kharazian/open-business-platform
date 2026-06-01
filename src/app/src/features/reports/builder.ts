import { getReportableFields, type ReportableField, type ReportableFieldOption } from "../forms/reportableFields";
import type { FormSchema } from "../forms/types";
import type { ListReportConfig, ListReportFilter, ListReportSort } from "./types";

export type ReportFieldOption = {
  id: string;
  label: string;
  type: string;
  source: "form" | "system";
  options: ReportableFieldOption[];
};

export function getReportFieldOptions(schema: FormSchema | null | undefined): ReportFieldOption[] {
  return getReportableFields(schema).map(toReportFieldOption);
}

export function createListReportConfig(input: {
  fieldOptions: ReportFieldOption[];
  selectedFieldIds: string[];
  columnLabels?: Record<string, string | undefined>;
  filters?: ListReportFilter[];
  sort?: ListReportSort[];
}): ListReportConfig {
  const selectedFields = input.selectedFieldIds.filter((fieldId, index, fields) => fields.indexOf(fieldId) === index);
  const fieldsById = new Map(input.fieldOptions.map((field) => [field.id, field]));
  const columns = selectedFields
    .map((fieldId) => fieldsById.get(fieldId))
    .filter((field): field is ReportFieldOption => Boolean(field))
    .map((field) => ({
      fieldId: field.id,
      label: input.columnLabels?.[field.id]?.trim() || field.label,
      visible: true,
      width: field.source === "system" ? 140 : 180
    }));

  return {
    schemaVersion: 1,
    columns,
    filters: input.filters ?? [],
    sort: input.sort ?? []
  };
}

function toReportFieldOption(field: ReportableField): ReportFieldOption {
  return {
    id: field.id,
    label: field.label,
    type: field.type,
    source: field.source,
    options: field.options
  };
}

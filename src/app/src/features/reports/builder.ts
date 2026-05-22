import type { FormField, FormSchema } from "../forms/types";
import { reportSystemFields, type ListReportConfig, type ListReportFilter, type ListReportSort } from "./types";

export type ReportFieldOption = {
  id: string;
  label: string;
  source: "form" | "system";
};

export function getReportFieldOptions(schema: FormSchema | null | undefined): ReportFieldOption[] {
  const formFields = schema?.fields.map(toReportFieldOption) ?? [];
  const systemFields = reportSystemFields.map((field) => ({ id: field.id, label: field.label, source: "system" as const }));

  return [...formFields, ...systemFields];
}

export function createListReportConfig(input: {
  fieldOptions: ReportFieldOption[];
  selectedFieldIds: string[];
  filters?: ListReportFilter[];
  sort?: ListReportSort[];
}): ListReportConfig {
  const selectedFields = input.selectedFieldIds.filter((fieldId, index, fields) => fields.indexOf(fieldId) === index);
  const columns = input.fieldOptions
    .filter((field) => selectedFields.includes(field.id))
    .map((field) => ({
      fieldId: field.id,
      label: field.label,
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

function toReportFieldOption(field: FormField): ReportFieldOption {
  return {
    id: field.id,
    label: field.label,
    source: "form"
  };
}

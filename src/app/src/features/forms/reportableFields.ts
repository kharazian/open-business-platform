import type { FormField, FormSchema } from "./types";

export const reportableFieldSources = ["form", "system"] as const;

export type ReportableFieldSource = (typeof reportableFieldSources)[number];

export type ReportableFieldOption = {
  id: string;
  label: string;
  value: string;
};

export type ReportableField = {
  id: string;
  label: string;
  type: string;
  source: ReportableFieldSource;
  options: ReportableFieldOption[];
  filterable: boolean;
  sortable: boolean;
  searchable: boolean;
  supportsAggregation: boolean;
  supportsChoiceGrouping: boolean;
};

export const reportableSystemFields = [
  createSystemField("status", "Record status", "status", {
    searchable: true,
    supportsChoiceGrouping: true
  }),
  createSystemField("created_at", "Created date", "datetime"),
  createSystemField("created_by_id", "Created by", "user"),
  createSystemField("updated_at", "Updated date", "datetime"),
  createSystemField("updated_by_id", "Updated by", "user"),
  createSystemField("owner_id", "Owner", "user"),
  createSystemField("department_id", "Department", "department", {
    supportsChoiceGrouping: true
  })
] as const satisfies readonly ReportableField[];

export const reportableSystemFieldIds = reportableSystemFields.map((field) => field.id);

export function getReportableFields(schema: FormSchema | null | undefined): ReportableField[] {
  const formFields = schema?.fields.map(toReportableField) ?? [];
  return [...formFields, ...reportableSystemFields];
}

function toReportableField(field: FormField): ReportableField {
  const isChoice = field.type === "select" || field.type === "radio";

  return {
    id: field.id,
    label: field.label,
    type: field.type,
    source: "form",
    options: (field.options ?? []).map((option) => ({
      id: option.id,
      label: option.label,
      value: option.value
    })),
    filterable: true,
    sortable: true,
    searchable: ["text", "textarea", "email", "phone", "select", "radio"].includes(field.type),
    supportsAggregation: field.type === "number",
    supportsChoiceGrouping: isChoice
  };
}

function createSystemField(
  id: string,
  label: string,
  type: string,
  capabilities: Partial<Pick<ReportableField, "searchable" | "supportsAggregation" | "supportsChoiceGrouping">> = {}
): ReportableField {
  return {
    id,
    label,
    type,
    source: "system",
    options: [],
    filterable: true,
    sortable: true,
    searchable: capabilities.searchable ?? false,
    supportsAggregation: capabilities.supportsAggregation ?? false,
    supportsChoiceGrouping: capabilities.supportsChoiceGrouping ?? false
  };
}

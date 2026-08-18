import { getReportableFields, type ReportableField } from "../forms/reportableFields";
import type { FormSchema } from "../forms/types";
import type { ListReportConfig, ListReportFilter, ListReportSort, ReportFieldCatalogItem, ReportFilterOperator, ReportRowOpenAction, ReportSortDirection } from "./types";

export type ReportFieldOption = Pick<ReportFieldCatalogItem, "id" | "label" | "type" | "source" | "options">;

export type ReportFilterOperatorOption = {
  label: string;
  value: ReportFilterOperator;
};

export type ReportFilterValueInputType = "text" | "number" | "date" | "datetime-local" | "time";

export type ReportFilterDraft = {
  id: string;
  fieldId: string;
  operator: ReportFilterOperator;
  value: string;
};

export type ReportSortDraft = {
  id: string;
  fieldId: string;
  direction: ReportSortDirection;
};

export type ReportFilterDraftValidationErrors = {
  fieldId?: string;
  operator?: string;
  value?: string;
};

export type ReportSortDraftValidationErrors = {
  fieldId?: string;
};

export type ReportBuilderValidationResult = {
  isValid: boolean;
  filterErrorsById: Record<string, ReportFilterDraftValidationErrors>;
  sortErrorsById: Record<string, ReportSortDraftValidationErrors>;
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
  rowOpenAction?: ReportRowOpenAction;
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
    sort: input.sort ?? [],
    rowOpenAction: input.rowOpenAction ?? "detail"
  };
}

export function toListReportFilters(drafts: ReportFilterDraft[]): ListReportFilter[] {
  return drafts
    .map((draft) => ({
      fieldId: draft.fieldId.trim(),
      operator: draft.operator,
      value: filterOperatorRequiresValue(draft.operator) ? normalizeOptionalText(draft.value) : null
    }))
    .filter((filter) => filter.fieldId.length > 0);
}

export function toListReportSorts(drafts: ReportSortDraft[]): ListReportSort[] {
  return drafts
    .map((draft) => ({
      fieldId: draft.fieldId.trim(),
      direction: draft.direction
    }))
    .filter((sort) => sort.fieldId.length > 0);
}

export function filterOperatorRequiresValue(operator: ReportFilterOperator): boolean {
  return operator !== "is_empty" && operator !== "is_not_empty";
}

export function getReportFilterOperatorOptions(field: ReportFieldOption | null | undefined): ReportFilterOperatorOption[] {
  if (!field) {
    return defaultFilterOperatorOptions;
  }

  if (isNumericReportField(field)) {
    return numericFilterOperatorOptions;
  }

  if (isTemporalReportField(field)) {
    return temporalFilterOperatorOptions;
  }

  if (isChoiceReportField(field)) {
    return choiceFilterOperatorOptions;
  }

  return defaultFilterOperatorOptions;
}

export function getReportFilterValueInputType(field: ReportFieldOption | null | undefined): ReportFilterValueInputType {
  if (!field) {
    return "text";
  }

  if (isNumericReportField(field)) {
    return "number";
  }

  if (field.type === "date") {
    return "date";
  }

  if (field.type === "datetime") {
    return "datetime-local";
  }

  if (field.type === "time") {
    return "time";
  }

  return "text";
}

export function getReportFilterValueOptions(field: ReportFieldOption | null | undefined): Array<{ label: string; value: string }> {
  if (!field || !isChoiceReportField(field)) {
    return [];
  }

  return field.options.map((option) => ({ label: option.label, value: option.value }));
}

export function validateReportBuilderDrafts(input: {
  fieldOptions: ReportFieldOption[];
  filterDrafts: ReportFilterDraft[];
  sortDrafts: ReportSortDraft[];
}): ReportBuilderValidationResult {
  const validFieldIds = new Set(input.fieldOptions.map((field) => field.id));
  const filterErrorsById: Record<string, ReportFilterDraftValidationErrors> = {};
  const sortErrorsById: Record<string, ReportSortDraftValidationErrors> = {};
  const sortFieldCounts = new Map<string, number>();

  for (const sortDraft of input.sortDrafts) {
    const fieldId = sortDraft.fieldId.trim();

    if (fieldId) {
      sortFieldCounts.set(fieldId, (sortFieldCounts.get(fieldId) ?? 0) + 1);
    }
  }

  for (const filterDraft of input.filterDrafts) {
    const fieldId = filterDraft.fieldId.trim();

    if (!fieldId) {
      continue;
    }

    const errors: ReportFilterDraftValidationErrors = {};

    if (!validFieldIds.has(fieldId)) {
      errors.fieldId = "Filter field is not available.";
    }

    const field = input.fieldOptions.find((option) => option.id === fieldId);
    if (field && !getReportFilterOperatorOptions(field).some((option) => option.value === filterDraft.operator)) {
      errors.operator = "Filter operator is not available for this field.";
    }

    if (filterOperatorRequiresValue(filterDraft.operator) && !filterDraft.value.trim()) {
      errors.value = "Filter value is required.";
    }

    if (field && filterOperatorRequiresValue(filterDraft.operator) && filterDraft.value.trim()) {
      const valueError = validateFilterValueForField(field, filterDraft.value);

      if (valueError) {
        errors.value = valueError;
      }
    }

    if (hasValidationErrors(errors)) {
      filterErrorsById[filterDraft.id] = errors;
    }
  }

  for (const sortDraft of input.sortDrafts) {
    const fieldId = sortDraft.fieldId.trim();
    const errors: ReportSortDraftValidationErrors = {};

    if (!fieldId) {
      errors.fieldId = "Sort field is required.";
    } else if (!validFieldIds.has(fieldId)) {
      errors.fieldId = "Sort field is not available.";
    } else if ((sortFieldCounts.get(fieldId) ?? 0) > 1) {
      errors.fieldId = "Sort field is already used.";
    }

    if (hasValidationErrors(errors)) {
      sortErrorsById[sortDraft.id] = errors;
    }
  }

  return {
    isValid: Object.keys(filterErrorsById).length === 0 && Object.keys(sortErrorsById).length === 0,
    filterErrorsById,
    sortErrorsById
  };
}

function normalizeOptionalText(value: string): string | null {
  const normalized = value.trim();
  return normalized.length > 0 ? normalized : null;
}

function hasValidationErrors(errors: ReportFilterDraftValidationErrors | ReportSortDraftValidationErrors): boolean {
  return Object.values(errors).some(Boolean);
}

const defaultFilterOperatorOptions: ReportFilterOperatorOption[] = [
  { label: "Equals", value: "equals" },
  { label: "Contains", value: "contains" },
  { label: "Is empty", value: "is_empty" },
  { label: "Is not empty", value: "is_not_empty" }
];

const numericFilterOperatorOptions: ReportFilterOperatorOption[] = [
  { label: "Equals", value: "equals" },
  { label: "Greater than", value: "greater_than" },
  { label: "Greater or equal", value: "greater_or_equal" },
  { label: "Less than", value: "less_than" },
  { label: "Less or equal", value: "less_or_equal" },
  { label: "Is empty", value: "is_empty" },
  { label: "Is not empty", value: "is_not_empty" }
];

const temporalFilterOperatorOptions: ReportFilterOperatorOption[] = [
  { label: "Equals", value: "equals" },
  { label: "Before", value: "before" },
  { label: "After", value: "after" },
  { label: "Is empty", value: "is_empty" },
  { label: "Is not empty", value: "is_not_empty" }
];

const choiceFilterOperatorOptions: ReportFilterOperatorOption[] = [
  { label: "Equals", value: "equals" },
  { label: "Is empty", value: "is_empty" },
  { label: "Is not empty", value: "is_not_empty" }
];

function isNumericReportField(field: ReportFieldOption): boolean {
  return field.type === "number" || field.type === "currency" || field.type === "percent" || field.type === "rating";
}

function isTemporalReportField(field: ReportFieldOption): boolean {
  return field.type === "date" || field.type === "datetime" || field.type === "time";
}

function isChoiceReportField(field: ReportFieldOption): boolean {
  return field.type === "select" || field.type === "radio" || field.type === "status";
}

function validateFilterValueForField(field: ReportFieldOption, value: string): string | undefined {
  const normalizedValue = value.trim();

  if (isNumericReportField(field) && !Number.isFinite(Number(normalizedValue))) {
    return "Filter value must be a number.";
  }

  if (field.type === "time" && !isValidTimeValue(normalizedValue)) {
    return "Filter value must be a valid time.";
  }

  if ((field.type === "date" || field.type === "datetime") && Number.isNaN(Date.parse(normalizedValue))) {
    return "Filter value must be a valid date/time.";
  }

  const valueOptions = getReportFilterValueOptions(field);
  if (valueOptions.length > 0 && !valueOptions.some((option) => option.value === normalizedValue)) {
    return "Choose an available filter value.";
  }

  return undefined;
}

function isValidTimeValue(value: string): boolean {
  return /^([01]\d|2[0-3]):[0-5]\d(?::[0-5]\d)?$/.test(value);
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

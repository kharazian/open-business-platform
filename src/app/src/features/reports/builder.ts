import { getReportableFields, type ReportableField, type ReportableFieldOption } from "../forms/reportableFields";
import type { FormSchema } from "../forms/types";
import type { ListReportConfig, ListReportFilter, ListReportSort, ReportFilterOperator, ReportSortDirection } from "./types";

export type ReportFieldOption = {
  id: string;
  label: string;
  type: string;
  source: "form" | "system";
  options: ReportableFieldOption[];
};

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
  return operator === "equals" || operator === "contains";
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

    if (filterOperatorRequiresValue(filterDraft.operator) && !filterDraft.value.trim()) {
      errors.value = "Filter value is required.";
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

function toReportFieldOption(field: ReportableField): ReportFieldOption {
  return {
    id: field.id,
    label: field.label,
    type: field.type,
    source: field.source,
    options: field.options
  };
}

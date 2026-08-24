import type { ReportableField } from "../forms/reportableFields";
import type { DashboardAnalyticsFilterValue, DashboardFilterDefinition, DashboardFilterType, SavedDashboardWidget } from "./types";

export const dashboardFilterLimit = 8;

export function getCompatibleDashboardFilterTypes(field: ReportableField): DashboardFilterType[] {
  if (field.type === "date" || field.type === "datetime") return ["date_range"];
  if (field.id === "status") return ["record_status", "single_select", "multi_select"];
  return ["single_select", "multi_select"];
}

export function createDashboardFilter(formId: string, field: ReportableField, existing: DashboardFilterDefinition[]): DashboardFilterDefinition {
  const compatibleTypes = getCompatibleDashboardFilterTypes(field);
  return {
    id: createDashboardFilterId(field.id, existing),
    label: field.label,
    type: compatibleTypes[0],
    sourceFormId: formId,
    fieldId: field.id,
    options: field.options.map((option) => option.value),
    defaultValue: null,
    required: false,
    applyToWidgetIds: null
  };
}

export function updateDashboardFilterField(filter: DashboardFilterDefinition, field: ReportableField): DashboardFilterDefinition {
  const types = getCompatibleDashboardFilterTypes(field);
  return {
    ...filter,
    label: field.label,
    fieldId: field.id,
    type: types.includes(filter.type) ? filter.type : types[0],
    options: field.options.map((option) => option.value),
    defaultValue: null
  };
}

export function moveDashboardFilter(filters: DashboardFilterDefinition[], filterId: string, direction: -1 | 1): DashboardFilterDefinition[] {
  const index = filters.findIndex((filter) => filter.id === filterId);
  const target = index + direction;
  if (index < 0 || target < 0 || target >= filters.length) return filters;
  const next = [...filters];
  [next[index], next[target]] = [next[target], next[index]];
  return next;
}

export function getDashboardFilterDefaults(definitions: DashboardFilterDefinition[]): Record<string, DashboardAnalyticsFilterValue | undefined> {
  return Object.fromEntries(definitions.map((definition) => [definition.id, hasDashboardFilterValue(definition.defaultValue, definition.type) ? { ...definition.defaultValue!, fieldId: definition.fieldId } : undefined]));
}

export function hasDashboardFilterValue(value: DashboardAnalyticsFilterValue | null | undefined, type?: DashboardFilterType): boolean {
  if (!value) return false;
  if (type === "date_range") return Boolean(value.start && value.end);
  return (value.values?.length ?? 0) > 0;
}

export function getMissingRequiredDashboardFilters(definitions: DashboardFilterDefinition[], selections: Record<string, DashboardAnalyticsFilterValue | undefined>): DashboardFilterDefinition[] {
  return definitions.filter((definition) => definition.required && !hasDashboardFilterValue(selections[definition.id], definition.type));
}

export function getCompatibleFilterWidgets(filter: DashboardFilterDefinition, widgets: SavedDashboardWidget[]): SavedDashboardWidget[] {
  return widgets.filter((widget) => widget.sourceFormId === filter.sourceFormId && Boolean(widget.chart));
}

function createDashboardFilterId(fieldId: string, existing: DashboardFilterDefinition[]): string {
  const base = `filter-${fieldId}`.toLowerCase().replace(/[^a-z0-9_-]+/g, "-").replace(/-+/g, "-").slice(0, 42) || "filter";
  let candidate = base;
  let suffix = 2;
  while (existing.some((filter) => filter.id === candidate)) candidate = `${base.slice(0, 45)}-${suffix++}`;
  return candidate;
}

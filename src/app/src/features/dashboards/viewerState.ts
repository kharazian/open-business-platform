import type { DashboardAnalyticsFilterValue, DashboardFilterDefinition } from "./types";

const stateVersion = "1";
const maximumValueLength = 100;
const maximumValuesPerFilter = 20;

export type DashboardViewerUrlState = {
  activeSectionId: string | null;
  filters: Record<string, DashboardAnalyticsFilterValue | undefined>;
};

export function readDashboardViewerUrlState(
  searchParams: URLSearchParams,
  sectionIds: ReadonlySet<string>,
  definitions: DashboardFilterDefinition[]
): DashboardViewerUrlState {
  if (searchParams.get("dv") !== stateVersion) return { activeSectionId: null, filters: {} };
  const requestedSection = searchParams.get("tab");
  const activeSectionId = requestedSection && sectionIds.has(requestedSection) ? requestedSection : null;
  const filters: DashboardViewerUrlState["filters"] = {};

  for (const definition of definitions.slice(0, 8)) {
    if (definition.type === "date_range") {
      const start = normalizeDate(searchParams.get(`filter.${definition.id}.start`));
      const end = normalizeDate(searchParams.get(`filter.${definition.id}.end`));
      if (start && end && start < end) filters[definition.id] = { fieldId: definition.fieldId, start, end };
      continue;
    }

    const allowed = new Set(definition.options ?? []);
    const values = searchParams.getAll(`filter.${definition.id}`).filter((value) =>
      value.length > 0 && value.length <= maximumValueLength && allowed.has(value)
    ).slice(0, definition.type === "multi_select" ? maximumValuesPerFilter : 1);
    const uniqueValues = [...new Set(values)];
    if (uniqueValues.length > 0) filters[definition.id] = { fieldId: definition.fieldId, values: uniqueValues };
  }

  return { activeSectionId, filters };
}

export function writeDashboardViewerUrlState(
  activeSectionId: string,
  definitions: DashboardFilterDefinition[],
  filters: Record<string, DashboardAnalyticsFilterValue | undefined>
): URLSearchParams {
  const searchParams = new URLSearchParams({ dv: stateVersion, tab: activeSectionId });
  for (const definition of definitions.slice(0, 8)) {
    const value = filters[definition.id];
    if (!value) continue;
    if (definition.type === "date_range") {
      const start = normalizeDate(value.start ?? null);
      const end = normalizeDate(value.end ?? null);
      if (start && end && start < end) {
        searchParams.set(`filter.${definition.id}.start`, start);
        searchParams.set(`filter.${definition.id}.end`, end);
      }
      continue;
    }
    const allowed = new Set(definition.options ?? []);
    [...new Set(value.values ?? [])].filter((item) => item.length <= maximumValueLength && allowed.has(item))
      .slice(0, definition.type === "multi_select" ? maximumValuesPerFilter : 1)
      .forEach((item) => searchParams.append(`filter.${definition.id}`, item));
  }
  return searchParams;
}

function normalizeDate(value: string | null): string | null {
  return value && /^\d{4}-\d{2}-\d{2}$/.test(value) ? value : null;
}

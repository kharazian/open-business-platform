import type { DashboardAnalyticsFilterValue, DashboardFilterDefinition, SavedDashboardWidget } from "./types";

export type DashboardPointSelection = { key: string; label: string; value: number; recordId?: string };
export type DashboardDrillFilters = Record<string, string>;

export function buildDashboardDrillThroughPath(
  widget: SavedDashboardWidget,
  definitions: DashboardFilterDefinition[] | null | undefined,
  selections: Record<string, DashboardAnalyticsFilterValue | undefined>,
  point?: DashboardPointSelection
): string | null {
  const interaction = widget.interaction;
  if (!interaction || !widget.sourceFormId) return null;
  if (interaction.destination === "records" && point?.recordId) return `/records/${encodeURIComponent(point.recordId)}`;

  const params = new URLSearchParams({ drill: "1" });
  if (interaction.includeDashboardFilters !== false) {
    for (const definition of definitions ?? []) {
      if (definition.sourceFormId !== widget.sourceFormId || (definition.applyToWidgetIds && !definition.applyToWidgetIds.includes(widget.id))) continue;
      const values = selections[definition.id]?.values;
      if (values?.length === 1) params.set(`filter.${definition.fieldId}`, values[0].slice(0, 200));
    }
  }
  const pointFieldId = getDashboardPointFieldId(widget);
  if (interaction.includePointFilter !== false && point && pointFieldId) params.set(`filter.${pointFieldId}`, point.key.slice(0, 200));

  if (interaction.destination === "report") {
    if (!interaction.reportId) return null;
    params.set("formId", widget.sourceFormId);
    params.set("reportId", interaction.reportId);
    return `/reports?${params.toString()}`;
  }
  return `/forms/${encodeURIComponent(widget.sourceFormId)}/records?${params.toString()}`;
}

export function readDashboardDrillFilters(searchParams: URLSearchParams): DashboardDrillFilters {
  const filters: DashboardDrillFilters = {};
  if (searchParams.get("drill") !== "1") return filters;
  for (const [key, value] of searchParams.entries()) {
    if (!key.startsWith("filter.") || Object.keys(filters).length >= 8) continue;
    const fieldId = key.slice(7);
    if (!fieldId || fieldId.length > 100 || value.length > 200) continue;
    filters[fieldId] = value;
  }
  return filters;
}

export function getDashboardPointFieldId(widget: SavedDashboardWidget): string | null {
  if (!widget.chart) return null;
  if (widget.chart.widgetType === "choice_breakdown") return widget.chart.groupByFieldId ?? null;
  if (widget.chart.widgetType === "date_trend") return widget.chart.dateFieldId ?? null;
  return null;
}

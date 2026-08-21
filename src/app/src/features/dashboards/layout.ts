import type { DashboardWidgetWidth, SavedDashboardWidgetLayout } from "./types";

export function orderDashboardLayoutWidgets(widgets: SavedDashboardWidgetLayout[]): SavedDashboardWidgetLayout[] {
  return [...widgets].sort((left, right) => left.order - right.order || left.id.localeCompare(right.id));
}

export function moveDashboardLayoutWidget(
  widgets: SavedDashboardWidgetLayout[],
  sourceId: string,
  targetId: string | null
): SavedDashboardWidgetLayout[] {
  const ordered = orderDashboardLayoutWidgets(widgets);
  const source = ordered.find((widget) => widget.id === sourceId);
  if (!source || sourceId === targetId) return ordered;
  const remaining = ordered.filter((widget) => widget.id !== sourceId);
  const targetIndex = targetId ? remaining.findIndex((widget) => widget.id === targetId) : remaining.length;
  if (targetIndex < 0) return ordered;
  remaining.splice(targetIndex, 0, source);
  return remaining.map((widget, order) => ({ ...widget, order }));
}

export function getDashboardWidgetGridClass(width: DashboardWidgetWidth): string {
  switch (width) {
    case "small":
      return "md:col-span-3";
    case "medium":
      return "md:col-span-6";
    case "wide":
      return "md:col-span-9";
    case "full":
      return "md:col-span-12";
  }
}

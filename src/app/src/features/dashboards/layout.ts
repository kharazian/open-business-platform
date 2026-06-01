import type { DashboardWidgetWidth, SavedDashboardWidgetLayout } from "./types";

export function orderDashboardLayoutWidgets(widgets: SavedDashboardWidgetLayout[]): SavedDashboardWidgetLayout[] {
  return [...widgets].sort((left, right) => left.order - right.order || left.id.localeCompare(right.id));
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

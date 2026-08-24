import type { SavedDashboardSection, SavedDashboardWidget } from "./types";

export function appendBoundedCanvasHistory<T>(history: T[], snapshot: T, limit = 30): T[] { return [...history, snapshot].slice(-limit); }

export function toggleDashboardWidgetSelection(current: ReadonlySet<string>, widgetId: string): Set<string> {
  const next = new Set(current); if (next.has(widgetId)) next.delete(widgetId); else next.add(widgetId); return next;
}

export function canDuplicateDashboardSection(sections: SavedDashboardSection[], widgets: SavedDashboardWidget[], sectionId: string): boolean {
  const count = widgets.filter((widget) => widget.sectionId === sectionId).length;
  return sections.some((section) => section.id === sectionId) && sections.length < 16 && count <= 16 && widgets.length + count <= 48;
}

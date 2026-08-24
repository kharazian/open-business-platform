import { orderDashboardLayoutWidgets } from "./layout";
import type { SavedDashboardSection, SavedDashboardWidget, SavedDashboardWidgetLayout } from "./types";

export const dashboardCanvasQualityLimits = {
  maxSections: 16,
  maxWidgets: 48,
  historyEntries: 30,
  previewConcurrency: 4
} as const;

export function appendBoundedCanvasHistory<T>(history: T[], snapshot: T, limit = dashboardCanvasQualityLimits.historyEntries): T[] { return [...history, snapshot].slice(-limit); }

export function toggleDashboardWidgetSelection(current: ReadonlySet<string>, widgetId: string): Set<string> {
  const next = new Set(current); if (next.has(widgetId)) next.delete(widgetId); else next.add(widgetId); return next;
}

export function canDuplicateDashboardSection(sections: SavedDashboardSection[], widgets: SavedDashboardWidget[], sectionId: string): boolean {
  const count = widgets.filter((widget) => widget.sectionId === sectionId).length;
  return sections.some((section) => section.id === sectionId) && sections.length < dashboardCanvasQualityLimits.maxSections && count <= dashboardCanvasQualityLimits.maxSections && widgets.length + count <= dashboardCanvasQualityLimits.maxWidgets;
}

export function moveDashboardWidgetWithinSection(layout: SavedDashboardWidgetLayout[], widgets: SavedDashboardWidget[], widgetId: string, direction: -1 | 1): SavedDashboardWidgetLayout[] {
  const widget = widgets.find((item) => item.id === widgetId);
  if (!widget) return layout;
  const ordered = orderDashboardLayoutWidgets(layout);
  const sectionWidgetIds = ordered.filter((item) => widgets.find((candidate) => candidate.id === item.id)?.sectionId === widget.sectionId).map((item) => item.id);
  const sectionIndex = sectionWidgetIds.indexOf(widgetId);
  const targetId = sectionWidgetIds[sectionIndex + direction];
  if (sectionIndex < 0 || !targetId) return layout;
  const sourceIndex = ordered.findIndex((item) => item.id === widgetId);
  const targetIndex = ordered.findIndex((item) => item.id === targetId);
  const next = [...ordered];
  [next[sourceIndex], next[targetIndex]] = [next[targetIndex], next[sourceIndex]];
  return next.map((item, index) => ({ ...item, order: index + 1 }));
}

export function getAdjacentDashboardSectionId(sections: SavedDashboardSection[], sectionId: string | null | undefined, direction: -1 | 1): string | null {
  const ordered = [...sections].sort((left, right) => left.order - right.order);
  const index = ordered.findIndex((section) => section.id === sectionId);
  return ordered[index + direction]?.id ?? null;
}

export async function runDashboardTasksWithConcurrency<T>(items: T[], worker: (item: T) => Promise<void>, concurrency = dashboardCanvasQualityLimits.previewConcurrency): Promise<void> {
  let nextIndex = 0;
  const workerCount = Math.min(Math.max(1, concurrency), items.length);
  await Promise.all(Array.from({ length: workerCount }, async () => {
    while (nextIndex < items.length) {
      const item = items[nextIndex++];
      await worker(item);
    }
  }));
}

import type { SavedDashboardSection, SavedDashboardWidget } from "./types";

export const defaultDashboardSection: SavedDashboardSection = {
  id: "overview",
  title: "Overview",
  order: 0
};

export function normalizeDashboardSections(
  sections: SavedDashboardSection[] | null | undefined
): SavedDashboardSection[] {
  const configured = sections ?? [];
  const source = configured.length > 0 ? configured : [defaultDashboardSection];

  return source
    .map((section, index) => ({ section, index }))
    .sort((left, right) => left.section.order - right.section.order || left.index - right.index)
    .map(({ section }, order) => ({ ...section, id: section.id.trim(), title: section.title.trim(), order }));
}

export function assignWidgetsToDashboardSections(
  widgets: SavedDashboardWidget[],
  sections: SavedDashboardSection[]
): SavedDashboardWidget[] {
  const fallbackSectionId = sections[0]?.id ?? defaultDashboardSection.id;
  const validSectionIds = new Set(sections.map((section) => section.id));

  return widgets.map((widget) => ({
    ...widget,
    sectionId: widget.sectionId && validSectionIds.has(widget.sectionId) ? widget.sectionId : fallbackSectionId
  }));
}

export function createDashboardSectionId(title: string, sections: SavedDashboardSection[]): string {
  const base = title
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-|-$/g, "") || "section";
  const ids = new Set(sections.map((section) => section.id));
  let candidate = base;
  let suffix = 2;

  while (ids.has(candidate)) {
    candidate = `${base}-${suffix}`;
    suffix += 1;
  }

  return candidate;
}

export function moveDashboardSection(sections: SavedDashboardSection[], sourceId: string, targetId: string): SavedDashboardSection[] {
  const ordered = normalizeDashboardSections(sections);
  if (sourceId === targetId) return ordered;
  const source = ordered.find((section) => section.id === sourceId);
  const targetIndex = ordered.findIndex((section) => section.id === targetId);
  if (!source || targetIndex < 0) return ordered;
  const remaining = ordered.filter((section) => section.id !== sourceId);
  remaining.splice(targetIndex, 0, source);
  return remaining.map((section, order) => ({ ...section, order }));
}

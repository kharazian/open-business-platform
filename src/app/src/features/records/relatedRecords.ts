import type { RelatedRecordPanel } from "../forms/api";

export function getRelatedPanelKey(panel: Pick<RelatedRecordPanel, "sourceFormId" | "sourceFieldId">): string {
  return `${panel.sourceFormId}:${panel.sourceFieldId}`;
}

export function getRelatedPageCount(totalCount: number, pageSize: number): number {
  return Math.max(1, Math.ceil(Math.max(0, totalCount) / Math.max(1, pageSize)));
}

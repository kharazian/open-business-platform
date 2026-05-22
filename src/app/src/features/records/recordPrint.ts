export function requestBrowserPrint(printAction: () => void = () => window.print()): void {
  printAction();
}

export function getRecordListPrintDescription(
  totalCount: number,
  page: number,
  totalPages: number,
  search: string
): string {
  const recordLabel = totalCount === 1 ? "record" : "records";
  const parts = [`${totalCount} total ${recordLabel}`, `Page ${page} of ${totalPages}`];
  const trimmedSearch = search.trim();

  if (trimmedSearch.length > 0) {
    parts.push(`Filter: ${trimmedSearch}`);
  }

  return parts.join(" | ");
}

export function getRecordDetailPrintDescription(createdAt: string, formVersionId: string): string {
  return `Submitted ${createdAt} | Version ${formVersionId}`;
}

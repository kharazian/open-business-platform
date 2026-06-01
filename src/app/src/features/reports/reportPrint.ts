import { joinPrintMetadata } from "../printing/printLayout";

export function getReportTablePrintDescription(totalCount: number, page: number, totalPages: number, search: string): string {
  const rowLabel = totalCount === 1 ? "row" : "rows";
  const trimmedSearch = search.trim();

  return joinPrintMetadata([
    `${totalCount} matching ${rowLabel}`,
    `Visible page ${page} of ${totalPages}`,
    trimmedSearch ? `Search: ${trimmedSearch}` : null
  ]);
}

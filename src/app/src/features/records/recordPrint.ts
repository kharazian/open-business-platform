import { joinPrintMetadata, requestBrowserPrint } from "../printing/printLayout";

export { requestBrowserPrint };

export function getRecordListPrintDescription(
  totalCount: number,
  page: number,
  totalPages: number,
  search: string
): string {
  const recordLabel = totalCount === 1 ? "record" : "records";
  const trimmedSearch = search.trim();
  return joinPrintMetadata([
    `${totalCount} total ${recordLabel}`,
    `Page ${page} of ${totalPages}`,
    trimmedSearch ? `Filter: ${trimmedSearch}` : null
  ]);
}

export function getRecordDetailPrintDescription(createdAt: string, formVersionId: string): string {
  return `Submitted ${createdAt} | Version ${formVersionId}`;
}

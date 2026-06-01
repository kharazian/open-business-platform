export type PrintMetadataValue = string | null | undefined | false;

export function requestBrowserPrint(printAction: () => void = () => window.print()): void {
  printAction();
}

export function formatPrintDateTime(value: Date | string, locale = "en", timeZone?: string): string {
  return new Intl.DateTimeFormat(locale, {
    month: "short",
    day: "numeric",
    year: "numeric",
    hour: "numeric",
    minute: "2-digit",
    timeZone
  }).format(new Date(value));
}

export function getGeneratedAtPrintMetadata(value: Date | string = new Date(), locale = "en", timeZone?: string): string {
  return `Generated ${formatPrintDateTime(value, locale, timeZone)}`;
}

export function joinPrintMetadata(values: PrintMetadataValue[]): string {
  return values
    .map((value) => (typeof value === "string" ? value.trim() : ""))
    .filter((value) => value.length > 0)
    .join(" | ");
}

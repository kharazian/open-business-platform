export const themeDensity = {
  comfortable: {
    pageGap: "gap-6",
    pagePadding: "px-4 py-6 sm:px-6 lg:px-8",
    cardPadding: "p-5 sm:p-6",
    controlHeight: "min-h-10",
    tableCell: "px-4 py-3"
  },
  compact: {
    pageGap: "gap-4",
    pagePadding: "px-3 py-4 sm:px-5 lg:px-6",
    cardPadding: "p-4",
    controlHeight: "min-h-9",
    tableCell: "px-3 py-2"
  }
} as const;

export type ThemeDensity = keyof typeof themeDensity;

export function isThemeDensity(value: string | null): value is ThemeDensity {
  return value === "comfortable" || value === "compact";
}

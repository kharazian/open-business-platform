import type { DashboardCardAccent, DashboardChartAppearance, DashboardChartPalette, DashboardSeriesColor } from "./types";

export const defaultDashboardChartAppearance: DashboardChartAppearance = { palette: "theme", showLegend: true, showDataLabels: false, showGridlines: true, cardAccent: "none", numberFormat: "auto", currencyCode: "CAD", decimalPlaces: 0 };

export function resolveDashboardChartAppearance(value?: DashboardChartAppearance | null): DashboardChartAppearance {
  return { ...defaultDashboardChartAppearance, ...value };
}

const palettes: Record<DashboardChartPalette, Record<DashboardSeriesColor, string>> = {
  theme: { primary: "var(--color-primary)", info: "#0891b2", success: "var(--color-success)", warning: "var(--color-warning)", danger: "var(--color-danger)", violet: "#7c3aed" },
  cool: { primary: "#2563eb", info: "#0891b2", success: "#0f766e", warning: "#a16207", danger: "#be123c", violet: "#7c3aed" },
  warm: { primary: "#c2410c", info: "#0369a1", success: "#15803d", warning: "#a16207", danger: "#b91c1c", violet: "#9333ea" },
  mono: { primary: "#334155", info: "#475569", success: "#64748b", warning: "#78716c", danger: "#52525b", violet: "#3f3f46" }
};

export function getDashboardSeriesColor(color: DashboardSeriesColor | string, palette: DashboardChartPalette = "theme"): string {
  return palettes[palette][color as DashboardSeriesColor] ?? palettes[palette].primary;
}

export function getDashboardAccentColor(accent?: DashboardCardAccent | null, palette: DashboardChartPalette = "theme"): string | undefined {
  return !accent || accent === "none" ? undefined : getDashboardSeriesColor(accent, palette);
}

export function formatDashboardValue(value: number, appearance: DashboardChartAppearance, locale: string): string {
  if (appearance.numberFormat === "auto") return new Intl.NumberFormat(locale).format(value);
  const options: Intl.NumberFormatOptions = { minimumFractionDigits: appearance.decimalPlaces, maximumFractionDigits: appearance.decimalPlaces };
  if (appearance.numberFormat === "currency") { options.style = "currency"; options.currency = appearance.currencyCode.toUpperCase(); }
  if (appearance.numberFormat === "percent") return `${new Intl.NumberFormat(locale, options).format(value)}%`;
  return new Intl.NumberFormat(locale, options).format(value);
}

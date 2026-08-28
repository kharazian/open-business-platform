import { isDashboardCircularDisplayType } from "./chartPresentation";
import type { ChartWidgetType, DashboardCardAccent, DashboardChartAppearance, DashboardChartPalette, DashboardChartSeriesDefinition, DashboardConditionalFormatting, DashboardConditionalOperator, DashboardKpiTarget, DashboardReferenceLine, DashboardSeriesColor } from "./types";

export const defaultDashboardChartAppearance: DashboardChartAppearance = { palette: "theme", showLegend: true, showDataLabels: false, showGridlines: true, cardAccent: "none", numberFormat: "auto", currencyCode: "CAD", decimalPlaces: 0, conditionalFormatting: { enabled: false, rules: [] }, kpiTarget: { enabled: false, value: 0, label: "Target", direction: "higher_is_better" }, referenceLines: [] };

export function resolveDashboardChartAppearance(value?: Partial<DashboardChartAppearance> | null): DashboardChartAppearance {
  return { ...defaultDashboardChartAppearance, ...value, conditionalFormatting: { ...defaultDashboardChartAppearance.conditionalFormatting, ...value?.conditionalFormatting, rules: value?.conditionalFormatting?.rules?.map((rule) => ({ ...rule })) ?? [] }, kpiTarget: { ...defaultDashboardChartAppearance.kpiTarget, ...value?.kpiTarget }, referenceLines: value?.referenceLines?.map((line) => ({ ...line })) ?? [] };
}

export function cloneDashboardChartAppearance(value?: Partial<DashboardChartAppearance> | null): DashboardChartAppearance { return resolveDashboardChartAppearance(value); }

export function getDashboardEffectiveCardAccent(appearance: DashboardChartAppearance, value?: number | null): DashboardCardAccent {
  return getDashboardConditionalResult(appearance, value)?.accent ?? appearance.cardAccent;
}

export function getDashboardConditionalResult(appearance: DashboardChartAppearance, value?: number | null): { ruleId: string; accent: DashboardSeriesColor; label: string | null } | null {
  if (!appearance.conditionalFormatting.enabled || value === null || value === undefined || !Number.isFinite(value)) return null;
  const rule = appearance.conditionalFormatting.rules.find((candidate) => matchesConditionalRule(value, candidate.operator, candidate.value));
  return rule ? { ruleId: rule.id, accent: rule.accent, label: rule.label?.trim() || null } : null;
}

export function isDashboardConditionalFormattingValid(formatting: DashboardConditionalFormatting, widgetType: ChartWidgetType): boolean {
  if (formatting.rules.length > 5 || (formatting.enabled && (widgetType !== "number_card" || formatting.rules.length < 1))) return false;
  const ids = new Set<string>();
  return formatting.rules.every((rule) => /^[A-Za-z0-9_-]{1,50}$/.test(rule.id) && !ids.has(rule.id) && Boolean(ids.add(rule.id)) && conditionalOperators.has(rule.operator) && Number.isFinite(rule.value) && Math.abs(rule.value) <= 1_000_000_000_000_000 && conditionalAccents.has(rule.accent) && (!formatting.enabled || Boolean(rule.label?.trim())) && (rule.label?.length ?? 0) <= 80);
}

export function isDashboardKpiTargetValid(target: DashboardKpiTarget, widgetType: ChartWidgetType): boolean {
  if (!target.enabled) return true;
  return widgetType === "number_card" && Number.isFinite(target.value) && Math.abs(target.value) <= 1_000_000_000_000_000 && target.label.trim().length > 0 && target.label.length <= 80 && kpiGoalDirections.has(target.direction);
}

export function isDashboardReferenceLinesValid(lines: DashboardReferenceLine[], widgetType: ChartWidgetType, series: Array<Pick<DashboardChartSeriesDefinition, "displayType">> = []): boolean {
  if (lines.length === 0) return true;
  if (!["bar_chart", "choice_breakdown", "date_trend"].includes(widgetType) || series.some((item) => isDashboardCircularDisplayType(item.displayType)) || lines.length > 4) return false;
  const ids = new Set<string>();
  return lines.every((line) => /^[A-Za-z0-9_-]{1,50}$/.test(line.id) && !ids.has(line.id) && Boolean(ids.add(line.id)) && line.label.trim().length > 0 && line.label.length <= 80 && Number.isFinite(line.value) && line.value >= 0 && line.value <= 1_000_000_000_000_000 && conditionalAccents.has(line.color) && referenceLineStyles.has(line.style) && referenceLineAxes.has(line.axis));
}

export function getDashboardKpiTargetSummary(appearance: DashboardChartAppearance, actual?: number | null, locale = "en"): { label: string; target: string; variance: string; outcome: "favorable" | "needs_attention" | "on_target"; progress: number | null } | null {
  const config = appearance.kpiTarget;
  if (!config.enabled || actual === null || actual === undefined || !Number.isFinite(actual)) return null;
  const difference = actual - config.value;
  const direction = difference > 0 ? "above" : difference < 0 ? "below" : null;
  const variance = !direction
    ? "On target"
    : config.value === 0
      ? `${formatDashboardValue(Math.abs(difference), appearance, locale)} ${direction} target`
      : `${new Intl.NumberFormat(locale, { style: "percent", minimumFractionDigits: 1, maximumFractionDigits: 1 }).format(Math.abs(difference) / Math.abs(config.value))} ${direction} target`;
  const favorable = config.direction === "lower_is_better" ? actual < config.value : actual > config.value;
  return { label: config.label.trim(), target: formatDashboardValue(config.value, appearance, locale), variance, outcome: difference === 0 ? "on_target" : favorable ? "favorable" : "needs_attention", progress: getKpiTargetProgress(actual, config.value, config.direction) };
}

const kpiGoalDirections = new Set(["higher_is_better", "lower_is_better"]);
function getKpiTargetProgress(actual: number, target: number, direction: DashboardKpiTarget["direction"]): number | null {
  if (actual < 0 || target < 0) return null;
  const ratio = direction === "higher_is_better" ? (target === 0 ? (actual >= 0 ? 1 : 0) : actual / target) : actual <= target ? 1 : target === 0 ? 0 : target / actual;
  return Math.round(Math.max(0, Math.min(1, ratio)) * 1000) / 10;
}

const conditionalOperators = new Set<DashboardConditionalOperator>(["greater_than", "greater_or_equal", "less_than", "less_or_equal", "equal"]);
const conditionalAccents = new Set<DashboardSeriesColor>(["primary", "info", "success", "warning", "danger", "violet"]);
const referenceLineStyles = new Set(["solid", "dashed", "dotted"]);
const referenceLineAxes = new Set(["left", "right"]);
function matchesConditionalRule(actual: number, operator: DashboardConditionalOperator, threshold: number): boolean {
  if (operator === "greater_than") return actual > threshold;
  if (operator === "greater_or_equal") return actual >= threshold;
  if (operator === "less_than") return actual < threshold;
  if (operator === "less_or_equal") return actual <= threshold;
  return actual === threshold;
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

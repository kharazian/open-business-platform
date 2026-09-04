import { isDashboardCircularDisplayType } from "./chartPresentation";
import type { ChartWidgetType, DashboardBarMode, DashboardBarOrientation, DashboardCardAccent, DashboardCategorySort, DashboardChartAppearance, DashboardChartAxes, DashboardChartPalette, DashboardChartSeriesDefinition, DashboardConditionalFormatting, DashboardConditionalOperator, DashboardDataLabelContent, DashboardDataLabelPosition, DashboardKpiTarget, DashboardLegendPosition, DashboardReferenceLine, DashboardSeriesColor } from "./types";

export const defaultDashboardChartAppearance: DashboardChartAppearance = { palette: "theme", showLegend: true, showDataLabels: false, showGridlines: true, cardAccent: "none", numberFormat: "auto", currencyCode: "CAD", decimalPlaces: 0, displayUnit: "none", conditionalFormatting: { enabled: false, rules: [] }, kpiTarget: { enabled: false, value: 0, label: "Target", direction: "higher_is_better" }, referenceLines: [], barMode: "grouped", barOrientation: "vertical", categorySort: "source", categoryLimit: null, groupRemainingCategories: false, legendPosition: "top", dataLabelContent: "auto", dataLabelPosition: "auto", axes: { left: { title: "", maximum: null }, right: { title: "", maximum: null } } };

export function resolveDashboardChartAppearance(value?: Partial<DashboardChartAppearance> | null): DashboardChartAppearance {
  return { ...defaultDashboardChartAppearance, ...value, conditionalFormatting: { ...defaultDashboardChartAppearance.conditionalFormatting, ...value?.conditionalFormatting, rules: value?.conditionalFormatting?.rules?.map((rule) => ({ ...rule })) ?? [] }, kpiTarget: { ...defaultDashboardChartAppearance.kpiTarget, ...value?.kpiTarget }, referenceLines: value?.referenceLines?.map((line) => ({ ...line })) ?? [], axes: { left: { ...defaultDashboardChartAppearance.axes.left, ...value?.axes?.left }, right: { ...defaultDashboardChartAppearance.axes.right, ...value?.axes?.right } } };
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

export function isDashboardBarModeValid(barMode: DashboardBarMode | string, widgetType: ChartWidgetType, series: Array<Pick<DashboardChartSeriesDefinition, "displayType" | "axis">>, referenceLines: DashboardReferenceLine[]): boolean {
  if (!barModes.has(barMode)) return false;
  if (barMode === "grouped") return true;
  return ["bar_chart", "choice_breakdown", "date_trend"].includes(widgetType)
    && series.length >= 2
    && series.every((item) => item.displayType === "bar")
    && new Set(series.map((item) => item.axis)).size === 1
    && (barMode !== "stacked_percent" || referenceLines.length === 0);
}

export function isDashboardBarOrientationValid(orientation: DashboardBarOrientation | string, widgetType: ChartWidgetType, series: Array<Pick<DashboardChartSeriesDefinition, "displayType" | "axis">>): boolean {
  if (!barOrientations.has(orientation)) return false;
  if (orientation === "vertical") return true;
  return ["bar_chart", "choice_breakdown"].includes(widgetType)
    && series.length > 0
    && series.every((item) => item.displayType === "bar")
    && new Set(series.map((item) => item.axis)).size === 1;
}

export function isDashboardCategorySortValid(sort: DashboardCategorySort | string, widgetType: ChartWidgetType): boolean {
  return categorySorts.has(sort) && (sort === "source" || ["bar_chart", "choice_breakdown"].includes(widgetType));
}

export function normalizeDashboardCategoryLimit(limit: number | null, groupRemaining: boolean, widgetType: ChartWidgetType, series: Array<Pick<DashboardChartSeriesDefinition, "metric">> = []): { limit: number | null; groupRemaining: boolean } {
  const breakdown = ["bar_chart", "choice_breakdown"].includes(widgetType);
  const normalizedLimit = breakdown && Number.isInteger(limit) && limit !== null && limit >= 1 && limit <= 12 ? limit : null;
  return { limit: normalizedLimit, groupRemaining: normalizedLimit !== null && Boolean(groupRemaining) && series.every((item) => item.metric.type !== "average") };
}

export function isDashboardCategoryLimitValid(limit: number | null, groupRemaining: boolean, widgetType: ChartWidgetType, series: Array<Pick<DashboardChartSeriesDefinition, "metric">> = []): boolean {
  const normalized = normalizeDashboardCategoryLimit(limit, groupRemaining, widgetType, series);
  return normalized.limit === limit && normalized.groupRemaining === groupRemaining;
}

export function isDashboardLegendPositionValid(position: DashboardLegendPosition | string, widgetType: ChartWidgetType): boolean {
  return legendPositions.has(position) && (position === "top" || ["bar_chart", "choice_breakdown", "date_trend"].includes(widgetType));
}

export function normalizeDashboardDataLabelSettings(content: DashboardDataLabelContent | string, position: DashboardDataLabelPosition | string, widgetType: ChartWidgetType, series: Array<Pick<DashboardChartSeriesDefinition, "displayType">>, barMode: DashboardBarMode): { content: DashboardDataLabelContent; position: DashboardDataLabelPosition } {
  const chart = ["bar_chart", "choice_breakdown", "date_trend"].includes(widgetType);
  const circular = series.some((item) => isDashboardCircularDisplayType(item.displayType));
  const percentageSupported = circular || barMode === "stacked_percent";
  return {
    content: dataLabelContents.has(content) && chart && (!percentageContents.has(content) || percentageSupported) ? content as DashboardDataLabelContent : "auto",
    position: dataLabelPositions.has(position) && chart && !circular ? position as DashboardDataLabelPosition : "auto"
  };
}

export function isDashboardDataLabelSettingsValid(content: DashboardDataLabelContent | string, position: DashboardDataLabelPosition | string, widgetType: ChartWidgetType, series: Array<Pick<DashboardChartSeriesDefinition, "displayType">>, barMode: DashboardBarMode): boolean {
  const normalized = normalizeDashboardDataLabelSettings(content, position, widgetType, series, barMode);
  return normalized.content === content && normalized.position === position;
}

export function normalizeDashboardChartAxes(axesInput: DashboardChartAxes, widgetType: ChartWidgetType, series: Array<Pick<DashboardChartSeriesDefinition, "displayType" | "axis">>, referenceLines: DashboardReferenceLine[], barMode: DashboardBarMode): DashboardChartAxes {
  const axes = { left: { ...axesInput.left }, right: { ...axesInput.right } };
  const cartesian = ["bar_chart", "choice_breakdown", "date_trend"].includes(widgetType) && !series.some((item) => isDashboardCircularDisplayType(item.displayType));
  if (!cartesian) return { left: { ...defaultDashboardChartAppearance.axes.left }, right: { ...defaultDashboardChartAppearance.axes.right } };
  if (series.length > 0 && !series.some((item) => item.axis === "left") && !referenceLines.some((line) => line.axis === "left")) axes.left = { ...defaultDashboardChartAppearance.axes.left };
  if (!series.some((item) => item.axis === "right") && !referenceLines.some((line) => line.axis === "right")) axes.right = { ...defaultDashboardChartAppearance.axes.right };
  if (barMode === "stacked_percent") { axes.left.maximum = null; axes.right.maximum = null; }
  return axes;
}

export function isDashboardChartAxesValid(axes: DashboardChartAxes, widgetType: ChartWidgetType, series: Array<Pick<DashboardChartSeriesDefinition, "displayType" | "axis">>, referenceLines: DashboardReferenceLine[], barMode: DashboardBarMode): boolean {
  const normalized = normalizeDashboardChartAxes(axes, widgetType, series, referenceLines, barMode);
  if (JSON.stringify(normalized) !== JSON.stringify(axes)) return false;
  return [axes.left, axes.right].every((axis) => axis.title.length <= 80 && (axis.maximum === null || Number.isFinite(axis.maximum) && axis.maximum > 0 && axis.maximum <= 1_000_000_000_000_000));
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
const barModes = new Set(["grouped", "stacked", "stacked_percent"]);
const barOrientations = new Set(["vertical", "horizontal"]);
const categorySorts = new Set(["source", "value_desc", "value_asc", "label_asc", "label_desc"]);
const legendPositions = new Set(["top", "bottom", "left", "right"]);
const dataLabelContents = new Set(["auto", "value", "percentage", "value_and_percentage"]);
const percentageContents = new Set(["percentage", "value_and_percentage"]);
const dataLabelPositions = new Set(["auto", "inside", "outside"]);
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
  if (appearance.numberFormat === "auto" && appearance.displayUnit === "none") return new Intl.NumberFormat(locale).format(value);
  const options: Intl.NumberFormatOptions = { minimumFractionDigits: appearance.decimalPlaces, maximumFractionDigits: appearance.decimalPlaces };
  if (appearance.numberFormat === "currency") { options.style = "currency"; options.currency = appearance.currencyCode.toUpperCase(); }
  if (appearance.displayUnit === "auto") options.notation = "compact";
  const fixedUnit = appearance.displayUnit === "thousands" ? { divisor: 1_000, suffix: "K" } : appearance.displayUnit === "millions" ? { divisor: 1_000_000, suffix: "M" } : appearance.displayUnit === "billions" ? { divisor: 1_000_000_000, suffix: "B" } : null;
  const formatted = new Intl.NumberFormat(locale, options).format(fixedUnit ? value / fixedUnit.divisor : value);
  return `${formatted}${fixedUnit?.suffix ?? ""}${appearance.numberFormat === "percent" ? "%" : ""}`;
}

export function formatDashboardCount(value: number, appearance: DashboardChartAppearance, locale: string): string {
  return formatDashboardValue(value, { ...appearance, numberFormat: "number" }, locale);
}

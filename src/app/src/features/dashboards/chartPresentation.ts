import type { ChartSeriesPoint, ChartWidgetType, DashboardBarMode, DashboardCategorySort, DashboardChartSeriesDefinition, DashboardReferenceLine, DashboardSeriesAxis, DashboardSeriesDisplayType } from "./types";

const circularDisplayTypes = new Set<DashboardSeriesDisplayType>(["pie", "donut"]);

export function isDashboardCircularDisplayType(value: DashboardSeriesDisplayType | string): value is "pie" | "donut" {
  return circularDisplayTypes.has(value as DashboardSeriesDisplayType);
}

export function isDashboardSeriesPresentationValid(widgetType: ChartWidgetType, series: DashboardChartSeriesDefinition[]): boolean {
  const circularSeries = series.filter((item) => isDashboardCircularDisplayType(item.displayType));
  return circularSeries.length === 0 || widgetType === "choice_breakdown" && series.length === 1;
}

export type DashboardCircularSegment = { point: ChartSeriesPoint; ratio: number; offset: number };

export function getDashboardCircularSegments(points: ChartSeriesPoint[]): DashboardCircularSegment[] {
  if (points.some((point) => point.value < 0)) return [];
  const positive = points.filter((point) => point.value > 0);
  const total = positive.reduce((sum, point) => sum + point.value, 0);
  if (total <= 0) return [];
  let offset = 0;
  return positive.map((point) => {
    const ratio = point.value / total;
    const segment = { point, ratio, offset };
    offset += ratio;
    return segment;
  });
}

export function getDashboardAxisMaximum(series: Array<DashboardChartSeriesDefinition & { points: ChartSeriesPoint[] }>, referenceLines: DashboardReferenceLine[], axis: DashboardSeriesAxis, barMode: DashboardBarMode = "grouped"): number {
  const values = series.filter((item) => item.axis === axis).flatMap((item) => item.points.map((point) => Math.max(0, point.value)));
  const references = referenceLines.filter((line) => line.axis === axis).map((line) => line.value);
  if (barMode === "stacked_percent" && series.some((item) => item.axis === axis)) return 100;
  if (barMode === "stacked") {
    const keys = new Set(series.filter((item) => item.axis === axis).flatMap((item) => item.points.map((point) => point.key)));
    const totals = [...keys].map((key) => series.filter((item) => item.axis === axis).reduce((sum, item) => sum + Math.max(0, item.points.find((point) => point.key === key)?.value ?? 0), 0));
    return Math.max(1, ...totals, ...references);
  }
  return Math.max(1, ...values, ...references);
}

export function resolveDashboardAxisMaximum(automaticMaximum: number, configuredMaximum: number | null): number {
  return configuredMaximum !== null && Number.isFinite(configuredMaximum) && configuredMaximum > 0 ? configuredMaximum : automaticMaximum;
}

export function isDashboardAxisClipped(automaticMaximum: number, configuredMaximum: number | null): boolean {
  return configuredMaximum !== null && automaticMaximum > configuredMaximum;
}

export type DashboardStackedBarSegment = { start: number; end: number; percentage: number; total: number; value: number };

export function getDashboardStackedBarSegment(series: Array<DashboardChartSeriesDefinition & { points: ChartSeriesPoint[] }>, key: string, seriesIndex: number, barMode: Exclude<DashboardBarMode, "grouped">): DashboardStackedBarSegment {
  const values = series.map((item) => Math.max(0, item.points.find((point) => point.key === key)?.value ?? 0));
  const value = values[seriesIndex] ?? 0;
  const total = values.reduce((sum, item) => sum + item, 0);
  const startValue = values.slice(0, seriesIndex).reduce((sum, item) => sum + item, 0);
  const percentage = total > 0 ? value / total * 100 : 0;
  return barMode === "stacked_percent"
    ? { start: total > 0 ? startValue / total * 100 : 0, end: total > 0 ? (startValue + value) / total * 100 : 0, percentage, total, value }
    : { start: startValue, end: startValue + value, percentage, total, value };
}

export function hasDashboardNegativeSeriesValues(series: Array<{ points: ChartSeriesPoint[] }>): boolean {
  return series.some((item) => item.points.some((point) => point.value < 0));
}

export function getDashboardOrderedCategoryKeys(series: Array<{ points: ChartSeriesPoint[] }>, sort: DashboardCategorySort): string[] {
  const keys = [...new Set(series.flatMap((item) => item.points.map((point) => point.key)))];
  if (sort === "source") return keys;
  const label = (key: string) => series.flatMap((item) => item.points).find((point) => point.key === key)?.label ?? key;
  const value = (key: string) => series.reduce((sum, item) => sum + (item.points.find((point) => point.key === key)?.value ?? 0), 0);
  return [...keys].sort((left, right) => {
    if (sort === "label_asc" || sort === "label_desc") {
      const comparison = label(left).localeCompare(label(right), undefined, { sensitivity: "base" });
      return sort === "label_asc" ? comparison : -comparison;
    }
    const comparison = value(left) - value(right);
    return sort === "value_asc" ? comparison : -comparison;
  });
}

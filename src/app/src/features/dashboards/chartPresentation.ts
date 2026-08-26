import type { ChartSeriesPoint, ChartWidgetType, DashboardChartSeriesDefinition, DashboardSeriesDisplayType } from "./types";

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

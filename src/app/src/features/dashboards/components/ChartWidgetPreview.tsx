import { EmptyState } from "../../../components/ui/EmptyState";
import { Table, type TableColumn } from "../../../components/ui/Table";
import { useLocalization } from "../../../context/LocalizationContext";
import { formatDashboardCount, formatDashboardValue, getDashboardAccentColor, getDashboardConditionalResult, getDashboardEffectiveCardAccent, getDashboardKpiTargetSummary, getDashboardSeriesColor, getDashboardTooltipText, resolveDashboardChartAppearance } from "../appearance";
import { getDashboardAxisMaximum, getDashboardCircularSegments, getDashboardDataLabelText, getDashboardPresentedSeries, getDashboardStackedBarSegment, hasDashboardNegativeSeriesValues, isDashboardAxisClipped, isDashboardCircularDisplayType, resolveDashboardAxisMaximum } from "../chartPresentation";
import type { ChartTableRow, ChartWidgetPreview as ChartWidgetPreviewData, DashboardAnalyticsResponse, DashboardChartAppearance, DashboardLegendPosition, DashboardSeriesColor } from "../types";
import type { DashboardPointSelection } from "../drillThrough";

type WidgetPreviewData = ChartWidgetPreviewData | DashboardAnalyticsResponse;

export function ChartWidgetPreview({ appearance: appearanceInput, interactionLabel = "Open details", onSelect, preview }: { appearance?: DashboardChartAppearance | null; interactionLabel?: string; onSelect?: (selection: DashboardPointSelection) => void; preview: WidgetPreviewData }) {
  const [selectedKey, setSelectedKey] = useState<string | null>(null);
  useEffect(() => setSelectedKey(null), [preview]);
  const select = (selection: DashboardPointSelection) => { setSelectedKey(selection.recordId ?? selection.key); onSelect?.(selection); };
  const { effectiveLocale } = useLocalization();
  const appearance = resolveDashboardChartAppearance(appearanceInput);
  const primaryValue = preview.series[0]?.isMissing ? null : preview.series[0]?.value;
  const conditionalResult = getDashboardConditionalResult(appearance, primaryValue);
  const targetSummary = getDashboardKpiTargetSummary(appearance, primaryValue, effectiveLocale);
  const formatNumber = (value: number) => formatDashboardValue(value, appearance, effectiveLocale);
  const formatCount = (value: number) => formatDashboardCount(value, appearance, effectiveLocale);
  const formatMetric = preview.metric.type === "count" ? formatCount : formatNumber;
  if ("dataSeries" in preview && (preview.dataSeries?.length ?? 0) > 1 && preview.widgetType === "summary") {
    return <MultiSeriesSummary appearance={appearance} comparison={preview.comparison} conditionalResult={conditionalResult} formatCount={formatCount} formatNumber={formatNumber} interactionLabel={interactionLabel} onSelect={onSelect ? select : undefined} selectedKey={selectedKey} series={preview.dataSeries!} targetSummary={targetSummary} />;
  }
  if (shouldRenderConfiguredSeriesChart(preview)) {
    return <MultiSeriesChart appearance={appearance} formatCount={formatCount} formatNumber={formatNumber} interactionLabel={interactionLabel} onSelect={onSelect ? select : undefined} selectedKey={selectedKey} series={preview.dataSeries!} />;
  }
  if (preview.widgetType === "table") {
    return <ChartTable interactionLabel={interactionLabel} onSelect={onSelect ? select : undefined} preview={preview} selectedKey={selectedKey} />;
  }

  if (preview.widgetType === "number_card" || preview.widgetType === "summary") {
    const point = preview.series[0];
    const missing = Boolean(point?.isMissing);
    const formatted = missing ? "No data" : formatNumber(point?.value ?? 0);
    const accent = getDashboardAccentColor(getDashboardEffectiveCardAccent(appearance, missing ? null : point?.value), appearance.palette);

    return (
      <button aria-label={onSelect && !missing ? `${interactionLabel}: ${point?.label ?? "Records"}, ${formatted}` : undefined} className={`w-full rounded-lg border border-border bg-muted/30 p-4 text-left transition ${onSelect && !missing ? "cursor-pointer hover:border-primary hover:bg-primary/5 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary" : "cursor-default"} ${selectedKey === (point?.key ?? "summary") ? "ring-2 ring-primary" : ""}`} disabled={!onSelect || missing} onClick={() => select({ key: point?.key ?? "summary", label: point?.label ?? "Records", value: point?.value ?? 0 })} style={accent ? { borderLeftColor: accent, borderLeftWidth: 5 } : undefined} title={appearance.showTooltips ? `${point?.label ?? "Records"}: ${formatted}` : undefined} type="button">
        <p className="break-words text-sm font-bold text-muted-foreground">{point?.label ?? "Records"}</p>
        <p className="mt-2 break-words text-3xl font-bold text-foreground tabular-nums">{formatted}</p>
        <ConditionalStatus accent={accent} label={conditionalResult?.label} />
        <KpiTargetSummary value={targetSummary} />
        <KpiComparisonSummary formatValue={formatMetric} value={"comparison" in preview ? preview.comparison : null} />
      </button>
    );
  }

  return <SeriesBars appearance={appearance} formatNumber={formatNumber} interactionLabel={interactionLabel} onSelect={onSelect ? select : undefined} points={preview.series} selectedKey={selectedKey} />;
}

export function shouldRenderConfiguredSeriesChart(preview: WidgetPreviewData): preview is DashboardAnalyticsResponse & { dataSeries: NonNullable<DashboardAnalyticsResponse["dataSeries"]> } {
  return "dataSeries" in preview && preview.widgetType !== "summary" && preview.widgetType !== "table" && Boolean(preview.dataSeries?.some((series) => series.points.length > 0));
}

export function getPresentPointSegments(points: Array<{ isMissing?: boolean } | undefined>): number[][] {
  const segments: number[][] = [];
  for (const [index, point] of points.entries()) {
    if (!point || point.isMissing) continue;
    if (index === 0 || !points[index - 1] || points[index - 1]?.isMissing) segments.push([]);
    segments.at(-1)!.push(index);
  }
  return segments;
}

function MultiSeriesSummary({ appearance, comparison, conditionalResult, series, formatCount, formatNumber, interactionLabel, onSelect, selectedKey, targetSummary }: { appearance: DashboardChartAppearance; comparison?: DashboardAnalyticsResponse["comparison"]; conditionalResult: ReturnType<typeof getDashboardConditionalResult>; series: NonNullable<DashboardAnalyticsResponse["dataSeries"]>; formatCount: (value: number) => string; formatNumber: (value: number) => string; interactionLabel: string; onSelect?: (selection: DashboardPointSelection) => void; selectedKey: string | null; targetSummary: ReturnType<typeof getDashboardKpiTargetSummary> }) {
  const accent = conditionalResult ? getDashboardAccentColor(conditionalResult.accent, appearance.palette) : undefined;
  return <div className="grid gap-3"><ConditionalStatus accent={accent} label={conditionalResult?.label} /><KpiTargetSummary value={targetSummary} /><KpiComparisonSummary formatValue={series[0]?.metric.type === "count" ? formatCount : formatNumber} value={comparison} /><div className="grid gap-3 sm:grid-cols-2">{series.map((item) => { const point = item.points[0]; const missing = Boolean(point?.isMissing); const formatted = missing ? "No data" : (item.metric.type === "count" ? formatCount : formatNumber)(point?.value ?? 0); return <button aria-label={onSelect && !missing ? `${interactionLabel}: ${item.label}, ${formatted}` : undefined} className={`rounded-lg border border-border bg-muted/20 p-4 text-left transition ${onSelect && !missing ? "hover:border-primary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary" : "cursor-default"} ${selectedKey === (point?.key ?? item.id) ? "ring-2 ring-primary" : ""}`} disabled={!onSelect || missing} key={item.id} onClick={() => onSelect?.({ key: point?.key ?? item.id, label: item.label, value: point?.value ?? 0 })} title={appearance.showTooltips ? `${item.label}: ${formatted}` : undefined} type="button"><div className="mb-3 h-1.5 rounded-full" style={{ background: getDashboardSeriesColor(item.color, appearance.palette) }} /><p className="text-xs font-bold text-muted-foreground">{item.label}</p><p className="mt-1 text-2xl font-extrabold tabular-nums">{formatted}</p><p className="mt-1 text-[11px] text-muted-foreground">{item.metric.type}{item.axis === "right" ? " · right axis" : ""}</p></button>;})}</div></div>;
}

function ConditionalStatus({ accent, label }: { accent?: string; label?: string | null }) { return label ? <span aria-label={`KPI status: ${label}`} className="mt-2 inline-flex w-fit items-center gap-2 rounded-full border border-border bg-card px-2.5 py-1 text-xs font-bold"><span className="size-2 rounded-full" style={{ background: accent }} />{label}</span> : null; }

function KpiTargetSummary({ value }: { value: ReturnType<typeof getDashboardKpiTargetSummary> }) {
  if (!value) return null;
  const outcome = value.outcome === "favorable" ? "Favorable" : value.outcome === "on_target" ? "On target" : "Needs attention";
  const tone = value.outcome === "needs_attention" ? "var(--color-warning)" : "var(--color-success)";
  return <div aria-label={`${value.label}: ${value.target}. ${value.variance}. ${outcome}`} className="mt-2 grid gap-1.5 text-xs font-semibold text-muted-foreground">
    <p><span className="text-foreground">{value.label}: {value.target}</span><span aria-hidden="true"> · </span>{value.variance}</p>
    <p className="flex items-center gap-1.5 font-bold" style={{ color: tone }}><span aria-hidden="true" className="size-2 rounded-full" style={{ background: tone }} />{outcome}</p>
    {value.progress !== null ? <div aria-label={`Target progress: ${value.progress.toFixed(1)}%`} aria-valuemax={100} aria-valuemin={0} aria-valuenow={value.progress} className="h-1.5 overflow-hidden rounded-full bg-muted" role="progressbar"><div className="h-full rounded-full" style={{ background: tone, width: `${value.progress}%` }} /></div> : null}
  </div>;
}

function KpiComparisonSummary({ formatValue, value }: { formatValue: (value: number) => string; value?: DashboardAnalyticsResponse["comparison"] }) {
  if (!value) return null;
  const direction = value.direction === "up" ? "Up" : value.direction === "down" ? "Down" : "No change";
  const arrow = value.direction === "up" ? "↑" : value.direction === "down" ? "↓" : "→";
  const change = value.changePercent === null || value.changePercent === undefined ? direction : value.direction === "unchanged" ? "No change" : `${direction} ${Math.abs(value.changePercent).toFixed(1)}%`;
  return <div aria-label={`${change} versus ${value.periodLabel}. Previous value ${formatValue(value.previousValue)}`} className="mt-2 rounded-lg border border-border bg-card px-3 py-2 text-xs"><p className="font-bold text-foreground"><span aria-hidden="true">{arrow} </span>{change} vs {value.periodLabel}</p><p className="mt-0.5 text-muted-foreground">Previous: {formatValue(value.previousValue)}</p></div>;
}

function PositionedChartLayout({ chart, legend, position }: { chart: ReactNode; legend: ReactNode; position: DashboardLegendPosition }) {
  if (!legend) return <div className="min-w-0">{chart}</div>;
  if (position === "left") return <div className="grid min-w-0 items-start gap-4 sm:grid-cols-[minmax(9rem,0.28fr)_minmax(0,1fr)]">{legend}<div className="min-w-0">{chart}</div></div>;
  if (position === "right") return <div className="grid min-w-0 items-start gap-4 sm:grid-cols-[minmax(0,1fr)_minmax(9rem,0.28fr)]"><div className="order-2 min-w-0 sm:order-1">{chart}</div><div className="order-1 min-w-0 sm:order-2">{legend}</div></div>;
  return <div className="grid min-w-0 gap-3">{position === "top" ? legend : null}<div className="min-w-0">{chart}</div>{position === "bottom" ? legend : null}</div>;
}

function SeriesLegend({ appearance, series }: { appearance: DashboardChartAppearance; series: NonNullable<DashboardAnalyticsResponse["dataSeries"]> }) {
  const sidePosition = appearance.legendPosition === "left" || appearance.legendPosition === "right";
  return <div aria-label="Chart legend" className={sidePosition ? "grid max-h-64 min-w-0 gap-2 overflow-y-auto pr-1" : "flex min-w-0 flex-wrap gap-3"} data-legend-position={appearance.legendPosition}>{series.map((item) => <span className="flex min-w-0 items-center gap-1.5 text-xs font-bold" key={item.id} title={item.label}><span className="size-2.5 shrink-0 rounded-full" style={{ background: getDashboardSeriesColor(item.color, appearance.palette) }} /><span className="min-w-0 truncate">{item.label}</span><span className="shrink-0 font-medium text-muted-foreground">({item.axis})</span></span>)}</div>;
}

function MultiSeriesChart({ appearance, series, formatCount, formatNumber, interactionLabel, onSelect, selectedKey }: { appearance: DashboardChartAppearance; series: NonNullable<DashboardAnalyticsResponse["dataSeries"]>; formatCount: (value: number) => string; formatNumber: (value: number) => string; interactionLabel: string; onSelect?: (selection: DashboardPointSelection) => void; selectedKey: string | null }) {
  if (series.every((item) => item.points.length === 0 || item.points.every((point) => point.isMissing))) return <EmptyState title="No chart data" description="The selected source has no usable numeric values for this chart." />;
  const presentation = getDashboardPresentedSeries(series, appearance.categorySort, appearance.categoryLimit, appearance.groupRemainingCategories);
  series = presentation.series;
  const circularSeries = series.length === 1 && isDashboardCircularDisplayType(series[0].displayType) ? series[0] : null;
  if (circularSeries) return <CircularSeriesChart aggregateKey={presentation.aggregateKey} appearance={appearance} formatValue={circularSeries.metric.type === "count" ? formatCount : formatNumber} interactionLabel={interactionLabel} onSelect={onSelect} selectedKey={selectedKey} series={circularSeries} />;
  const stacked = appearance.barMode !== "grouped";
  if (stacked && hasDashboardNegativeSeriesValues(series)) return <EmptyState title="Stacked chart unavailable" description="Stacked bars require non-negative values." />;
  if (appearance.barOrientation === "horizontal") return <HorizontalBarChart aggregateKey={presentation.aggregateKey} appearance={appearance} categoryKeys={presentation.categoryKeys} formatCount={formatCount} formatNumber={formatNumber} interactionLabel={interactionLabel} onSelect={onSelect} selectedKey={selectedKey} series={series} />;
  const keys = presentation.categoryKeys;
  const labels = keys.map((key) => series.flatMap((item) => item.points).find((point) => point.key === key)?.label ?? key);
  const automaticLeftMaximum = getDashboardAxisMaximum(series, appearance.referenceLines, "left", appearance.barMode);
  const automaticRightMaximum = getDashboardAxisMaximum(series, appearance.referenceLines, "right", appearance.barMode);
  const leftMaximum = resolveDashboardAxisMaximum(automaticLeftMaximum, appearance.axes.left.maximum);
  const rightMaximum = resolveDashboardAxisMaximum(automaticRightMaximum, appearance.axes.right.maximum);
  const plot = { left: 52, top: 16, width: 548, height: 170 };
  const x = (index: number) => plot.left + (index + .5) * plot.width / Math.max(keys.length, 1);
  const y = (value: number, axis: "left" | "right") => plot.top + plot.height - Math.min(1, Math.max(0, value) / (axis === "right" ? rightMaximum : leftMaximum)) * plot.height;
  const barSeries = series.filter((item) => item.displayType === "bar");
  const gridlines = appearance.showGridlines ? [0, .25, .5, .75, 1] : [0];
  const axisFormat = (axis: "left" | "right") => appearance.barMode === "stacked_percent" ? formatPercentage : series.find((item) => item.axis === axis)?.metric.type === "count" ? formatCount : formatNumber;
  const hasRightAxis = series.some((item) => item.axis === "right") || appearance.referenceLines.some((line) => line.axis === "right");
  const legend = appearance.showLegend ? <SeriesLegend appearance={appearance} series={series} /> : null;
  const chart = <div className="grid min-w-0 gap-3">
    <div className="max-w-full overflow-x-auto"><svg aria-label="Configured series chart" className="min-w-[38rem]" data-bar-mode={appearance.barMode} data-bar-orientation={appearance.barOrientation} role="img" viewBox="0 0 640 230">
      {gridlines.map((ratio) => <line key={ratio} opacity={ratio === 0 ? 1 : .65} stroke="var(--color-border)" x1={plot.left} x2={plot.left + plot.width} y1={plot.top + plot.height - ratio * plot.height} y2={plot.top + plot.height - ratio * plot.height} />)}
      <text fill="currentColor" fontSize="9" x={plot.left} y="11">{axisFormat("left")(leftMaximum)}</text>
      {hasRightAxis ? <text fill="currentColor" fontSize="9" textAnchor="end" x={plot.left + plot.width} y="11">{axisFormat("right")(rightMaximum)}</text> : null}
      {appearance.axes.left.title.trim() ? <text fill="currentColor" fontSize="10" fontWeight="700" textAnchor="middle" transform="rotate(-90 13 101)" x="13" y="101">{appearance.axes.left.title.trim()}</text> : null}
      {hasRightAxis && appearance.axes.right.title.trim() ? <text fill="currentColor" fontSize="10" fontWeight="700" textAnchor="middle" transform="rotate(90 627 101)" x="627" y="101">{appearance.axes.right.title.trim()}</text> : null}
      {appearance.referenceLines.map((line, index) => {
        const lineY = y(line.value, line.axis);
        const formattedValue = axisFormat(line.axis)(line.value);
        const tooltip = getDashboardTooltipText(appearance.tooltipContent, line.label, "Reference line", formattedValue, null, `${line.label}: ${formattedValue}`);
        return <g aria-label={`Reference line ${line.label}: ${formattedValue}`} data-reference-line={line.id} key={line.id}>
          <line stroke={getDashboardSeriesColor(line.color, appearance.palette)} strokeDasharray={getReferenceLineDasharray(line.style)} strokeWidth="2" x1={plot.left} x2={plot.left + plot.width} y1={lineY} y2={lineY}>{appearance.showTooltips ? <title>{tooltip}</title> : null}</line>
          <text fill={getDashboardSeriesColor(line.color, appearance.palette)} fontSize="9" fontWeight="700" paintOrder="stroke" stroke="var(--color-card)" strokeWidth="3" textAnchor="end" x={plot.left + plot.width - 4} y={Math.max(plot.top + 11 + index * 11, lineY - 4)}>{line.label} · {formattedValue}</text>
        </g>;
      })}
      {series.map((item, seriesIndex) => {
        const pointItems = keys.map((key) => item.points.find((point) => point.key === key));
        const values = pointItems.map((point) => point?.value ?? 0);
        const segments = getPresentPointSegments(pointItems).map((indexes) => indexes.map((index) => `${x(index)},${y(values[index], item.axis)}`).join(" "));
        const color = getDashboardSeriesColor(item.color, appearance.palette);
        const formatter = item.metric.type === "count" ? formatCount : formatNumber;
        const pointLabelY = (value: number) => appearance.dataLabelPosition === "inside" ? Math.min(plot.top + plot.height - 4, y(value, item.axis) + 12) : Math.max(28 + seriesIndex * 12, y(value, item.axis) - 5);
        const labelProps = { "data-data-label-content": appearance.dataLabelContent, "data-data-label-position": appearance.dataLabelPosition, fill: "currentColor", fontSize: 9, paintOrder: "stroke" as const, stroke: "var(--color-card)", strokeWidth: 3, textAnchor: "middle" as const };
        if (item.displayType === "bar") {
          if (stacked) {
            const width = Math.min(42, plot.width / Math.max(keys.length, 1) * .64);
            return <g key={item.id}>{values.map((value, index) => {
              if (pointItems[index]?.isMissing) return null;
              const segment = getDashboardStackedBarSegment(series, keys[index], seriesIndex, appearance.barMode === "stacked_percent" ? "stacked_percent" : "stacked");
              const top = y(segment.end, item.axis);
              const bottom = y(segment.start, item.axis);
              const label = getDashboardDataLabelText(appearance.dataLabelContent, value, appearance.barMode === "stacked_percent" ? segment.percentage : null, formatter);
              const formatted = formatter(value);
              const percentage = appearance.barMode === "stacked_percent" ? formatPercentage(segment.percentage) : null;
              const tooltip = getDashboardTooltipText(appearance.tooltipContent, labels[index], item.label, formatted, percentage, percentage ? `${item.label}: ${formatted} (${percentage})` : `${item.label}: ${formatted}`);
              const inside = appearance.dataLabelPosition === "inside" || appearance.dataLabelPosition === "auto";
              const combined = appearance.dataLabelContent === "value_and_percentage" && appearance.barMode === "stacked_percent";
              return <g key={keys[index]}><rect data-series-id={item.id} data-tooltip-target="mark" fill={color} height={Math.max(0, bottom - top)} rx="2" width={width} x={x(index) - width / 2} y={top}>{appearance.showTooltips ? <title>{tooltip}</title> : null}</rect>{appearance.showDataLabels && segment.end > segment.start ? <text {...labelProps} dominantBaseline={inside ? "middle" : undefined} fontSize={combined ? 8 : labelProps.fontSize} x={x(index)} y={inside ? (top + bottom) / 2 : Math.max(28 + seriesIndex * 12, top - 5)}>{combined ? <><tspan x={x(index)} dy="-0.45em">{formatter(value)}</tspan><tspan x={x(index)} dy="1.1em">{formatPercentage(segment.percentage)}</tspan></> : label}</text> : null}</g>;
            })}</g>;
          }
          const barIndex = barSeries.findIndex((seriesItem) => seriesItem.id === item.id);
          const width = Math.min(28, plot.width / Math.max(keys.length, 1) / Math.max(barSeries.length + 1, 2));
          return <g key={item.id}>{values.map((value, index) => { if (pointItems[index]?.isMissing) return null; const top = y(value, item.axis); const bottom = plot.top + plot.height; const inside = appearance.dataLabelPosition === "inside"; const formatted = formatter(value); const tooltip = getDashboardTooltipText(appearance.tooltipContent, labels[index], item.label, formatted, null, `${item.label}: ${formatted}`); return <g key={keys[index]}><rect data-tooltip-target="mark" fill={color} height={bottom - top} rx="2" width={width} x={x(index) - barSeries.length * width / 2 + barIndex * width} y={top}>{appearance.showTooltips ? <title>{tooltip}</title> : null}</rect>{appearance.showDataLabels ? <text {...labelProps} dominantBaseline={inside ? "middle" : undefined} x={x(index) - barSeries.length * width / 2 + barIndex * width + width / 2} y={inside ? (top + bottom) / 2 : pointLabelY(value)}>{getDashboardDataLabelText(appearance.dataLabelContent, value, null, formatter)}</text> : null}</g>; })}</g>;
        }
        const tooltipPoints = values.map((value, index) => { if (pointItems[index]?.isMissing) return null; const formatted = formatter(value); const tooltip = getDashboardTooltipText(appearance.tooltipContent, labels[index], item.label, formatted, null, `${item.label}: ${formatted}`); return <circle cx={x(index)} cy={y(value, item.axis)} data-tooltip-target="mark" fill={color} key={keys[index]} r="4">{appearance.showTooltips ? <title>{tooltip}</title> : null}</circle>; });
        const dataLabels = appearance.showDataLabels ? values.map((value, index) => pointItems[index]?.isMissing ? null : <text {...labelProps} key={keys[index]} x={x(index)} y={pointLabelY(value)}>{getDashboardDataLabelText(appearance.dataLabelContent, value, null, formatter)}</text>) : null;
        if (item.displayType === "area") return <g data-gap-count={pointItems.filter((point) => point?.isMissing).length} key={item.id}>{getPresentPointSegments(pointItems).map((indexes, segmentIndex) => { const points = segments[segmentIndex]; return <g key={segmentIndex}><polygon fill={color} opacity=".18" points={`${x(indexes[0])},${plot.top + plot.height} ${points} ${x(indexes[indexes.length - 1])},${plot.top + plot.height}`} /><polyline fill="none" points={points} stroke={color} strokeWidth="3" /></g>; })}{tooltipPoints}{dataLabels}</g>;
        return <g data-gap-count={pointItems.filter((point) => point?.isMissing).length} key={item.id}>{segments.map((points, index) => <polyline fill="none" key={index} points={points} stroke={color} strokeLinecap="round" strokeLinejoin="round" strokeWidth="3" />)}{tooltipPoints}{dataLabels}</g>;
      })}
      {labels.map((label, index) => <text data-aggregate-category={keys[index] === presentation.aggregateKey ? "true" : undefined} data-category-key={keys[index]} fill="currentColor" fontSize="10" key={keys[index]} textAnchor="middle" x={x(index)} y="207">{label.slice(0, 10)}</text>)}
      {onSelect ? keys.map((key, index) => {
        const point = series.flatMap((item) => item.points).find((item) => item.key === key && !item.isMissing);
        if (!point) return null;
        const width = plot.width / Math.max(keys.length, 1);
        const total = series.reduce((sum, item) => sum + Math.max(0, item.points.find((candidate) => candidate.key === key)?.value ?? 0), 0);
        const tooltip = series.map((item) => {
          const seriesPoint = item.points.find((candidate) => candidate.key === key);
          const value = seriesPoint?.value ?? 0;
          const formatted = seriesPoint?.isMissing ? "No data" : (item.metric.type === "count" ? formatCount : formatNumber)(value);
          const percentage = appearance.barMode === "stacked_percent" && !seriesPoint?.isMissing ? formatPercentage(total > 0 ? Math.max(0, value) / total * 100 : 0) : null;
          return getDashboardTooltipText(appearance.tooltipContent, labels[index], item.label, formatted, percentage, percentage ? `${item.label}: ${formatted} (${percentage})` : `${item.label}: ${formatted}`);
        }).join(" · ");
        const accessibleText = series.map((item) => { const seriesPoint = item.points.find((candidate) => candidate.key === key); const value = seriesPoint?.value ?? 0; const formatted = seriesPoint?.isMissing ? "No data" : (item.metric.type === "count" ? formatCount : formatNumber)(value); const percentage = appearance.barMode === "stacked_percent" && !seriesPoint?.isMissing ? ` (${formatPercentage(total > 0 ? Math.max(0, value) / total * 100 : 0)})` : ""; return `${item.label}: ${formatted}${percentage}`; }).join(" · ");
        const selection = { key, label: labels[index], value: point?.value ?? 0, aggregate: key === presentation.aggregateKey };
        return <rect aria-label={`${interactionLabel}: ${labels[index]}. ${accessibleText}`} fill="transparent" height={plot.height + 25} key={`interaction-${key}`} onClick={() => onSelect(selection)} onKeyDown={(event) => { if (event.key === "Enter" || event.key === " ") { event.preventDefault(); onSelect(selection); } }} role="button" stroke={selectedKey === key ? "var(--color-primary)" : "transparent"} strokeWidth="2" tabIndex={0} width={width} x={plot.left + index * width} y={plot.top}>{appearance.showTooltips ? <title>{tooltip}</title> : null}</rect>;
      }) : null}
    </svg></div>
    <AxisClippingWarning axes={[...(isDashboardAxisClipped(automaticLeftMaximum, appearance.axes.left.maximum) ? ["left"] : []), ...(hasRightAxis && isDashboardAxisClipped(automaticRightMaximum, appearance.axes.right.maximum) ? ["right"] : [])]} />
  </div>;
  return <PositionedChartLayout chart={chart} legend={legend} position={appearance.legendPosition} />;
}

function HorizontalBarChart({ aggregateKey, appearance, categoryKeys: keys, series, formatCount, formatNumber, interactionLabel, onSelect, selectedKey }: { aggregateKey: string | null; appearance: DashboardChartAppearance; categoryKeys: string[]; series: NonNullable<DashboardAnalyticsResponse["dataSeries"]>; formatCount: (value: number) => string; formatNumber: (value: number) => string; interactionLabel: string; onSelect?: (selection: DashboardPointSelection) => void; selectedKey: string | null }) {
  const labels = keys.map((key) => series.flatMap((item) => item.points).find((point) => point.key === key)?.label ?? key);
  const axis = series[0]?.axis ?? "left";
  const automaticMaximum = getDashboardAxisMaximum(series, appearance.referenceLines, axis, appearance.barMode);
  const maximum = resolveDashboardAxisMaximum(automaticMaximum, appearance.axes[axis].maximum);
  const stacked = appearance.barMode !== "grouped";
  const plot = { left: 118, top: 34, width: 492, height: Math.max(170, keys.length * 32) };
  const chartHeight = plot.top + plot.height + 26;
  const rowHeight = plot.height / Math.max(keys.length, 1);
  const x = (value: number) => plot.left + Math.min(1, Math.max(0, value) / maximum) * plot.width;
  const y = (index: number) => plot.top + (index + .5) * rowHeight;
  const gridlines = appearance.showGridlines ? [0, .25, .5, .75, 1] : [0];
  const axisFormat = appearance.barMode === "stacked_percent" ? formatPercentage : series[0]?.metric.type === "count" ? formatCount : formatNumber;
  const labelProps = { "data-data-label-content": appearance.dataLabelContent, "data-data-label-position": appearance.dataLabelPosition, fill: "currentColor", fontSize: 9, paintOrder: "stroke" as const, stroke: "var(--color-card)", strokeWidth: 3 };
  const legend = appearance.showLegend ? <SeriesLegend appearance={appearance} series={series} /> : null;
  const chart = <div className="grid min-w-0 gap-3">
    <div className="max-w-full overflow-x-auto"><svg aria-label="Configured series chart" className="min-w-[38rem]" data-bar-mode={appearance.barMode} data-bar-orientation="horizontal" role="img" viewBox={`0 0 640 ${chartHeight}`}>
      {gridlines.map((ratio) => <g key={ratio}><line opacity={ratio === 0 ? 1 : .65} stroke="var(--color-border)" x1={plot.left + ratio * plot.width} x2={plot.left + ratio * plot.width} y1={plot.top} y2={plot.top + plot.height} />{ratio === 0 || ratio === 1 ? <text fill="currentColor" fontSize="9" textAnchor={ratio === 0 ? "start" : "end"} x={plot.left + ratio * plot.width} y="13">{axisFormat(ratio * maximum)}</text> : null}</g>)}
      {appearance.axes[axis].title.trim() ? <text fill="currentColor" fontSize="10" fontWeight="700" textAnchor="middle" x={plot.left + plot.width / 2} y="27">{appearance.axes[axis].title.trim()}</text> : null}
      {appearance.referenceLines.filter((line) => line.axis === axis).map((line, index) => {
        const lineX = x(line.value);
        const formattedValue = axisFormat(line.value);
        const tooltip = getDashboardTooltipText(appearance.tooltipContent, line.label, "Reference line", formattedValue, null, `${line.label}: ${formattedValue}`);
        return <g aria-label={`Reference line ${line.label}: ${formattedValue}`} data-reference-line={line.id} key={line.id}><line stroke={getDashboardSeriesColor(line.color, appearance.palette)} strokeDasharray={getReferenceLineDasharray(line.style)} strokeWidth="2" x1={lineX} x2={lineX} y1={plot.top} y2={plot.top + plot.height}>{appearance.showTooltips ? <title>{tooltip}</title> : null}</line><text fill={getDashboardSeriesColor(line.color, appearance.palette)} fontSize="9" fontWeight="700" paintOrder="stroke" stroke="var(--color-card)" strokeWidth="3" textAnchor="end" transform={`rotate(-90 ${lineX - 4} ${plot.top + 8 + index * 11})`} x={lineX - 4} y={plot.top + 8 + index * 11}>{line.label} · {formattedValue}</text></g>;
      })}
      {labels.map((label, index) => <text data-aggregate-category={keys[index] === aggregateKey ? "true" : undefined} data-category-key={keys[index]} dominantBaseline="middle" fill="currentColor" fontSize="10" key={keys[index]} textAnchor="end" x={plot.left - 8} y={y(index)}>{label.slice(0, 17)}</text>)}
      {series.map((item, seriesIndex) => {
        const color = getDashboardSeriesColor(item.color, appearance.palette);
        const formatter = item.metric.type === "count" ? formatCount : formatNumber;
        if (stacked) return <g key={item.id}>{keys.map((key, index) => {
          const segment = getDashboardStackedBarSegment(series, key, seriesIndex, appearance.barMode === "stacked_percent" ? "stacked_percent" : "stacked");
          const start = x(segment.start);
          const end = x(segment.end);
          const label = getDashboardDataLabelText(appearance.dataLabelContent, segment.value, appearance.barMode === "stacked_percent" ? segment.percentage : null, formatter);
          const formatted = formatter(segment.value);
          const percentage = appearance.barMode === "stacked_percent" ? formatPercentage(segment.percentage) : null;
          const tooltip = getDashboardTooltipText(appearance.tooltipContent, labels[index], item.label, formatted, percentage, percentage ? `${item.label}: ${formatted} (${percentage})` : `${item.label}: ${formatted}`);
          const height = Math.min(22, rowHeight * .68);
          const inside = appearance.dataLabelPosition === "inside" || appearance.dataLabelPosition === "auto";
          const outsideX = Math.min(end + 4, plot.left + plot.width - 2);
          const combined = appearance.dataLabelContent === "value_and_percentage" && appearance.barMode === "stacked_percent";
          const labelX = inside ? (start + end) / 2 : outsideX;
          return <g key={key}><rect data-series-id={item.id} data-tooltip-target="mark" fill={color} height={height} rx="2" width={Math.max(0, end - start)} x={start} y={y(index) - height / 2}>{appearance.showTooltips ? <title>{tooltip}</title> : null}</rect>{appearance.showDataLabels && end > start ? <text {...labelProps} dominantBaseline="middle" fontSize={combined ? 8 : labelProps.fontSize} textAnchor={inside ? "middle" : outsideX >= plot.left + plot.width - 2 ? "end" : "start"} x={labelX} y={y(index)}>{combined ? <><tspan x={labelX} dy="-0.45em">{formatter(segment.value)}</tspan><tspan x={labelX} dy="1.1em">{formatPercentage(segment.percentage)}</tspan></> : label}</text> : null}</g>;
        })}</g>;
        const height = Math.min(13, rowHeight / Math.max(series.length + 1, 2));
        return <g key={item.id}>{keys.map((key, index) => {
          const value = item.points.find((point) => point.key === key)?.value ?? 0;
          const end = x(value);
          const barY = y(index) - series.length * height / 2 + seriesIndex * height;
          const inside = appearance.dataLabelPosition === "inside";
          const labelX = inside ? (plot.left + end) / 2 : Math.min(end + 4, plot.left + plot.width - 2);
          const formatted = formatter(value);
          const tooltip = getDashboardTooltipText(appearance.tooltipContent, labels[index], item.label, formatted, null, `${item.label}: ${formatted}`);
          return <g key={key}><rect data-series-id={item.id} data-tooltip-target="mark" fill={color} height={height} rx="2" width={Math.max(0, end - plot.left)} x={plot.left} y={barY}>{appearance.showTooltips ? <title>{tooltip}</title> : null}</rect>{appearance.showDataLabels ? <text {...labelProps} dominantBaseline="middle" textAnchor={inside ? "middle" : labelX >= plot.left + plot.width - 2 ? "end" : "start"} x={labelX} y={barY + height / 2}>{getDashboardDataLabelText(appearance.dataLabelContent, value, null, formatter)}</text> : null}</g>;
        })}</g>;
      })}
      {onSelect ? keys.map((key, index) => {
        const point = series.flatMap((item) => item.points).find((item) => item.key === key);
        const total = series.reduce((sum, item) => sum + Math.max(0, item.points.find((candidate) => candidate.key === key)?.value ?? 0), 0);
        const tooltip = series.map((item) => { const value = item.points.find((candidate) => candidate.key === key)?.value ?? 0; const formatted = (item.metric.type === "count" ? formatCount : formatNumber)(value); const percentage = appearance.barMode === "stacked_percent" ? formatPercentage(total > 0 ? Math.max(0, value) / total * 100 : 0) : null; return getDashboardTooltipText(appearance.tooltipContent, labels[index], item.label, formatted, percentage, percentage ? `${item.label}: ${formatted} (${percentage})` : `${item.label}: ${formatted}`); }).join(" · ");
        const accessibleText = series.map((item) => { const value = item.points.find((candidate) => candidate.key === key)?.value ?? 0; const formatted = (item.metric.type === "count" ? formatCount : formatNumber)(value); const percentage = appearance.barMode === "stacked_percent" ? ` (${formatPercentage(total > 0 ? Math.max(0, value) / total * 100 : 0)})` : ""; return `${item.label}: ${formatted}${percentage}`; }).join(" · ");
        const selection = { key, label: labels[index], value: point?.value ?? 0, aggregate: key === aggregateKey };
        return <rect aria-label={`${interactionLabel}: ${labels[index]}. ${accessibleText}`} fill="transparent" height={rowHeight} key={`interaction-${key}`} onClick={() => onSelect(selection)} onKeyDown={(event) => { if (event.key === "Enter" || event.key === " ") { event.preventDefault(); onSelect(selection); } }} role="button" stroke={selectedKey === key ? "var(--color-primary)" : "transparent"} strokeWidth="2" tabIndex={0} width={plot.width} x={plot.left} y={plot.top + index * rowHeight}>{appearance.showTooltips ? <title>{tooltip}</title> : null}</rect>;
      }) : null}
    </svg></div>
    <AxisClippingWarning axes={isDashboardAxisClipped(automaticMaximum, appearance.axes[axis].maximum) ? [axis] : []} />
  </div>;
  return <PositionedChartLayout chart={chart} legend={legend} position={appearance.legendPosition} />;
}

function AxisClippingWarning({ axes }: { axes: string[] }) {
  if (axes.length === 0) return null;
  return <p className="rounded-lg border border-warning/40 bg-warning/10 px-3 py-2 text-xs font-semibold text-warning" role="status">Manual {axes.join(" and ")} axis maximum clips values above the configured scale.</p>;
}

function formatPercentage(value: number): string { return `${Number.isInteger(value) ? value.toFixed(0) : value.toFixed(1)}%`; }

function getReferenceLineDasharray(style: DashboardChartAppearance["referenceLines"][number]["style"]): string | undefined {
  return style === "dashed" ? "8 5" : style === "dotted" ? "2 4" : undefined;
}

const circularColorOrder: DashboardSeriesColor[] = ["primary", "info", "success", "warning", "danger", "violet"];

function CircularSeriesChart({ aggregateKey, appearance, formatValue, interactionLabel, onSelect, selectedKey, series }: { aggregateKey: string | null; appearance: DashboardChartAppearance; formatValue: (value: number) => string; interactionLabel: string; onSelect?: (selection: DashboardPointSelection) => void; selectedKey: string | null; series: NonNullable<DashboardAnalyticsResponse["dataSeries"]>[number] }) {
  const orderedPoints = series.points;
  const segments = getDashboardCircularSegments(orderedPoints);
  if (segments.length === 0) return <EmptyState title="Circular chart unavailable" description={series.points.some((point) => point.value < 0) ? "Pie and donut charts require non-negative values." : "The selected source did not produce positive values."} />;
  const radius = series.displayType === "donut" ? 70 : 50;
  const circumference = 2 * Math.PI * radius;
  const total = segments.reduce((sum, segment) => sum + segment.point.value, 0);
  const startColor = Math.max(0, circularColorOrder.indexOf(series.color));
  const color = (index: number) => getDashboardSeriesColor(circularColorOrder[(startColor + index) % circularColorOrder.length], appearance.palette);
  const label = (index: number) => {
    const segment = segments[index];
    return `${segment.point.label}: ${formatValue(segment.point.value)}, ${(segment.ratio * 100).toFixed(1)}%`;
  };
  const tooltip = (index: number) => {
    const segment = segments[index];
    const formattedValue = formatValue(segment.point.value);
    const percentage = `${(segment.ratio * 100).toFixed(1)}%`;
    return getDashboardTooltipText(appearance.tooltipContent, segment.point.label, series.label, formattedValue, percentage, label(index));
  };
  const summary = (index: number) => {
    const value = formatValue(segments[index].point.value);
    if (!appearance.showDataLabels) return appearance.showLegend ? value : "";
    const dataLabel = getDashboardDataLabelText(appearance.dataLabelContent, segments[index].point.value, segments[index].ratio * 100, formatValue);
    if (!appearance.showLegend || appearance.dataLabelContent === "value" || appearance.dataLabelContent === "value_and_percentage") return dataLabel;
    return `${value} · ${dataLabel}`;
  };
  const selectSegment = (index: number) => onSelect?.({ ...segments[index].point, aggregate: segments[index].point.key === aggregateKey });
  const chart = <svg aria-label={`${series.displayType === "donut" ? "Donut" : "Pie"} chart for ${series.label}`} className="mx-auto h-auto w-full max-w-[19rem]" role="img" viewBox="0 0 260 220">
      <g transform="rotate(-90 130 105)">{segments.map((segment, index) => <circle aria-label={onSelect ? `${interactionLabel}: ${label(index)}` : label(index)} className={onSelect ? "cursor-pointer outline-none focus-visible:stroke-[var(--color-foreground)]" : undefined} cx="130" cy="105" data-tooltip-target="mark" fill="none" key={segment.point.key || segment.point.label} onClick={() => selectSegment(index)} onKeyDown={(event) => { if (onSelect && (event.key === "Enter" || event.key === " ")) { event.preventDefault(); selectSegment(index); } }} r={radius} role={onSelect ? "button" : undefined} stroke={color(index)} strokeDasharray={`${segment.ratio * circumference} ${circumference}`} strokeDashoffset={-segment.offset * circumference} strokeWidth={series.displayType === "donut" ? 38 : 100} tabIndex={onSelect ? 0 : undefined}>{appearance.showTooltips ? <title>{tooltip(index)}</title> : null}</circle>)}</g>
      {series.displayType === "donut" ? <><text fill="currentColor" fontSize="12" fontWeight="700" textAnchor="middle" x="130" y="101">Total</text><text fill="currentColor" fontSize="18" fontWeight="800" textAnchor="middle" x="130" y="124">{formatValue(total)}</text></> : null}
      {selectedKey ? <circle cx="130" cy="105" fill="none" pointerEvents="none" r={series.displayType === "donut" ? 91 : 102} stroke="var(--color-foreground)" strokeDasharray="4 5" strokeWidth="2" /> : null}
    </svg>;
  const legend = appearance.showLegend || appearance.showDataLabels ? <div aria-label="Chart legend" className="grid max-h-64 min-w-0 gap-1.5 overflow-y-auto pr-1" data-legend-position={appearance.legendPosition}>{segments.map((segment, index) => <button aria-label={onSelect ? `${interactionLabel}: ${label(index)}` : undefined} className={`grid grid-cols-[auto_minmax(0,1fr)_auto] items-center gap-2 rounded-lg px-2 py-1.5 text-left text-xs transition ${onSelect ? "hover:bg-primary/5 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary" : "cursor-default"} ${selectedKey === segment.point.key ? "bg-primary/10 ring-1 ring-primary" : ""}`} data-aggregate-category={segment.point.key === aggregateKey ? "true" : undefined} data-category-key={segment.point.key} disabled={!onSelect} key={segment.point.key || segment.point.label} onClick={() => selectSegment(index)} type="button"><span className="size-3 rounded-sm" style={{ background: color(index) }} /><span className="min-w-0 truncate font-bold" title={segment.point.label}>{segment.point.label}</span><span className="text-right font-semibold tabular-nums text-muted-foreground" data-data-label-content={appearance.dataLabelContent} data-data-label-position="auto">{summary(index)}</span></button>)}</div> : null;
  return <PositionedChartLayout chart={chart} legend={legend} position={appearance.legendPosition} />;
}

function SeriesBars({ appearance, points, formatNumber, interactionLabel, onSelect, selectedKey }: { appearance: DashboardChartAppearance; points: WidgetPreviewData["series"]; formatNumber: (value: number) => string; interactionLabel: string; onSelect?: (selection: DashboardPointSelection) => void; selectedKey: string | null }) {
  const maxValue = Math.max(...points.map((point) => point.value), 1);

  if (points.length === 0) {
    return <EmptyState title="No chart data" description="The selected source did not produce any chart groups." />;
  }

  return (
    <div className="grid max-h-72 gap-3 overflow-y-auto pr-1">
      {points.map((point) => {
        const width = `${Math.max(6, (point.value / maxValue) * 100)}%`;
        const missing = Boolean(point.isMissing);

        return (
          <button aria-label={onSelect && !missing ? `${interactionLabel}: ${point.label}, ${formatNumber(point.value)}` : undefined} className={`grid gap-2 rounded-lg p-1 text-left transition ${onSelect && !missing ? "hover:bg-primary/5 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary" : "cursor-default"} ${selectedKey === point.key ? "bg-primary/10 ring-1 ring-primary" : ""}`} disabled={!onSelect || missing} key={point.key || point.label} onClick={() => onSelect?.(point)} title={appearance.showTooltips ? `${point.label}: ${missing ? "No data" : formatNumber(point.value)}` : undefined} type="button">
            <div className="grid grid-cols-[minmax(0,1fr)_auto] items-center gap-3 text-sm">
              <span className="min-w-0 truncate font-bold text-foreground">{point.label}</span>
              <span className="font-semibold text-muted-foreground">{missing ? "No data" : formatNumber(point.value)}</span>
            </div>
            <div className="h-3 overflow-hidden rounded-full bg-muted">
              {!missing ? <div className="h-full rounded-full" style={{ background: getDashboardSeriesColor("primary", appearance.palette), width }} /> : null}
            </div>
          </button>
        );
      })}
    </div>
  );
}

function ChartTable({ interactionLabel, onSelect, preview, selectedKey }: { interactionLabel: string; onSelect?: (selection: DashboardPointSelection) => void; preview: WidgetPreviewData; selectedKey: string | null }) {
  const columns: Array<TableColumn<ChartTableRow>> = preview.columns.map((column) => ({
    header: column.label,
    render: (row) => {
      const value = row.cells[column.fieldId]?.displayValue?.trim();
      return value ? value : <span className="text-muted-foreground">-</span>;
    }
  }));

  return preview.rows.length > 0 ? (
    <div className="max-h-96 overflow-auto">
      <Table columns={columns} getRowKey={(row) => row.recordId} onRowClick={onSelect ? (row) => onSelect({ key: row.recordId, label: `Record ${row.recordId}`, value: 0, recordId: row.recordId }) : undefined} rows={preview.rows} selectedRowKey={selectedKey} />
    </div>
  ) : (
    <EmptyState title="No table rows" description="The selected source did not return records for this table widget." />
  );
}
import { useEffect, useState, type ReactNode } from "react";

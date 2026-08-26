import { EmptyState } from "../../../components/ui/EmptyState";
import { Table, type TableColumn } from "../../../components/ui/Table";
import { useLocalization } from "../../../context/LocalizationContext";
import { formatDashboardValue, getDashboardAccentColor, getDashboardConditionalResult, getDashboardEffectiveCardAccent, getDashboardKpiTargetSummary, getDashboardSeriesColor, resolveDashboardChartAppearance } from "../appearance";
import type { ChartTableRow, ChartWidgetPreview as ChartWidgetPreviewData, DashboardAnalyticsResponse, DashboardChartAppearance } from "../types";
import type { DashboardPointSelection } from "../drillThrough";

type WidgetPreviewData = ChartWidgetPreviewData | DashboardAnalyticsResponse;

export function ChartWidgetPreview({ appearance: appearanceInput, interactionLabel = "Open details", onSelect, preview }: { appearance?: DashboardChartAppearance | null; interactionLabel?: string; onSelect?: (selection: DashboardPointSelection) => void; preview: WidgetPreviewData }) {
  const [selectedKey, setSelectedKey] = useState<string | null>(null);
  useEffect(() => setSelectedKey(null), [preview]);
  const select = (selection: DashboardPointSelection) => { setSelectedKey(selection.recordId ?? selection.key); onSelect?.(selection); };
  const { effectiveLocale } = useLocalization();
  const appearance = resolveDashboardChartAppearance(appearanceInput);
  const conditionalResult = getDashboardConditionalResult(appearance, preview.series[0]?.value);
  const targetSummary = getDashboardKpiTargetSummary(appearance, preview.series[0]?.value, effectiveLocale);
  const formatNumber = (value: number) => formatDashboardValue(value, appearance, effectiveLocale);
  const formatCount = (value: number) => new Intl.NumberFormat(effectiveLocale).format(value);
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
    const accent = getDashboardAccentColor(getDashboardEffectiveCardAccent(appearance, point?.value), appearance.palette);

    return (
      <button aria-label={onSelect ? interactionLabel : undefined} className={`w-full rounded-lg border border-border bg-muted/30 p-4 text-left transition ${onSelect ? "cursor-pointer hover:border-primary hover:bg-primary/5 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary" : "cursor-default"} ${selectedKey === (point?.key ?? "summary") ? "ring-2 ring-primary" : ""}`} disabled={!onSelect} onClick={() => select({ key: point?.key ?? "summary", label: point?.label ?? "Records", value: point?.value ?? 0 })} style={accent ? { borderLeftColor: accent, borderLeftWidth: 5 } : undefined} type="button">
        <p className="break-words text-sm font-bold text-muted-foreground">{point?.label ?? "Records"}</p>
        <p className="mt-2 break-words text-3xl font-bold text-foreground tabular-nums">{formatNumber(point?.value ?? 0)}</p>
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

function MultiSeriesSummary({ appearance, comparison, conditionalResult, series, formatCount, formatNumber, interactionLabel, onSelect, selectedKey, targetSummary }: { appearance: DashboardChartAppearance; comparison?: DashboardAnalyticsResponse["comparison"]; conditionalResult: ReturnType<typeof getDashboardConditionalResult>; series: NonNullable<DashboardAnalyticsResponse["dataSeries"]>; formatCount: (value: number) => string; formatNumber: (value: number) => string; interactionLabel: string; onSelect?: (selection: DashboardPointSelection) => void; selectedKey: string | null; targetSummary: ReturnType<typeof getDashboardKpiTargetSummary> }) {
  const accent = conditionalResult ? getDashboardAccentColor(conditionalResult.accent, appearance.palette) : undefined;
  return <div className="grid gap-3"><ConditionalStatus accent={accent} label={conditionalResult?.label} /><KpiTargetSummary value={targetSummary} /><KpiComparisonSummary formatValue={series[0]?.metric.type === "count" ? formatCount : formatNumber} value={comparison} /><div className="grid gap-3 sm:grid-cols-2">{series.map((item) => { const point = item.points[0]; return <button aria-label={onSelect ? `${interactionLabel}: ${item.label}` : undefined} className={`rounded-lg border border-border bg-muted/20 p-4 text-left transition ${onSelect ? "hover:border-primary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary" : "cursor-default"} ${selectedKey === (point?.key ?? item.id) ? "ring-2 ring-primary" : ""}`} disabled={!onSelect} key={item.id} onClick={() => onSelect?.({ key: point?.key ?? item.id, label: item.label, value: point?.value ?? 0 })} type="button"><div className="mb-3 h-1.5 rounded-full" style={{ background: getDashboardSeriesColor(item.color, appearance.palette) }} /><p className="text-xs font-bold text-muted-foreground">{item.label}</p><p className="mt-1 text-2xl font-extrabold tabular-nums">{(item.metric.type === "count" ? formatCount : formatNumber)(point?.value ?? 0)}</p><p className="mt-1 text-[11px] text-muted-foreground">{item.metric.type}{item.axis === "right" ? " · right axis" : ""}</p></button>;})}</div></div>;
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

function MultiSeriesChart({ appearance, series, formatCount, formatNumber, interactionLabel, onSelect, selectedKey }: { appearance: DashboardChartAppearance; series: NonNullable<DashboardAnalyticsResponse["dataSeries"]>; formatCount: (value: number) => string; formatNumber: (value: number) => string; interactionLabel: string; onSelect?: (selection: DashboardPointSelection) => void; selectedKey: string | null }) {
  const keys = [...new Set(series.flatMap((item) => item.points.map((point) => point.key)))].slice(0, 12);
  const labels = keys.map((key) => series.flatMap((item) => item.points).find((point) => point.key === key)?.label ?? key);
  const axisMaximum = (axis: "left" | "right") => Math.max(1, ...series.filter((item) => item.axis === axis).flatMap((item) => item.points.map((point) => Math.max(0, point.value))));
  const leftMaximum = axisMaximum("left");
  const rightMaximum = axisMaximum("right");
  const plot = { left: 42, top: 16, width: 570, height: 170 };
  const x = (index: number) => plot.left + (index + .5) * plot.width / Math.max(keys.length, 1);
  const y = (value: number, axis: "left" | "right") => plot.top + plot.height - Math.max(0, value) / (axis === "right" ? rightMaximum : leftMaximum) * plot.height;
  const barSeries = series.filter((item) => item.displayType === "bar");
  const gridlines = appearance.showGridlines ? [0, .25, .5, .75, 1] : [0];
  const axisFormat = (axis: "left" | "right") => series.find((item) => item.axis === axis)?.metric.type === "count" ? formatCount : formatNumber;
  return <div className="grid min-w-0 gap-3">{appearance.showLegend ? <div className="flex flex-wrap gap-3" aria-label="Chart legend">{series.map((item) => <span className="flex items-center gap-1.5 text-xs font-bold" key={item.id}><span className="size-2.5 rounded-full" style={{ background: getDashboardSeriesColor(item.color, appearance.palette) }} />{item.label}<span className="font-medium text-muted-foreground">({item.axis})</span></span>)}</div> : null}<div className="max-w-full overflow-x-auto"><svg aria-label="Configured series chart" className="min-w-[38rem]" role="img" viewBox="0 0 640 230">{gridlines.map((ratio) => <line key={ratio} opacity={ratio === 0 ? 1 : .65} stroke="var(--color-border)" x1={plot.left} x2={plot.left + plot.width} y1={plot.top + plot.height - ratio * plot.height} y2={plot.top + plot.height - ratio * plot.height} />)}<text fill="currentColor" fontSize="9" x={plot.left} y="11">{axisFormat("left")(leftMaximum)}</text>{series.some((item) => item.axis === "right") ? <text fill="currentColor" fontSize="9" textAnchor="end" x={plot.left + plot.width} y="11">{axisFormat("right")(rightMaximum)}</text> : null}{series.map((item, seriesIndex) => { const values = keys.map((key) => item.points.find((point) => point.key === key)?.value ?? 0); const points = values.map((value, index) => `${x(index)},${y(value, item.axis)}`).join(" "); const color = getDashboardSeriesColor(item.color, appearance.palette); const formatter = item.metric.type === "count" ? formatCount : formatNumber; const labelY = (value: number) => Math.max(28 + seriesIndex * 12, y(value, item.axis) - 5); const labelProps = { fill: "currentColor", fontSize: 9, paintOrder: "stroke" as const, stroke: "var(--color-card)", strokeWidth: 3, textAnchor: "middle" as const }; if (item.displayType === "bar") { const barIndex = barSeries.findIndex((seriesItem) => seriesItem.id === item.id); const width = Math.min(28, plot.width / Math.max(keys.length, 1) / Math.max(barSeries.length + 1, 2)); return <g key={item.id}>{values.map((value, index) => <g key={keys[index]}><rect fill={color} height={plot.top + plot.height - y(value, item.axis)} rx="2" width={width} x={x(index) - barSeries.length * width / 2 + barIndex * width} y={y(value, item.axis)}><title>{item.label}: {formatter(value)}</title></rect>{appearance.showDataLabels ? <text {...labelProps} x={x(index) - barSeries.length * width / 2 + barIndex * width + width / 2} y={labelY(value)}>{formatter(value)}</text> : null}</g>)}</g>; } if (item.displayType === "area") return <g key={item.id}><polygon fill={color} opacity=".18" points={`${x(0)},${plot.top + plot.height} ${points} ${x(Math.max(0, keys.length - 1))},${plot.top + plot.height}`} /><polyline fill="none" points={points} stroke={color} strokeWidth="3" />{appearance.showDataLabels ? values.map((value, index) => <text {...labelProps} key={keys[index]} x={x(index)} y={labelY(value)}>{formatter(value)}</text>) : null}</g>; return <g key={item.id}><polyline fill="none" points={points} stroke={color} strokeLinecap="round" strokeLinejoin="round" strokeWidth="3" />{appearance.showDataLabels ? values.map((value, index) => <text {...labelProps} key={keys[index]} x={x(index)} y={labelY(value)}>{formatter(value)}</text>) : null}</g>; })}{labels.map((label, index) => <text fill="currentColor" fontSize="10" key={keys[index]} textAnchor="middle" x={x(index)} y="207">{label.slice(0, 10)}</text>)}{onSelect ? keys.map((key, index) => { const point = series.flatMap((item) => item.points).find((item) => item.key === key); const width = plot.width / Math.max(keys.length, 1); const tooltip = series.map((item) => `${item.label}: ${(item.metric.type === "count" ? formatCount : formatNumber)(item.points.find((candidate) => candidate.key === key)?.value ?? 0)}`).join(" · "); return <rect aria-label={`${interactionLabel}: ${labels[index]}. ${tooltip}`} fill="transparent" height={plot.height + 25} key={`interaction-${key}`} onClick={() => onSelect({ key, label: labels[index], value: point?.value ?? 0 })} onKeyDown={(event) => { if (event.key === "Enter" || event.key === " ") { event.preventDefault(); onSelect({ key, label: labels[index], value: point?.value ?? 0 }); } }} role="button" stroke={selectedKey === key ? "var(--color-primary)" : "transparent"} strokeWidth="2" tabIndex={0} width={width} x={plot.left + index * width} y={plot.top}><title>{tooltip}</title></rect>; }) : null}</svg></div></div>;
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

        return (
          <button aria-label={onSelect ? `${interactionLabel}: ${point.label}, ${formatNumber(point.value)}` : undefined} className={`grid gap-2 rounded-lg p-1 text-left transition ${onSelect ? "hover:bg-primary/5 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary" : "cursor-default"} ${selectedKey === point.key ? "bg-primary/10 ring-1 ring-primary" : ""}`} disabled={!onSelect} key={point.key || point.label} onClick={() => onSelect?.(point)} type="button">
            <div className="grid grid-cols-[minmax(0,1fr)_auto] items-center gap-3 text-sm">
              <span className="min-w-0 truncate font-bold text-foreground">{point.label}</span>
              <span className="font-semibold text-muted-foreground">{formatNumber(point.value)}</span>
            </div>
            <div className="h-3 overflow-hidden rounded-full bg-muted">
              <div className="h-full rounded-full" style={{ background: getDashboardSeriesColor("primary", appearance.palette), width }} />
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
import { useEffect, useState } from "react";

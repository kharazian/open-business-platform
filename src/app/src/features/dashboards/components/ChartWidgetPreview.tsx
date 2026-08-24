import { EmptyState } from "../../../components/ui/EmptyState";
import { Table, type TableColumn } from "../../../components/ui/Table";
import { useLocalization } from "../../../context/LocalizationContext";
import { formatDashboardValue, getDashboardSeriesColor, resolveDashboardChartAppearance } from "../appearance";
import type { ChartTableRow, ChartWidgetPreview as ChartWidgetPreviewData, DashboardAnalyticsResponse, DashboardChartAppearance } from "../types";

type WidgetPreviewData = ChartWidgetPreviewData | DashboardAnalyticsResponse;

export function ChartWidgetPreview({ appearance: appearanceInput, preview }: { appearance?: DashboardChartAppearance | null; preview: WidgetPreviewData }) {
  const { effectiveLocale } = useLocalization();
  const appearance = resolveDashboardChartAppearance(appearanceInput);
  const formatNumber = (value: number) => formatDashboardValue(value, appearance, effectiveLocale);
  const formatCount = (value: number) => new Intl.NumberFormat(effectiveLocale).format(value);
  if ("dataSeries" in preview && (preview.dataSeries?.length ?? 0) > 1 && preview.widgetType !== "table") {
    return preview.widgetType === "summary"
      ? <MultiSeriesSummary appearance={appearance} formatCount={formatCount} formatNumber={formatNumber} series={preview.dataSeries!} />
      : <MultiSeriesChart appearance={appearance} formatCount={formatCount} formatNumber={formatNumber} series={preview.dataSeries!} />;
  }
  if (preview.widgetType === "table") {
    return <ChartTable preview={preview} />;
  }

  if (preview.widgetType === "number_card" || preview.widgetType === "summary") {
    const point = preview.series[0];

    return (
      <div className="rounded-lg border border-border bg-muted/30 p-4">
        <p className="break-words text-sm font-bold text-muted-foreground">{point?.label ?? "Records"}</p>
        <p className="mt-2 break-words text-3xl font-bold text-foreground tabular-nums">{formatNumber(point?.value ?? 0)}</p>
      </div>
    );
  }

  return <SeriesBars appearance={appearance} formatNumber={formatNumber} points={preview.series} />;
}

function MultiSeriesSummary({ appearance, series, formatCount, formatNumber }: { appearance: DashboardChartAppearance; series: NonNullable<DashboardAnalyticsResponse["dataSeries"]>; formatCount: (value: number) => string; formatNumber: (value: number) => string }) {
  return <div className="grid gap-3 sm:grid-cols-2">{series.map((item) => <div className="rounded-lg border border-border bg-muted/20 p-4" key={item.id}><div className="mb-3 h-1.5 rounded-full" style={{ background: getDashboardSeriesColor(item.color, appearance.palette) }} /><p className="text-xs font-bold text-muted-foreground">{item.label}</p><p className="mt-1 text-2xl font-extrabold tabular-nums">{(item.metric.type === "count" ? formatCount : formatNumber)(item.points[0]?.value ?? 0)}</p><p className="mt-1 text-[11px] text-muted-foreground">{item.metric.type}{item.axis === "right" ? " · right axis" : ""}</p></div>)}</div>;
}

function MultiSeriesChart({ appearance, series, formatCount, formatNumber }: { appearance: DashboardChartAppearance; series: NonNullable<DashboardAnalyticsResponse["dataSeries"]>; formatCount: (value: number) => string; formatNumber: (value: number) => string }) {
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
  return <div className="grid min-w-0 gap-3">{appearance.showLegend ? <div className="flex flex-wrap gap-3" aria-label="Chart legend">{series.map((item) => <span className="flex items-center gap-1.5 text-xs font-bold" key={item.id}><span className="size-2.5 rounded-full" style={{ background: getDashboardSeriesColor(item.color, appearance.palette) }} />{item.label}<span className="font-medium text-muted-foreground">({item.axis})</span></span>)}</div> : null}<div className="max-w-full overflow-x-auto"><svg aria-label="Multi-series chart" className="min-w-[38rem]" role="img" viewBox="0 0 640 230">{gridlines.map((ratio) => <line key={ratio} opacity={ratio === 0 ? 1 : .65} stroke="var(--color-border)" x1={plot.left} x2={plot.left + plot.width} y1={plot.top + plot.height - ratio * plot.height} y2={plot.top + plot.height - ratio * plot.height} />)}<text fill="currentColor" fontSize="9" x={plot.left} y="11">{axisFormat("left")(leftMaximum)}</text>{series.some((item) => item.axis === "right") ? <text fill="currentColor" fontSize="9" textAnchor="end" x={plot.left + plot.width} y="11">{axisFormat("right")(rightMaximum)}</text> : null}{series.map((item, seriesIndex) => { const values = keys.map((key) => item.points.find((point) => point.key === key)?.value ?? 0); const points = values.map((value, index) => `${x(index)},${y(value, item.axis)}`).join(" "); const color = getDashboardSeriesColor(item.color, appearance.palette); const formatter = item.metric.type === "count" ? formatCount : formatNumber; const labelY = (value: number) => Math.max(28 + seriesIndex * 12, y(value, item.axis) - 5); const labelProps = { fill: "currentColor", fontSize: 9, paintOrder: "stroke" as const, stroke: "var(--color-card)", strokeWidth: 3, textAnchor: "middle" as const }; if (item.displayType === "bar") { const barIndex = barSeries.findIndex((seriesItem) => seriesItem.id === item.id); const width = Math.min(28, plot.width / Math.max(keys.length, 1) / Math.max(barSeries.length + 1, 2)); return <g key={item.id}>{values.map((value, index) => <g key={keys[index]}><rect fill={color} height={plot.top + plot.height - y(value, item.axis)} rx="2" width={width} x={x(index) - barSeries.length * width / 2 + barIndex * width} y={y(value, item.axis)}><title>{item.label}: {formatter(value)}</title></rect>{appearance.showDataLabels ? <text {...labelProps} x={x(index) - barSeries.length * width / 2 + barIndex * width + width / 2} y={labelY(value)}>{formatter(value)}</text> : null}</g>)}</g>; } if (item.displayType === "area") return <g key={item.id}><polygon fill={color} opacity=".18" points={`${x(0)},${plot.top + plot.height} ${points} ${x(Math.max(0, keys.length - 1))},${plot.top + plot.height}`} /><polyline fill="none" points={points} stroke={color} strokeWidth="3" />{appearance.showDataLabels ? values.map((value, index) => <text {...labelProps} key={keys[index]} x={x(index)} y={labelY(value)}>{formatter(value)}</text>) : null}</g>; return <g key={item.id}><polyline fill="none" points={points} stroke={color} strokeLinecap="round" strokeLinejoin="round" strokeWidth="3" />{appearance.showDataLabels ? values.map((value, index) => <text {...labelProps} key={keys[index]} x={x(index)} y={labelY(value)}>{formatter(value)}</text>) : null}</g>; })}{labels.map((label, index) => <text fill="currentColor" fontSize="10" key={keys[index]} textAnchor="middle" x={x(index)} y="207">{label.slice(0, 10)}</text>)}</svg></div></div>;
}

function SeriesBars({ appearance, points, formatNumber }: { appearance: DashboardChartAppearance; points: WidgetPreviewData["series"]; formatNumber: (value: number) => string }) {
  const maxValue = Math.max(...points.map((point) => point.value), 1);

  if (points.length === 0) {
    return <EmptyState title="No chart data" description="The selected source did not produce any chart groups." />;
  }

  return (
    <div className="grid max-h-72 gap-3 overflow-y-auto pr-1">
      {points.map((point) => {
        const width = `${Math.max(6, (point.value / maxValue) * 100)}%`;

        return (
          <div className="grid gap-2" key={point.key || point.label}>
            <div className="grid grid-cols-[minmax(0,1fr)_auto] items-center gap-3 text-sm">
              <span className="min-w-0 truncate font-bold text-foreground">{point.label}</span>
              <span className="font-semibold text-muted-foreground">{formatNumber(point.value)}</span>
            </div>
            <div className="h-3 overflow-hidden rounded-full bg-muted">
              <div className="h-full rounded-full" style={{ background: getDashboardSeriesColor("primary", appearance.palette), width }} />
            </div>
          </div>
        );
      })}
    </div>
  );
}

function ChartTable({ preview }: { preview: WidgetPreviewData }) {
  const columns: Array<TableColumn<ChartTableRow>> = preview.columns.map((column) => ({
    header: column.label,
    render: (row) => {
      const value = row.cells[column.fieldId]?.displayValue?.trim();
      return value ? value : <span className="text-muted-foreground">-</span>;
    }
  }));

  return preview.rows.length > 0 ? (
    <div className="max-h-96 overflow-auto">
      <Table columns={columns} rows={preview.rows} />
    </div>
  ) : (
    <EmptyState title="No table rows" description="The selected source did not return records for this table widget." />
  );
}

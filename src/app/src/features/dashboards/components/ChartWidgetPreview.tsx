import { EmptyState } from "../../../components/ui/EmptyState";
import { Table, type TableColumn } from "../../../components/ui/Table";
import type { ChartTableRow, ChartWidgetPreview as ChartWidgetPreviewData, DashboardAnalyticsResponse } from "../types";

type WidgetPreviewData = ChartWidgetPreviewData | DashboardAnalyticsResponse;

export function ChartWidgetPreview({ preview }: { preview: WidgetPreviewData }) {
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

  return <SeriesBars points={preview.series} />;
}

function SeriesBars({ points }: { points: WidgetPreviewData["series"] }) {
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
              <div className="h-full rounded-full bg-primary" style={{ width }} />
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

function formatNumber(value: number): string {
  return new Intl.NumberFormat(undefined, { maximumFractionDigits: 2 }).format(value);
}

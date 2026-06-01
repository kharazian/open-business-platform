import { type FormEvent, useEffect, useMemo, useState } from "react";
import { BarChart3, Play, RefreshCw } from "lucide-react";
import { Alert } from "../../../components/ui/Alert";
import { Badge } from "../../../components/ui/Badge";
import { Button } from "../../../components/ui/Button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "../../../components/ui/Card";
import { Checkbox } from "../../../components/ui/Checkbox";
import { EmptyState } from "../../../components/ui/EmptyState";
import { Input } from "../../../components/ui/Input";
import { PageHeader } from "../../../components/ui/PageHeader";
import { Select } from "../../../components/ui/Select";
import { Table, type TableColumn } from "../../../components/ui/Table";
import { getForm, listForms, type FormDetail } from "../../forms/api";
import type { FormSummary } from "../../forms/drafts";
import { getReportableFields } from "../../forms/reportableFields";
import { listReports } from "../../reports/api";
import type { ListReportSummary } from "../../reports/types";
import { previewChartWidget } from "../api";
import {
  type ChartMetricType,
  type ChartTableRow,
  type ChartWidgetConfig,
  type ChartWidgetPreview,
  type ChartWidgetType
} from "../types";

const defaultLimit = 10;

const widgetOptions: Array<{ label: string; value: ChartWidgetType }> = [
  { label: "Number card", value: "number_card" },
  { label: "Bar chart", value: "bar_chart" },
  { label: "Date trend", value: "date_trend" },
  { label: "Status / choice breakdown", value: "choice_breakdown" },
  { label: "Table", value: "table" }
];

const metricOptions: Array<{ label: string; value: ChartMetricType }> = [
  { label: "Count records", value: "count" },
  { label: "Sum numeric field", value: "sum" },
  { label: "Average numeric field", value: "average" }
];

export function ChartBuilderPage() {
  const [forms, setForms] = useState<FormSummary[]>([]);
  const [selectedFormId, setSelectedFormId] = useState("");
  const [formDetail, setFormDetail] = useState<FormDetail | null>(null);
  const [reports, setReports] = useState<ListReportSummary[]>([]);
  const [selectedReportId, setSelectedReportId] = useState("");
  const [widgetType, setWidgetType] = useState<ChartWidgetType>("number_card");
  const [metricType, setMetricType] = useState<ChartMetricType>("count");
  const [metricFieldId, setMetricFieldId] = useState("");
  const [groupByFieldId, setGroupByFieldId] = useState("status");
  const [dateFieldId, setDateFieldId] = useState("created_at");
  const [selectedColumns, setSelectedColumns] = useState<string[]>([]);
  const [limit, setLimit] = useState(defaultLimit);
  const [preview, setPreview] = useState<ChartWidgetPreview | null>(null);
  const [loadingForms, setLoadingForms] = useState(true);
  const [loadingSource, setLoadingSource] = useState(false);
  const [runningPreview, setRunningPreview] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    setLoadingForms(true);
    setError(null);

    listForms()
      .then((items) => {
        if (!active) return;
        setForms(items);
        setSelectedFormId((current) => current || items[0]?.id || "");
      })
      .catch((caught: unknown) => {
        if (!active) return;
        setError(getErrorMessage(caught));
      })
      .finally(() => {
        if (active) setLoadingForms(false);
      });

    return () => {
      active = false;
    };
  }, []);

  useEffect(() => {
    if (!selectedFormId) {
      setFormDetail(null);
      setReports([]);
      setPreview(null);
      return;
    }

    let active = true;
    setLoadingSource(true);
    setError(null);
    setPreview(null);
    setSelectedReportId("");

    Promise.all([getForm(selectedFormId), listReports(selectedFormId)])
      .then(([form, reportItems]) => {
        if (!active) return;
        setFormDetail(form);
        setReports(reportItems);
      })
      .catch((caught: unknown) => {
        if (!active) return;
        setError(getErrorMessage(caught));
        setFormDetail(null);
        setReports([]);
      })
      .finally(() => {
        if (active) setLoadingSource(false);
      });

    return () => {
      active = false;
    };
  }, [selectedFormId]);

  const fieldOptions = useMemo(() => (formDetail ? getReportableFields(formDetail.draftSchema) : []), [formDetail]);
  const numericFields = fieldOptions.filter((field) => field.supportsAggregation);
  const groupFields = fieldOptions.filter((field) => field.supportsChoiceGrouping);
  const dateFields = fieldOptions.filter((field) => field.type === "date" || field.type === "datetime");
  const selectedForm = forms.find((form) => form.id === selectedFormId) ?? null;

  useEffect(() => {
    if (numericFields.length > 0 && !numericFields.some((field) => field.id === metricFieldId)) {
      setMetricFieldId(numericFields[0].id);
    }
  }, [metricFieldId, numericFields]);

  useEffect(() => {
    if (groupFields.length > 0 && !groupFields.some((field) => field.id === groupByFieldId)) {
      setGroupByFieldId(groupFields[0].id);
    }
  }, [groupByFieldId, groupFields]);

  useEffect(() => {
    if (dateFields.length > 0 && !dateFields.some((field) => field.id === dateFieldId)) {
      setDateFieldId(dateFields[0].id);
    }
  }, [dateFieldId, dateFields]);

  useEffect(() => {
    if (fieldOptions.length === 0) {
      setSelectedColumns([]);
      return;
    }

    setSelectedColumns((current) => {
      const validCurrent = current.filter((fieldId) => fieldOptions.some((field) => field.id === fieldId));
      return validCurrent.length > 0 ? validCurrent : fieldOptions.slice(0, Math.min(5, fieldOptions.length)).map((field) => field.id);
    });
  }, [fieldOptions]);

  const request = buildChartConfig();
  const canPreview = Boolean(selectedFormId) && hasRequiredConfig(request);

  async function handlePreview(event?: FormEvent<HTMLFormElement>) {
    event?.preventDefault();

    if (!selectedFormId || !canPreview) {
      return;
    }

    setRunningPreview(true);
    setError(null);

    try {
      setPreview(await previewChartWidget(selectedFormId, request));
    } catch (caught) {
      setPreview(null);
      setError(getErrorMessage(caught));
    } finally {
      setRunningPreview(false);
    }
  }

  function buildChartConfig(): ChartWidgetConfig {
    return {
      widgetType,
      metric: {
        type: metricType,
        fieldId: metricType === "count" ? null : metricFieldId || null
      },
      groupByFieldId: widgetType === "bar_chart" || widgetType === "choice_breakdown" ? groupByFieldId || null : null,
      dateFieldId: widgetType === "date_trend" ? dateFieldId || null : null,
      columns: widgetType === "table" ? selectedColumns : [],
      limit,
      reportId: selectedReportId || null
    };
  }

  function handleToggleColumn(fieldId: string, selected: boolean) {
    setSelectedColumns((current) => {
      if (selected) {
        return current.includes(fieldId) ? current : [...current, fieldId];
      }

      return current.filter((currentFieldId) => currentFieldId !== fieldId);
    });
  }

  return (
    <div className="grid gap-6">
      <PageHeader
        eyebrow="Charts V2 preview"
        title="Chart builder lite"
        description="Preview simple widgets over permitted form records and saved list reports."
        actions={
          <Button disabled={!canPreview || runningPreview} onClick={() => void handlePreview()}>
            <Play className="size-4" />
            {runningPreview ? "Rendering..." : "Preview"}
          </Button>
        }
      />

      {error ? <Alert title="Chart builder">{error}</Alert> : null}

      <section className="grid gap-4 xl:grid-cols-[20rem_minmax(0,1fr)]">
        <Card className="self-start">
          <CardHeader>
            <CardTitle>Source</CardTitle>
            <CardDescription>Form data or a saved report filter.</CardDescription>
          </CardHeader>
          <CardContent className="grid gap-4">
            <Select
              disabled={loadingForms || forms.length === 0}
              label="Form"
              onChange={(event) => setSelectedFormId(event.target.value)}
              value={selectedFormId}
            >
              {forms.map((form) => (
                <option key={form.id} value={form.id}>
                  {form.name}
                </option>
              ))}
            </Select>
            <Select
              disabled={!selectedFormId || loadingSource}
              label="Saved report filter"
              onChange={(event) => setSelectedReportId(event.target.value)}
              value={selectedReportId}
            >
              <option value="">All form records</option>
              {reports.map((report) => (
                <option key={report.id} value={report.id}>
                  {report.name}
                </option>
              ))}
            </Select>
            <div className="rounded-xl border border-border bg-muted/30 p-4">
              <p className="font-bold text-foreground">{selectedForm?.name ?? "No form selected"}</p>
              <dl className="mt-4 grid grid-cols-2 gap-3 text-sm">
                <div>
                  <dt className="font-bold text-muted-foreground">Fields</dt>
                  <dd className="mt-1 text-foreground">{fieldOptions.length}</dd>
                </div>
                <div>
                  <dt className="font-bold text-muted-foreground">Reports</dt>
                  <dd className="mt-1 text-foreground">{reports.length}</dd>
                </div>
              </dl>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <div className="flex items-start justify-between gap-3">
              <div>
                <CardTitle>Widget config</CardTitle>
                <CardDescription>Metric, grouping, and display settings.</CardDescription>
              </div>
              <Badge>{widgetOptions.find((option) => option.value === widgetType)?.label}</Badge>
            </div>
          </CardHeader>
          <CardContent>
            {loadingSource ? (
              <EmptyState title="Loading fields" description="Fetching reportable form metadata." />
            ) : fieldOptions.length > 0 ? (
              <form className="grid gap-5" onSubmit={handlePreview}>
                <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
                  <Select
                    label="Widget"
                    onChange={(event) => setWidgetType(event.target.value as ChartWidgetType)}
                    options={widgetOptions}
                    value={widgetType}
                  />
                  <Select
                    label="Metric"
                    onChange={(event) => setMetricType(event.target.value as ChartMetricType)}
                    options={metricOptions}
                    value={metricType}
                  />
                  <Input
                    label="Limit"
                    max={50}
                    min={1}
                    onChange={(event) => setLimit(Number(event.target.value))}
                    type="number"
                    value={limit}
                  />
                </div>

                {metricType !== "count" ? (
                  <Select
                    disabled={numericFields.length === 0}
                    label="Numeric metric field"
                    onChange={(event) => setMetricFieldId(event.target.value)}
                    value={metricFieldId}
                  >
                    {numericFields.map((field) => (
                      <option key={field.id} value={field.id}>
                        {field.label}
                      </option>
                    ))}
                  </Select>
                ) : null}

                {widgetType === "bar_chart" || widgetType === "choice_breakdown" ? (
                  <Select
                    disabled={groupFields.length === 0}
                    label="Group by"
                    onChange={(event) => setGroupByFieldId(event.target.value)}
                    value={groupByFieldId}
                  >
                    {groupFields.map((field) => (
                      <option key={field.id} value={field.id}>
                        {field.label}
                      </option>
                    ))}
                  </Select>
                ) : null}

                {widgetType === "date_trend" ? (
                  <Select
                    disabled={dateFields.length === 0}
                    label="Trend date"
                    onChange={(event) => setDateFieldId(event.target.value)}
                    value={dateFieldId}
                  >
                    {dateFields.map((field) => (
                      <option key={field.id} value={field.id}>
                        {field.label}
                      </option>
                    ))}
                  </Select>
                ) : null}

                {widgetType === "table" ? (
                  <div className="grid gap-3">
                    <div className="flex items-center justify-between gap-3">
                      <p className="text-sm font-bold text-foreground">Table columns</p>
                      <Badge>{selectedColumns.length} selected</Badge>
                    </div>
                    <div className="grid gap-2 md:grid-cols-2 xl:grid-cols-3">
                      {fieldOptions.map((field) => (
                        <Checkbox
                          checked={selectedColumns.includes(field.id)}
                          description={field.source === "system" ? "System field" : "Form field"}
                          key={field.id}
                          label={field.label}
                          onChange={(event) => handleToggleColumn(field.id, event.target.checked)}
                        />
                      ))}
                    </div>
                  </div>
                ) : null}

                <div className="flex justify-end">
                  <Button disabled={!canPreview || runningPreview} type="submit">
                    <BarChart3 className="size-4" />
                    {runningPreview ? "Rendering..." : "Render widget"}
                  </Button>
                </div>
              </form>
            ) : (
              <EmptyState title="No chart fields" description="Save fields on this form before rendering charts." />
            )}
          </CardContent>
        </Card>
      </section>

      <Card>
        <CardHeader>
          <div className="flex flex-wrap items-start justify-between gap-3">
            <div>
              <CardTitle>Preview</CardTitle>
              <CardDescription>{preview ? `${preview.formName} · ${preview.totalCount} matching records` : "Render a widget to preview database-backed results."}</CardDescription>
            </div>
            <Badge>{preview ? preview.widgetType.replace(/_/g, " ") : "Not rendered"}</Badge>
          </div>
        </CardHeader>
        <CardContent>
          {runningPreview ? (
            <div className="rounded-xl border border-dashed border-border bg-muted/40 p-8 text-center">
              <RefreshCw className="mx-auto size-8 animate-spin text-muted-foreground" />
              <p className="mt-3 text-sm font-bold text-foreground">Rendering chart</p>
            </div>
          ) : preview ? (
            <ChartPreview preview={preview} />
          ) : (
            <EmptyState title="No chart preview" description="Choose a widget type and render a preview." />
          )}
        </CardContent>
      </Card>
    </div>
  );
}

function ChartPreview({ preview }: { preview: ChartWidgetPreview }) {
  if (preview.widgetType === "table") {
    return <ChartTable preview={preview} />;
  }

  if (preview.widgetType === "number_card") {
    const point = preview.series[0];

    return (
      <div className="rounded-xl border border-border bg-muted/30 p-6">
        <p className="text-sm font-bold text-muted-foreground">{point?.label ?? "Records"}</p>
        <p className="mt-3 text-4xl font-bold text-foreground">{formatNumber(point?.value ?? 0)}</p>
      </div>
    );
  }

  return <SeriesBars points={preview.series} />;
}

function SeriesBars({ points }: { points: ChartWidgetPreview["series"] }) {
  const maxValue = Math.max(...points.map((point) => point.value), 1);

  if (points.length === 0) {
    return <EmptyState title="No chart data" description="The selected source did not produce any chart groups." />;
  }

  return (
    <div className="grid gap-3">
      {points.map((point) => {
        const width = `${Math.max(6, (point.value / maxValue) * 100)}%`;

        return (
          <div className="grid gap-2" key={point.key || point.label}>
            <div className="flex items-center justify-between gap-3 text-sm">
              <span className="font-bold text-foreground">{point.label}</span>
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

function ChartTable({ preview }: { preview: ChartWidgetPreview }) {
  const columns = useMemo<Array<TableColumn<ChartTableRow>>>(
    () =>
      preview.columns.map((column) => ({
        header: column.label,
        render: (row) => {
          const value = row.cells[column.fieldId]?.displayValue?.trim();
          return value ? value : <span className="text-muted-foreground">-</span>;
        }
      })),
    [preview.columns]
  );

  return preview.rows.length > 0 ? (
    <Table columns={columns} rows={preview.rows} />
  ) : (
    <EmptyState title="No table rows" description="The selected source did not return records for this table widget." />
  );
}

function hasRequiredConfig(config: ChartWidgetConfig): boolean {
  if ((config.metric.type === "sum" || config.metric.type === "average") && !config.metric.fieldId) {
    return false;
  }

  if ((config.widgetType === "bar_chart" || config.widgetType === "choice_breakdown") && !config.groupByFieldId) {
    return false;
  }

  if (config.widgetType === "date_trend" && !config.dateFieldId) {
    return false;
  }

  if (config.widgetType === "table" && (config.columns?.length ?? 0) === 0) {
    return false;
  }

  return true;
}

function formatNumber(value: number): string {
  return new Intl.NumberFormat(undefined, { maximumFractionDigits: 2 }).format(value);
}

function getErrorMessage(error: unknown): string {
  return error instanceof Error ? error.message : "Chart request failed.";
}

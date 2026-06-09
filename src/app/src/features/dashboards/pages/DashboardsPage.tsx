import { useEffect, useMemo, useState } from "react";
import { ArrowDown, ArrowUp, Plus, RefreshCw, Save, Trash2 } from "lucide-react";
import { Alert } from "../../../components/ui/Alert";
import { Badge } from "../../../components/ui/Badge";
import { Button } from "../../../components/ui/Button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "../../../components/ui/Card";
import { Checkbox } from "../../../components/ui/Checkbox";
import { EmptyState } from "../../../components/ui/EmptyState";
import { Input } from "../../../components/ui/Input";
import { PageHeader } from "../../../components/ui/PageHeader";
import { Select } from "../../../components/ui/Select";
import { getForm, listForms, type FormDetail } from "../../forms/api";
import type { FormSummary } from "../../forms/drafts";
import { getReportableFields } from "../../forms/reportableFields";
import { listReports } from "../../reports/api";
import type { ListReportSummary } from "../../reports/types";
import { createDashboard, getDashboard, listDashboards, runDashboardAnalytics, updateDashboard } from "../api";
import {
  buildChartConfigFromDashboardAnalytics,
  buildDashboardAnalyticsRequest,
  createDashboardPreviewStates,
  getDashboardAnalyticsWidgetLabel,
  hasRequiredDashboardAnalyticsConfig,
  toDashboardAnalyticsWidgetType,
  type DashboardPreviewState
} from "../analytics";
import { ChartWidgetPreview } from "../components/ChartWidgetPreview";
import { getDashboardWidgetGridClass, orderDashboardLayoutWidgets } from "../layout";
import {
  dashboardWidgetWidths,
  type ChartMetricType,
  type ChartWidgetConfig,
  type DashboardAnalyticsResponse,
  type DashboardAnalyticsWidgetType,
  type DashboardDetail,
  type DashboardSummaryItem,
  type DashboardWidgetWidth,
  type SavedDashboardWidget,
  type SavedDashboardWidgetLayout
} from "../types";

const analyticsWidgetOptions: Array<{ label: string; value: DashboardAnalyticsWidgetType }> = [
  { label: "Summary", value: "summary" },
  { label: "Breakdown", value: "breakdown" },
  { label: "Trend", value: "trend" },
  { label: "Table", value: "table" }
];

const metricOptions: Array<{ label: string; value: ChartMetricType }> = [
  { label: "Count records", value: "count" },
  { label: "Sum numeric field", value: "sum" },
  { label: "Average numeric field", value: "average" }
];

const widthOptions = dashboardWidgetWidths.map((width) => ({ label: width, value: width }));

export function DashboardsPage() {
  const [dashboards, setDashboards] = useState<DashboardSummaryItem[]>([]);
  const [selectedDashboardId, setSelectedDashboardId] = useState("");
  const [dashboardDetail, setDashboardDetail] = useState<DashboardDetail | null>(null);
  const [forms, setForms] = useState<FormSummary[]>([]);
  const [formDetail, setFormDetail] = useState<FormDetail | null>(null);
  const [reports, setReports] = useState<ListReportSummary[]>([]);
  const [previewStates, setPreviewStates] = useState<Record<string, DashboardPreviewState | undefined>>({});
  const [dashboardName, setDashboardName] = useState("Operations dashboard");
  const [dashboardDescription, setDashboardDescription] = useState("");
  const [selectedFormId, setSelectedFormId] = useState("");
  const [selectedReportId, setSelectedReportId] = useState("");
  const [widgetTitle, setWidgetTitle] = useState("New widget");
  const [widgetType, setWidgetType] = useState<DashboardAnalyticsWidgetType>("summary");
  const [metricType, setMetricType] = useState<ChartMetricType>("count");
  const [metricFieldId, setMetricFieldId] = useState("");
  const [groupByFieldId, setGroupByFieldId] = useState("status");
  const [dateFieldId, setDateFieldId] = useState("created_at");
  const [selectedColumns, setSelectedColumns] = useState<string[]>([]);
  const [widgetWidth, setWidgetWidth] = useState<DashboardWidgetWidth>("medium");
  const [widgets, setWidgets] = useState<SavedDashboardWidget[]>([]);
  const [layoutWidgets, setLayoutWidgets] = useState<SavedDashboardWidgetLayout[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  useEffect(() => {
    void loadInitialData();
  }, []);

  useEffect(() => {
    if (!selectedDashboardId) {
      setDashboardDetail(null);
      return;
    }

    void loadDashboard(selectedDashboardId);
  }, [selectedDashboardId]);

  useEffect(() => {
    if (!selectedFormId) {
      setFormDetail(null);
      setReports([]);
      return;
    }

    setSelectedReportId("");
    Promise.all([getForm(selectedFormId), listReports(selectedFormId)])
      .then(([form, reportItems]) => {
        setFormDetail(form);
        setReports(reportItems);
      })
      .catch((caught) => setError(getErrorMessage(caught)));
  }, [selectedFormId]);

  const fieldOptions = useMemo(() => (formDetail ? getReportableFields(formDetail.draftSchema) : []), [formDetail]);
  const numericFields = fieldOptions.filter((field) => field.supportsAggregation);
  const groupFields = fieldOptions.filter((field) => field.supportsChoiceGrouping);
  const dateFields = fieldOptions.filter((field) => field.type === "date" || field.type === "datetime");
  const orderedLayout = orderDashboardLayoutWidgets(layoutWidgets);
  const builderConfig = {
    widgetType,
    metricType,
    metricFieldId,
    groupByFieldId,
    dateFieldId,
    columns: selectedColumns,
    limit: 10,
    reportId: selectedReportId || null
  };
  const canAddWidget = Boolean(selectedFormId && widgetTitle.trim()) && hasRequiredDashboardAnalyticsConfig(builderConfig);

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

  async function loadInitialData() {
    setLoading(true);
    setError(null);

    try {
      const [dashboardItems, formItems] = await Promise.all([listDashboards(), listForms()]);
      setDashboards(dashboardItems);
      setForms(formItems);
      setSelectedDashboardId((current) => current || dashboardItems[0]?.id || "");
      setSelectedFormId((current) => current || formItems[0]?.id || "");
    } catch (caught) {
      setError(getErrorMessage(caught));
    } finally {
      setLoading(false);
    }
  }

  async function loadDashboard(dashboardId: string) {
    setError(null);

    try {
      const detail = await getDashboard(dashboardId);
      setDashboardDetail(detail);
      setDashboardName(detail.name);
      setDashboardDescription(detail.description ?? "");
      setWidgets(detail.config.widgets);
      setLayoutWidgets(detail.layout.widgets);
      await loadPreviews(detail.config.widgets);
    } catch (caught) {
      setError(getErrorMessage(caught));
    }
  }

  async function loadPreviews(nextWidgets: SavedDashboardWidget[]) {
    if (nextWidgets.length === 0) {
      setPreviewStates({});
      return;
    }

    setPreviewStates(createDashboardPreviewStates(nextWidgets));

    await Promise.all(
      nextWidgets.map(async (widget) => {
        await refreshWidgetPreview(widget, false);
      })
    );
  }

  async function refreshWidgetPreview(widget: SavedDashboardWidget, setLoadingState = true) {
    if (setLoadingState) {
      setPreviewStates((current) => ({ ...current, [widget.id]: { status: "loading" } }));
    }

    try {
      const preview = await runDashboardAnalytics(buildDashboardAnalyticsRequest(widget.sourceFormId, widget.chart));
      setPreviewStates((current) => ({ ...current, [widget.id]: { status: "ready", preview } }));
    } catch (caught) {
      setPreviewStates((current) => ({ ...current, [widget.id]: { status: "error", error: getErrorMessage(caught) } }));
    }
  }

  function buildChartConfig(): ChartWidgetConfig {
    return buildChartConfigFromDashboardAnalytics(builderConfig);
  }

  async function handleAddWidget() {
    if (!canAddWidget) return;

    const id = `widget-${Date.now()}`;
    const chart = buildChartConfig();
    const widget = { id, title: widgetTitle.trim(), sourceFormId: selectedFormId, chart };

    setError(null);

    try {
      const preview = await runDashboardAnalytics(buildDashboardAnalyticsRequest(selectedFormId, chart));
      setWidgets((current) => [...current, widget]);
      setLayoutWidgets((current) => [...current, { id, width: widgetWidth, order: current.length + 1 }]);
      setPreviewStates((current) => ({ ...current, [id]: { status: "ready", preview } }));
      setNotice("Widget added. Save the dashboard to persist it.");
    } catch (caught) {
      setError(getErrorMessage(caught));
    }
  }

  function handleRemoveWidget(widgetId: string) {
    setWidgets((current) => current.filter((widget) => widget.id !== widgetId));
    setLayoutWidgets((current) => current.filter((item) => item.id !== widgetId).map((item, index) => ({ ...item, order: index + 1 })));
    setPreviewStates((current) => {
      const next = { ...current };
      delete next[widgetId];
      return next;
    });
  }

  function handleMoveWidget(widgetId: string, direction: -1 | 1) {
    setLayoutWidgets((current) => {
      const ordered = orderDashboardLayoutWidgets(current);
      const index = ordered.findIndex((item) => item.id === widgetId);
      const targetIndex = index + direction;
      if (index < 0 || targetIndex < 0 || targetIndex >= ordered.length) return current;
      const next = [...ordered];
      [next[index], next[targetIndex]] = [next[targetIndex], next[index]];
      return next.map((item, nextIndex) => ({ ...item, order: nextIndex + 1 }));
    });
  }

  async function handleSave() {
    setSaving(true);
    setError(null);
    setNotice(null);

    const request = {
      name: dashboardName,
      description: dashboardDescription || null,
      config: { schemaVersion: 1 as const, widgets },
      layout: { schemaVersion: 1 as const, widgets: layoutWidgets }
    };

    try {
      const saved = dashboardDetail
        ? await updateDashboard(dashboardDetail.id, { ...request, concurrencyStamp: dashboardDetail.concurrencyStamp })
        : await createDashboard(request);
      setDashboardDetail(saved);
      setSelectedDashboardId(saved.id);
      setWidgets(saved.config.widgets);
      setLayoutWidgets(saved.layout.widgets);
      setDashboards(await listDashboards());
      await loadPreviews(saved.config.widgets);
      setNotice("Dashboard saved.");
    } catch (caught) {
      setError(getErrorMessage(caught));
    } finally {
      setSaving(false);
    }
  }

  function handleNewDashboard() {
    setSelectedDashboardId("");
    setDashboardDetail(null);
    setDashboardName("Operations dashboard");
    setDashboardDescription("");
    setWidgets([]);
    setLayoutWidgets([]);
    setPreviewStates({});
    setNotice("New dashboard draft started.");
  }

  function handleSelectDashboard(dashboardId: string) {
    if (!dashboardId) {
      handleNewDashboard();
      return;
    }

    setSelectedDashboardId(dashboardId);
  }

  return (
    <div className="grid gap-6">
      <PageHeader
        eyebrow="Dashboards"
        title="Saved dashboards"
        description="Create reusable dashboards from permitted chart and report widgets."
        actions={
          <div className="flex flex-wrap gap-2">
            <Button onClick={() => void loadInitialData()} variant="outline">
              <RefreshCw className="size-4" />
              Refresh
            </Button>
            <Button onClick={handleNewDashboard} variant="outline">
              <Plus className="size-4" />
              New
            </Button>
            <Button disabled={saving || widgets.length === 0} onClick={() => void handleSave()}>
              <Save className="size-4" />
              {saving ? "Saving..." : "Save"}
            </Button>
          </div>
        }
      />

      {error ? <Alert title="Dashboards">{error}</Alert> : null}
      {notice ? <div className="rounded-xl border border-success/40 bg-success/10 px-4 py-3 text-sm font-semibold text-success">{notice}</div> : null}

      <section className="grid gap-4 xl:grid-cols-[20rem_minmax(0,1fr)]">
        <Card className="self-start">
          <CardHeader>
            <CardTitle>Dashboard</CardTitle>
            <CardDescription>Saved layout definition.</CardDescription>
          </CardHeader>
          <CardContent className="grid gap-4">
            <Select disabled={loading || dashboards.length === 0} label="Saved dashboard" onChange={(event) => handleSelectDashboard(event.target.value)} value={selectedDashboardId}>
              <option value="">New dashboard</option>
              {dashboards.map((dashboard) => (
                <option key={dashboard.id} value={dashboard.id}>
                  {dashboard.name}
                </option>
              ))}
            </Select>
            <Input label="Name" onChange={(event) => setDashboardName(event.target.value)} value={dashboardName} />
            <Input label="Description" onChange={(event) => setDashboardDescription(event.target.value)} value={dashboardDescription} />
            <Badge>{widgets.length} widgets</Badge>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Widget builder</CardTitle>
            <CardDescription>Add analytics widgets to the saved dashboard layout.</CardDescription>
          </CardHeader>
          <CardContent className="grid gap-4">
            <div className="grid gap-4 lg:grid-cols-3">
              <Select disabled={forms.length === 0} label="Form" onChange={(event) => setSelectedFormId(event.target.value)} value={selectedFormId}>
                {forms.map((form) => (
                  <option key={form.id} value={form.id}>
                    {form.name}
                  </option>
                ))}
              </Select>
              <Select disabled={!selectedFormId} label="Saved report filter" onChange={(event) => setSelectedReportId(event.target.value)} value={selectedReportId}>
                <option value="">All form records</option>
                {reports.map((report) => (
                  <option key={report.id} value={report.id}>
                    {report.name}
                  </option>
                ))}
              </Select>
              <Input label="Widget title" onChange={(event) => setWidgetTitle(event.target.value)} value={widgetTitle} />
            </div>
            <div className="grid gap-4 lg:grid-cols-4">
              <Select label="Widget" onChange={(event) => setWidgetType(event.target.value as DashboardAnalyticsWidgetType)} options={analyticsWidgetOptions} value={widgetType} />
              <Select label="Metric" onChange={(event) => setMetricType(event.target.value as ChartMetricType)} options={metricOptions} value={metricType} />
              <Select label="Width" onChange={(event) => setWidgetWidth(event.target.value as DashboardWidgetWidth)} options={widthOptions} value={widgetWidth} />
              <Button disabled={!canAddWidget} onClick={() => void handleAddWidget()}>
                <Plus className="size-4" />
                Add widget
              </Button>
            </div>
            {metricType !== "count" ? (
              <Select disabled={numericFields.length === 0} label="Numeric metric field" onChange={(event) => setMetricFieldId(event.target.value)} value={metricFieldId}>
                {numericFields.map((field) => (
                  <option key={field.id} value={field.id}>
                    {field.label}
                  </option>
                ))}
              </Select>
            ) : null}
            {widgetType === "breakdown" ? (
              <Select disabled={groupFields.length === 0} label="Group by" onChange={(event) => setGroupByFieldId(event.target.value)} value={groupByFieldId}>
                {groupFields.map((field) => (
                  <option key={field.id} value={field.id}>
                    {field.label}
                  </option>
                ))}
              </Select>
            ) : null}
            {widgetType === "trend" ? (
              <Select disabled={dateFields.length === 0} label="Trend date" onChange={(event) => setDateFieldId(event.target.value)} value={dateFieldId}>
                {dateFields.map((field) => (
                  <option key={field.id} value={field.id}>
                    {field.label}
                  </option>
                ))}
              </Select>
            ) : null}
            {widgetType === "table" ? (
              <div className="grid gap-2">
                <p className="text-sm font-bold text-muted-foreground">Table columns</p>
                <div className="grid gap-2 md:grid-cols-2 xl:grid-cols-3">
                  {fieldOptions.map((field) => (
                    <Checkbox
                      checked={selectedColumns.includes(field.id)}
                      key={field.id}
                      label={field.label}
                      onChange={(event) => handleToggleColumn(field.id, event.target.checked)}
                    />
                  ))}
                </div>
              </div>
            ) : null}
          </CardContent>
        </Card>
      </section>

      <section className="grid gap-4 md:grid-cols-12">
        {orderedLayout.length === 0 ? (
          <div className="md:col-span-12">
            <EmptyState title="No dashboard widgets" description="Add a widget and save the dashboard." action={<Button disabled={!canAddWidget} onClick={() => void handleAddWidget()} variant="outline">Add widget</Button>} />
          </div>
        ) : (
          orderedLayout.map((layout) => {
            const widget = widgets.find((candidate) => candidate.id === layout.id);
            const previewState = previewStates[layout.id];

            if (!widget) return null;

            const analyticsWidgetType = toDashboardAnalyticsWidgetType(widget.chart.widgetType);
            const statusTone = getPreviewStatusTone(previewState);

            return (
              <Card className={getDashboardWidgetGridClass(layout.width)} key={layout.id}>
                <CardHeader>
                  <div className="flex flex-wrap items-start justify-between gap-3">
                    <div className="min-w-0">
                      <CardTitle className="break-words text-base">{widget.title}</CardTitle>
                      <CardDescription className="break-words">
                        {getDashboardAnalyticsWidgetLabel(analyticsWidgetType)} · {getMetricLabel(widget.chart.metric.type)}
                      </CardDescription>
                    </div>
                    <div className="flex flex-wrap justify-end gap-2">
                      <Badge tone={statusTone}>{getPreviewStatusLabel(previewState)}</Badge>
                      {previewState?.status === "ready" && previewState.preview ? <Badge>{formatPreviewCount(previewState.preview)}</Badge> : null}
                      <Button
                        aria-label="Refresh widget preview"
                        disabled={previewState?.status === "loading"}
                        onClick={() => void refreshWidgetPreview(widget)}
                        size="icon"
                        variant="outline"
                      >
                        <RefreshCw className={previewState?.status === "loading" ? "size-4 animate-spin" : "size-4"} />
                      </Button>
                      <Button aria-label="Move widget up" onClick={() => handleMoveWidget(layout.id, -1)} size="icon" variant="outline">
                        <ArrowUp className="size-4" />
                      </Button>
                      <Button aria-label="Move widget down" onClick={() => handleMoveWidget(layout.id, 1)} size="icon" variant="outline">
                        <ArrowDown className="size-4" />
                      </Button>
                      <Button aria-label="Remove widget" onClick={() => handleRemoveWidget(layout.id)} size="icon" variant="outline">
                        <Trash2 className="size-4" />
                      </Button>
                    </div>
                  </div>
                </CardHeader>
                <CardContent className="min-w-0">
                  <DashboardWidgetPreviewStateView state={previewState} onRefresh={() => void refreshWidgetPreview(widget)} />
                </CardContent>
              </Card>
            );
          })
        )}
      </section>
    </div>
  );

  function handleToggleColumn(fieldId: string, selected: boolean) {
    setSelectedColumns((current) => {
      if (selected) {
        return current.includes(fieldId) ? current : [...current, fieldId];
      }

      return current.filter((currentFieldId) => currentFieldId !== fieldId);
    });
  }
}

function getErrorMessage(error: unknown): string {
  return error instanceof Error ? error.message : "Dashboard request failed.";
}

function DashboardWidgetPreviewStateView({ state, onRefresh }: { state?: DashboardPreviewState; onRefresh: () => void }) {
  if (state?.status === "ready" && state.preview) {
    return <ChartWidgetPreview preview={state.preview} />;
  }

  if (state?.status === "loading") {
    return (
      <div className="rounded-lg border border-border bg-muted/30 p-4">
        <div className="flex items-center gap-3">
          <RefreshCw className="size-4 animate-spin text-muted-foreground" />
          <div className="min-w-0">
            <p className="text-sm font-bold text-foreground">Loading widget</p>
            <p className="mt-1 text-sm leading-6 text-muted-foreground">Refreshing analytics from the saved source.</p>
          </div>
        </div>
      </div>
    );
  }

  if (state?.status === "error") {
    const message = state.error ?? "Dashboard request failed.";

    return (
      <div className="rounded-lg border border-danger/25 bg-danger-soft p-4">
        <div className="grid gap-3 sm:grid-cols-[minmax(0,1fr)_auto] sm:items-start">
          <div className="min-w-0">
            <p className="text-sm font-bold text-danger">{getPreviewErrorTitle(message)}</p>
            <p className="mt-1 break-words text-sm leading-6 text-muted-foreground">{message}</p>
          </div>
          <Button onClick={onRefresh} size="sm" variant="outline">
            <RefreshCw className="size-4" />
            Retry
          </Button>
        </div>
      </div>
    );
  }

  return (
    <EmptyState
      title="Preview unavailable"
      description="Refresh this widget to render the saved analytics request."
      action={
        <Button onClick={onRefresh} variant="outline">
          <RefreshCw className="size-4" />
          Refresh preview
        </Button>
      }
    />
  );
}

function getPreviewStatusTone(state?: DashboardPreviewState): "default" | "info" | "success" | "warning" | "danger" {
  switch (state?.status) {
    case "ready":
      return "success";
    case "loading":
      return "info";
    case "error":
      return "danger";
    default:
      return "warning";
  }
}

function getPreviewStatusLabel(state?: DashboardPreviewState): string {
  switch (state?.status) {
    case "ready":
      return "Ready";
    case "loading":
      return "Loading";
    case "error":
      return "Needs review";
    default:
      return "Stale";
  }
}

function getMetricLabel(metricType: ChartMetricType): string {
  switch (metricType) {
    case "count":
      return "Count";
    case "sum":
      return "Sum";
    case "average":
      return "Average";
  }
}

function formatPreviewCount(preview: DashboardAnalyticsResponse): string {
  if (preview.widgetType === "table") {
    return `${preview.rows.length} rows`;
  }

  return `${preview.totalCount} records`;
}

function getPreviewErrorTitle(message: string): string {
  const normalized = message.toLowerCase();

  if (normalized.includes("permission") || normalized.includes("access") || normalized.includes("denied") || normalized.includes("forbidden")) {
    return "Permission denied";
  }

  if (normalized.includes("field") || normalized.includes("schema") || normalized.includes("report") || normalized.includes("source")) {
    return "Widget source needs review";
  }

  return "Preview failed";
}

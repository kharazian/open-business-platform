import { useEffect, useMemo, useState } from "react";
import { ArrowDown, ArrowUp, Plus, RefreshCw, Save, Trash2 } from "lucide-react";
import { Alert } from "../../../components/ui/Alert";
import { Badge } from "../../../components/ui/Badge";
import { Button } from "../../../components/ui/Button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "../../../components/ui/Card";
import { EmptyState } from "../../../components/ui/EmptyState";
import { Input } from "../../../components/ui/Input";
import { PageHeader } from "../../../components/ui/PageHeader";
import { Select } from "../../../components/ui/Select";
import { getForm, listForms, type FormDetail } from "../../forms/api";
import type { FormSummary } from "../../forms/drafts";
import { getReportableFields } from "../../forms/reportableFields";
import { listReports } from "../../reports/api";
import type { ListReportSummary } from "../../reports/types";
import { createDashboard, getDashboard, listDashboards, previewChartWidget, updateDashboard } from "../api";
import { ChartWidgetPreview } from "../components/ChartWidgetPreview";
import { getDashboardWidgetGridClass, orderDashboardLayoutWidgets } from "../layout";
import {
  dashboardWidgetWidths,
  type ChartMetricType,
  type ChartWidgetConfig,
  type ChartWidgetPreview as ChartWidgetPreviewData,
  type ChartWidgetType,
  type DashboardDetail,
  type DashboardSummaryItem,
  type DashboardWidgetWidth,
  type SavedDashboardWidget,
  type SavedDashboardWidgetLayout
} from "../types";

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

const widthOptions = dashboardWidgetWidths.map((width) => ({ label: width, value: width }));

export function DashboardsPage() {
  const [dashboards, setDashboards] = useState<DashboardSummaryItem[]>([]);
  const [selectedDashboardId, setSelectedDashboardId] = useState("");
  const [dashboardDetail, setDashboardDetail] = useState<DashboardDetail | null>(null);
  const [forms, setForms] = useState<FormSummary[]>([]);
  const [formDetail, setFormDetail] = useState<FormDetail | null>(null);
  const [reports, setReports] = useState<ListReportSummary[]>([]);
  const [previews, setPreviews] = useState<Record<string, ChartWidgetPreviewData | undefined>>({});
  const [dashboardName, setDashboardName] = useState("Operations dashboard");
  const [dashboardDescription, setDashboardDescription] = useState("");
  const [selectedFormId, setSelectedFormId] = useState("");
  const [selectedReportId, setSelectedReportId] = useState("");
  const [widgetTitle, setWidgetTitle] = useState("New widget");
  const [widgetType, setWidgetType] = useState<ChartWidgetType>("number_card");
  const [metricType, setMetricType] = useState<ChartMetricType>("count");
  const [metricFieldId, setMetricFieldId] = useState("");
  const [groupByFieldId, setGroupByFieldId] = useState("status");
  const [dateFieldId, setDateFieldId] = useState("created_at");
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
    const results = await Promise.all(
      nextWidgets.map(async (widget) => {
        const preview = await previewChartWidget(widget.sourceFormId, widget.chart);
        return [widget.id, preview] as const;
      })
    );
    setPreviews(Object.fromEntries(results));
  }

  function buildChartConfig(): ChartWidgetConfig {
    return {
      widgetType,
      metric: { type: metricType, fieldId: metricType === "count" ? null : metricFieldId || null },
      groupByFieldId: widgetType === "bar_chart" || widgetType === "choice_breakdown" ? groupByFieldId || null : null,
      dateFieldId: widgetType === "date_trend" ? dateFieldId || null : null,
      columns: widgetType === "table" ? fieldOptions.slice(0, 5).map((field) => field.id) : [],
      limit: 10,
      reportId: selectedReportId || null
    };
  }

  function handleAddWidget() {
    if (!selectedFormId || !widgetTitle.trim()) return;
    const id = `widget-${Date.now()}`;
    setWidgets((current) => [...current, { id, title: widgetTitle.trim(), sourceFormId: selectedFormId, chart: buildChartConfig() }]);
    setLayoutWidgets((current) => [...current, { id, width: widgetWidth, order: current.length + 1 }]);
    setNotice("Widget added. Save the dashboard to persist it.");
  }

  function handleRemoveWidget(widgetId: string) {
    setWidgets((current) => current.filter((widget) => widget.id !== widgetId));
    setLayoutWidgets((current) => current.filter((item) => item.id !== widgetId).map((item, index) => ({ ...item, order: index + 1 })));
    setPreviews((current) => {
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
    setPreviews({});
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
        eyebrow="Dashboards V2 preview"
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
            <CardDescription>Add chart widgets to the saved dashboard layout.</CardDescription>
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
              <Select label="Widget" onChange={(event) => setWidgetType(event.target.value as ChartWidgetType)} options={widgetOptions} value={widgetType} />
              <Select label="Metric" onChange={(event) => setMetricType(event.target.value as ChartMetricType)} options={metricOptions} value={metricType} />
              <Select label="Width" onChange={(event) => setWidgetWidth(event.target.value as DashboardWidgetWidth)} options={widthOptions} value={widgetWidth} />
              <Button disabled={!selectedFormId || !widgetTitle.trim()} onClick={handleAddWidget}>
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
            {widgetType === "bar_chart" || widgetType === "choice_breakdown" ? (
              <Select disabled={groupFields.length === 0} label="Group by" onChange={(event) => setGroupByFieldId(event.target.value)} value={groupByFieldId}>
                {groupFields.map((field) => (
                  <option key={field.id} value={field.id}>
                    {field.label}
                  </option>
                ))}
              </Select>
            ) : null}
            {widgetType === "date_trend" ? (
              <Select disabled={dateFields.length === 0} label="Trend date" onChange={(event) => setDateFieldId(event.target.value)} value={dateFieldId}>
                {dateFields.map((field) => (
                  <option key={field.id} value={field.id}>
                    {field.label}
                  </option>
                ))}
              </Select>
            ) : null}
          </CardContent>
        </Card>
      </section>

      <section className="grid gap-4 md:grid-cols-12">
        {orderedLayout.length === 0 ? (
          <div className="md:col-span-12">
            <EmptyState title="No dashboard widgets" description="Add a widget and save the dashboard." action={<Button onClick={handleAddWidget} variant="outline">Add widget</Button>} />
          </div>
        ) : (
          orderedLayout.map((layout) => {
            const widget = widgets.find((candidate) => candidate.id === layout.id);
            const preview = previews[layout.id];

            if (!widget) return null;

            return (
              <Card className={getDashboardWidgetGridClass(layout.width)} key={layout.id}>
                <CardHeader>
                  <div className="flex flex-wrap items-start justify-between gap-3">
                    <div>
                      <CardTitle>{widget.title}</CardTitle>
                      <CardDescription>{widget.chart.widgetType.replace(/_/g, " ")}</CardDescription>
                    </div>
                    <div className="flex gap-2">
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
                <CardContent>
                  {preview ? (
                    <ChartWidgetPreview preview={preview} />
                  ) : (
                    <EmptyState title="Preview unavailable" description="Save or refresh this dashboard to render the widget." action={<Button onClick={() => void loadPreviews(widgets)} variant="outline">Refresh preview</Button>} />
                  )}
                </CardContent>
              </Card>
            );
          })
        )}
      </section>
    </div>
  );
}

function getErrorMessage(error: unknown): string {
  return error instanceof Error ? error.message : "Dashboard request failed.";
}

import { useEffect, useMemo, useRef, useState } from "react";
import { ArrowDown, ArrowUp, Eye, LoaderCircle, Plus, RotateCcw, Save, Trash2, X } from "lucide-react";
import { Alert } from "../../../components/ui/Alert";
import { Badge } from "../../../components/ui/Badge";
import { Button } from "../../../components/ui/Button";
import { Checkbox } from "../../../components/ui/Checkbox";
import { Input } from "../../../components/ui/Input";
import { Select } from "../../../components/ui/Select";
import { getForm } from "../../forms/api";
import type { FormSummary } from "../../forms/drafts";
import { getReportableFields, type ReportableField } from "../../forms/reportableFields";
import { listReports } from "../../reports/api";
import type { ListReportSummary } from "../../reports/types";
import { runDashboardAnalytics } from "../api";
import { buildDashboardAnalyticsRequest, toDashboardAnalyticsWidgetType } from "../analytics";
import { defaultDashboardChartAppearance, getDashboardAccentColor, resolveDashboardChartAppearance } from "../appearance";
import { createDashboardAdapterWidget, isDashboardAdapterWidgetConfigured } from "../adapters";
import type { DashboardAdapterRegistration, DashboardAnalyticsResponse, DashboardAnalyticsWidgetType, DashboardCardAccent, DashboardChartAppearance, DashboardChartPalette, DashboardChartSeriesDefinition, DashboardNumberFormat, DashboardSeriesAxis, DashboardSeriesColor, DashboardSeriesDisplayType, DashboardWidgetWidth, SavedDashboardSection, SavedDashboardWidget, SavedDashboardWidgetLayout } from "../types";
import { ChartWidgetPreview } from "./ChartWidgetPreview";
import { DashboardAdapterSettingsEditor } from "./DashboardAdapterSettingsEditor";

const widgetTypes: Array<{ label: string; value: DashboardAnalyticsWidgetType }> = [
  { label: "KPI / summary", value: "summary" }, { label: "Category breakdown", value: "breakdown" },
  { label: "Time trend", value: "trend" }, { label: "Record table", value: "table" }
];
const metricTypes = [{ label: "Count records", value: "count" }, { label: "Sum a numeric field", value: "sum" }, { label: "Average a numeric field", value: "average" }];
const widths: Array<{ label: string; value: DashboardWidgetWidth }> = [
  { label: "Small · ¼ row", value: "small" }, { label: "Medium · ½ row", value: "medium" },
  { label: "Wide · ¾ row", value: "wide" }, { label: "Full row", value: "full" }
];
const displayTypes = [{ label: "Bar", value: "bar" }, { label: "Line", value: "line" }, { label: "Area", value: "area" }];
const seriesColors = [{ label: "Blue", value: "primary" }, { label: "Cyan", value: "info" }, { label: "Green", value: "success" }, { label: "Amber", value: "warning" }, { label: "Red", value: "danger" }, { label: "Violet", value: "violet" }];
const seriesAxes = [{ label: "Left axis", value: "left" }, { label: "Right axis", value: "right" }];
const palettes = [{ label: "Follow app theme", value: "theme" }, { label: "Cool", value: "cool" }, { label: "Warm", value: "warm" }, { label: "Monochrome", value: "mono" }];
const numberFormats = [{ label: "Automatic", value: "auto" }, { label: "Number", value: "number" }, { label: "Currency", value: "currency" }, { label: "Percent", value: "percent" }];
const cardAccents = [{ label: "None", value: "none" }, ...seriesColors];
const currencies = ["CAD", "USD", "EUR", "GBP"].map((value) => ({ label: value, value }));

export function DashboardWidgetPropertiesDrawer({ adapters, forms, layout, onApply, onClose, open, sections, widget }: {
  adapters: DashboardAdapterRegistration[]; forms: FormSummary[]; layout: SavedDashboardWidgetLayout | null;
  onApply: (widget: SavedDashboardWidget, width: DashboardWidgetWidth, preview?: DashboardAnalyticsResponse) => void;
  onClose: () => void; open: boolean; sections: SavedDashboardSection[]; widget: SavedDashboardWidget | null;
}) {
  const [draft, setDraft] = useState<SavedDashboardWidget | null>(null);
  const [width, setWidth] = useState<DashboardWidgetWidth>("medium");
  const [fields, setFields] = useState<ReportableField[]>([]);
  const [reports, setReports] = useState<ListReportSummary[]>([]);
  const [loadingSource, setLoadingSource] = useState(false);
  const [preview, setPreview] = useState<DashboardAnalyticsResponse | null>(null);
  const [previewError, setPreviewError] = useState<string | null>(null);
  const [previewing, setPreviewing] = useState(false);
  const previewSequence = useRef(0);

  useEffect(() => { if (open && widget) { setDraft(cloneDashboardWidgetForEditing(widget)); setWidth(layout?.width ?? "medium"); setPreview(null); setPreviewError(null); } }, [layout?.width, open, widget]);
  useEffect(() => {
    if (!open || !draft?.chart || !draft.sourceFormId) { setFields([]); setReports([]); return; }
    let active = true; setLoadingSource(true); setFields([]); setReports([]);
    Promise.all([getForm(draft.sourceFormId), listReports(draft.sourceFormId)]).then(([form, items]) => {
      if (!active) return; setFields(getReportableFields(form.draftSchema)); setReports(items);
    }).catch((error: unknown) => { if (active) setPreviewError(error instanceof Error ? error.message : "Could not load source fields."); }).finally(() => { if (active) setLoadingSource(false); });
    return () => { active = false; };
  }, [draft?.sourceFormId, open]);

  const dirty = Boolean(widget && draft && (JSON.stringify(widget) !== JSON.stringify(draft) || layout?.width !== width));
  const numericFields = fields.filter((field) => field.supportsAggregation);
  const groupFields = fields.filter((field) => field.supportsChoiceGrouping);
  const dateFields = fields.filter((field) => field.type === "date" || field.type === "datetime");
  const adapter = draft?.adapter ? adapters.find((item) => item.id === draft.adapter?.adapterId) : undefined;
  const effectiveSeries = useMemo(() => getEffectiveSeries(draft), [draft]);
  const appearance = useMemo(() => resolveDashboardChartAppearance(draft?.chart?.appearance), [draft?.chart?.appearance]);
  const valid = Boolean(draft?.title.trim() && draft.sectionId && (draft.chart ? draft.sourceFormId && !loadingSource && isDashboardAnalyticsWidgetDraftValid(draft, fields) : isDashboardAdapterWidgetConfigured(adapter, draft?.adapter ?? null)));

  useEffect(() => {
    if (!open || !draft?.chart || !draft.sourceFormId || !valid) { setPreview(null); return; }
    const sequence = ++previewSequence.current;
    const timer = window.setTimeout(() => {
      setPreviewing(true); setPreviewError(null);
      runDashboardAnalytics(buildDashboardAnalyticsRequest(draft.sourceFormId!, draft.chart!)).then((result) => {
        if (previewSequence.current === sequence) setPreview(result);
      }).catch((error: unknown) => { if (previewSequence.current === sequence) setPreviewError(error instanceof Error ? error.message : "Preview failed."); })
        .finally(() => { if (previewSequence.current === sequence) setPreviewing(false); });
    }, 350);
    return () => window.clearTimeout(timer);
  }, [draft, open, valid]);

  useEffect(() => {
    if (!open) return;
    const close = (event: KeyboardEvent) => { if (event.key === "Escape") requestClose(); };
    window.addEventListener("keydown", close); return () => window.removeEventListener("keydown", close);
  });

  const analyticsType = useMemo(() => draft?.chart ? toDashboardAnalyticsWidgetType(draft.chart.widgetType) : null, [draft?.chart]);
  if (!open || !draft || !widget) return null;
  const activeDraft = draft;

  function requestClose() { if (!dirty || window.confirm("Discard unsaved widget property changes?")) onClose(); }
  function update(next: Partial<SavedDashboardWidget>) { setDraft((current) => current ? { ...current, ...next } : current); }
  function updateChart(next: Partial<NonNullable<SavedDashboardWidget["chart"]>>) { setDraft((current) => current?.chart ? { ...current, chart: { ...current.chart, ...next } } : current); }
  function updateAppearance(next: Partial<DashboardChartAppearance>) { updateChart({ appearance: { ...appearance, ...next } }); }
  function changeType(next: DashboardAnalyticsWidgetType) {
    if (!activeDraft.chart) return;
    updateChart({ widgetType: next === "summary" ? "number_card" : next === "breakdown" ? "choice_breakdown" : next === "trend" ? "date_trend" : "table", groupByFieldId: next === "breakdown" ? (activeDraft.chart.groupByFieldId || groupFields[0]?.id || null) : null, dateFieldId: next === "trend" ? (activeDraft.chart.dateFieldId || dateFields[0]?.id || null) : null, columns: next === "table" ? (activeDraft.chart.columns?.length ? activeDraft.chart.columns : fields.slice(0, 5).map((field) => field.id)) : [], series: next === "table" ? null : activeDraft.chart.series });
  }
  function changeAdapterVisualization(visualizationId: string) { if (!adapter) return; const next = createDashboardAdapterWidget(adapter, visualizationId); if (next) update({ adapter: next }); }
  function toggleColumn(fieldId: string, selected: boolean) { const current = activeDraft.chart?.columns ?? []; updateChart({ columns: selected ? [...new Set([...current, fieldId])] : current.filter((id) => id !== fieldId) }); }
  function setSeries(series: DashboardChartSeriesDefinition[]) { updateChart({ series, metric: { ...series[0].metric } }); }
  function updateSeries(index: number, next: Partial<DashboardChartSeriesDefinition>) { setSeries(effectiveSeries.map((series, current) => current === index ? { ...series, ...next, metric: next.metric ? { ...next.metric } : series.metric } : series)); }
  function addSeries() { if (effectiveSeries.length >= 4 || !activeDraft.chart) return; const index = effectiveSeries.length; setSeries([...effectiveSeries, { id: `series-${Date.now()}`, label: `Series ${index + 1}`, metric: { type: "count", fieldId: null }, displayType: index % 2 ? "line" : "bar", color: (["primary", "info", "success", "warning"] as DashboardSeriesColor[])[index], axis: "left" }]); }
  function removeSeries(index: number) { if (effectiveSeries.length <= 1) return; setSeries(effectiveSeries.filter((_, current) => current !== index)); }
  function moveSeries(index: number, direction: -1 | 1) { const target = index + direction; if (target < 0 || target >= effectiveSeries.length) return; const next = [...effectiveSeries]; [next[index], next[target]] = [next[target], next[index]]; setSeries(next); }

  return <div className="fixed inset-0 z-50 bg-foreground/30 backdrop-blur-[2px]" onMouseDown={(event) => { if (event.target === event.currentTarget) requestClose(); }}>
    <aside aria-describedby="widget-properties-description" aria-label="Widget properties" aria-modal="true" className="absolute inset-y-0 right-0 flex w-full max-w-2xl flex-col border-l border-border bg-background shadow-2xl" role="dialog">
      <header className="flex items-start justify-between gap-4 border-b border-border p-5"><div><div className="flex flex-wrap items-center gap-2"><h2 className="text-xl font-bold">Widget properties</h2><Badge tone={draft.chart ? "info" : "success"}>{draft.chart ? "Analytics" : "Adapter"}</Badge></div><p className="mt-1 text-sm text-muted-foreground" id="widget-properties-description">Edit the selected widget and review the preview before applying.</p></div><Button aria-label="Close widget properties" onClick={requestClose} size="icon" variant="ghost"><X className="size-5" /></Button></header>
      <div className="grid min-h-0 flex-1 gap-6 overflow-y-auto p-5">
        {dirty ? <div className="rounded-lg border border-warning/40 bg-warning/10 px-3 py-2 text-sm font-semibold text-warning">Unsaved property changes</div> : null}
        <section className="grid gap-4"><h3 className="font-bold">Content and layout</h3><div className="grid gap-4 sm:grid-cols-2"><Input label="Widget title" maxLength={160} onChange={(event) => update({ title: event.target.value })} value={draft.title} /><Input label="Subtitle (optional)" maxLength={300} onChange={(event) => update({ subtitle: event.target.value || null })} value={draft.subtitle ?? ""} /><Select label="Section" onChange={(event) => update({ sectionId: event.target.value })} options={sections.map((section) => ({ label: section.title, value: section.id }))} value={draft.sectionId ?? ""} /><Select label="Card width" onChange={(event) => setWidth(event.target.value as DashboardWidgetWidth)} options={widths} value={width} /></div></section>
        {draft.chart ? <section className="grid gap-4 border-t border-border pt-5">
          <h3 className="font-bold">Data and chart</h3>
          <div className="grid gap-4 sm:grid-cols-2">
            <Select disabled={loadingSource} label="Source form" onChange={(event) => update({ sourceFormId: event.target.value, chart: { ...draft.chart!, reportId: null } })} value={draft.sourceFormId ?? ""}>{forms.map((form) => <option key={form.id} value={form.id}>{form.name}</option>)}</Select>
            <Select disabled={loadingSource} label="Saved report filter" onChange={(event) => updateChart({ reportId: event.target.value || null })} value={draft.chart.reportId ?? ""}><option value="">All permitted records</option>{reports.map((report) => <option key={report.id} value={report.id}>{report.name}</option>)}</Select>
            <Select label="Visualization" onChange={(event) => changeType(event.target.value as DashboardAnalyticsWidgetType)} options={widgetTypes} value={analyticsType ?? "summary"} />
            {analyticsType === "breakdown" ? <Select disabled={loadingSource || groupFields.length === 0} label="Group by" onChange={(event) => updateChart({ groupByFieldId: event.target.value })} value={draft.chart.groupByFieldId ?? ""}>{groupFields.map((field) => <option key={field.id} value={field.id}>{field.label}</option>)}</Select> : null}
            {analyticsType === "trend" ? <Select disabled={loadingSource || dateFields.length === 0} label="Date axis" onChange={(event) => updateChart({ dateFieldId: event.target.value })} value={draft.chart.dateFieldId ?? ""}>{dateFields.map((field) => <option key={field.id} value={field.id}>{field.label}</option>)}</Select> : null}
            <Input label="Result limit" max={50} min={1} onChange={(event) => updateChart({ limit: Math.max(1, Math.min(50, Number(event.target.value) || 1)) })} type="number" value={draft.chart.limit ?? 10} />
          </div>
          {analyticsType !== "table" ? <div className="grid gap-3 rounded-xl border border-border bg-muted/10 p-3">
            <div className="flex items-center justify-between gap-3"><div><p className="text-sm font-bold">Series</p><p className="text-xs text-muted-foreground">Up to four permission-checked metrics from this source.</p></div><Button disabled={effectiveSeries.length >= 4} onClick={addSeries} size="sm" variant="outline"><Plus className="size-4" />Add series</Button></div>
            {effectiveSeries.map((series, index) => <div className="grid gap-3 rounded-lg border border-border bg-card p-3" key={series.id}>
              <div className="flex items-center justify-between gap-2"><Badge tone="info">Series {index + 1}</Badge><div className="flex gap-1"><Button aria-label={`Move ${series.label} up`} disabled={index === 0} onClick={() => moveSeries(index, -1)} size="icon" variant="ghost"><ArrowUp className="size-4" /></Button><Button aria-label={`Move ${series.label} down`} disabled={index === effectiveSeries.length - 1} onClick={() => moveSeries(index, 1)} size="icon" variant="ghost"><ArrowDown className="size-4" /></Button><Button aria-label={`Remove ${series.label}`} disabled={effectiveSeries.length === 1} onClick={() => removeSeries(index)} size="icon" variant="ghost"><Trash2 className="size-4" /></Button></div></div>
              <div className="grid gap-3 sm:grid-cols-2"><Input label="Series label" maxLength={80} onChange={(event) => updateSeries(index, { label: event.target.value })} value={series.label} /><Select label="Aggregation" onChange={(event) => updateSeries(index, { metric: { type: event.target.value as "count" | "sum" | "average", fieldId: event.target.value === "count" ? null : (series.metric.fieldId || numericFields[0]?.id || null) } })} options={metricTypes} value={series.metric.type} />{series.metric.type !== "count" ? <Select disabled={loadingSource || numericFields.length === 0} label="Numeric field" onChange={(event) => updateSeries(index, { metric: { ...series.metric, fieldId: event.target.value } })} value={series.metric.fieldId ?? ""}>{numericFields.map((field) => <option key={field.id} value={field.id}>{field.label}</option>)}</Select> : null}<Select label="Display" onChange={(event) => updateSeries(index, { displayType: event.target.value as DashboardSeriesDisplayType })} options={displayTypes} value={series.displayType} /><Select label="Color" onChange={(event) => updateSeries(index, { color: event.target.value as DashboardSeriesColor })} options={seriesColors} value={series.color} /><Select label="Axis" onChange={(event) => updateSeries(index, { axis: event.target.value as DashboardSeriesAxis })} options={seriesAxes} value={series.axis} /></div>
            </div>)}
          </div> : <div className="grid gap-2"><p className="text-sm font-bold">Table columns</p><div className="grid gap-2 sm:grid-cols-2">{fields.map((field) => <Checkbox checked={(draft.chart?.columns ?? []).includes(field.id)} key={field.id} label={field.label} onChange={(event) => toggleColumn(field.id, event.target.checked)} />)}</div></div>}
          {analyticsType !== "table" ? <div className="grid gap-4 rounded-xl border border-border bg-muted/10 p-3"><div className="flex items-center justify-between gap-3"><div><p className="text-sm font-bold">Appearance and formatting</p><p className="text-xs text-muted-foreground">Accessible presets adapt chart presentation without changing its data.</p></div><Button onClick={() => updateChart({ appearance: { ...defaultDashboardChartAppearance } })} size="sm" variant="outline"><RotateCcw className="size-4" />Reset to theme</Button></div><div className="grid gap-3 sm:grid-cols-2"><Select label="Palette" onChange={(event) => updateAppearance({ palette: event.target.value as DashboardChartPalette })} options={palettes} value={appearance.palette} /><Select label="Card accent" onChange={(event) => updateAppearance({ cardAccent: event.target.value as DashboardCardAccent })} options={cardAccents} value={appearance.cardAccent} /><Select label="Number format" onChange={(event) => updateAppearance({ numberFormat: event.target.value as DashboardNumberFormat })} options={numberFormats} value={appearance.numberFormat} />{appearance.numberFormat === "currency" ? <Select label="Currency" onChange={(event) => updateAppearance({ currencyCode: event.target.value })} options={currencies} value={appearance.currencyCode} /> : null}{appearance.numberFormat !== "auto" ? <Input label="Decimal places" max={4} min={0} onChange={(event) => updateAppearance({ decimalPlaces: Math.max(0, Math.min(4, Number(event.target.value) || 0)) })} type="number" value={appearance.decimalPlaces} /> : null}</div><div className="grid gap-2 sm:grid-cols-3"><Checkbox checked={appearance.showLegend} label="Show legend" onChange={(event) => updateAppearance({ showLegend: event.target.checked })} /><Checkbox checked={appearance.showDataLabels} label="Show data labels" onChange={(event) => updateAppearance({ showDataLabels: event.target.checked })} /><Checkbox checked={appearance.showGridlines} label="Show gridlines" onChange={(event) => updateAppearance({ showGridlines: event.target.checked })} /></div></div> : null}
        </section> : adapter ? <section className="grid gap-4 border-t border-border pt-5"><h3 className="font-bold">Visualization properties</h3><Select label="Visualization" onChange={(event) => changeAdapterVisualization(event.target.value)} options={adapter.visualizations.map((item) => ({ label: item.name, value: item.id }))} value={draft.adapter!.visualizationId} /><DashboardAdapterSettingsEditor adapter={adapter} onChange={(next) => update({ adapter: next })} value={draft.adapter!} /></section> : <Alert title="Adapter unavailable">This widget's adapter is not installed.</Alert>}
        <section className="grid gap-3 border-t border-border pt-5" style={{ borderTopColor: getDashboardAccentColor(appearance.cardAccent, appearance.palette) }}><div className="flex items-center justify-between"><h3 className="flex items-center gap-2 font-bold"><Eye className="size-4" />Live preview</h3>{previewing ? <Badge><LoaderCircle className="size-3 animate-spin" />Refreshing</Badge> : null}</div>{previewError ? <Alert title="Preview unavailable">{previewError}</Alert> : draft.chart ? (preview ? <ChartWidgetPreview appearance={appearance} preview={preview} /> : <p className="text-sm text-muted-foreground">Complete the required properties to generate a preview.</p>) : adapter ? <adapter.render widget={draft} /> : null}</section>
      </div>
      <footer className="flex flex-wrap items-center justify-between gap-3 border-t border-border bg-card p-4"><p className="text-xs text-muted-foreground">Changes update the canvas first. Use Save to persist the dashboard.</p><div className="flex gap-2"><Button onClick={requestClose} variant="outline">Cancel</Button><Button disabled={!dirty || !valid || previewing} onClick={() => onApply(draft, width, preview ?? undefined)}><Save className="size-4" />Apply changes</Button></div></footer>
    </aside>
  </div>;
}

export function cloneDashboardWidgetForEditing(widget: SavedDashboardWidget): SavedDashboardWidget { return { ...widget, chart: widget.chart ? { ...widget.chart, metric: { ...widget.chart.metric }, columns: [...(widget.chart.columns ?? [])], series: widget.chart.series?.map((series) => ({ ...series, metric: { ...series.metric } })) ?? null, appearance: widget.chart.appearance ? { ...widget.chart.appearance } : null } : null, adapter: widget.adapter ? { ...widget.adapter, settings: { ...widget.adapter.settings } } : null }; }
export function isDashboardAnalyticsWidgetDraftValid(widget: SavedDashboardWidget, fields: ReportableField[]) {
  const chart = widget.chart;
  if (!chart || fields.length === 0) return false;
  const ids = new Set(fields.map((field) => field.id));
  const type = toDashboardAnalyticsWidgetType(chart.widgetType);
  const series = getEffectiveSeries(widget);
  const validSeries = series.length > 0 && series.length <= 4 && series.every((item) => item.label.trim() && (item.metric.type === "count" || Boolean(item.metric.fieldId && ids.has(item.metric.fieldId))));
  return validSeries && (type !== "table" || series.length === 1) && (chart.metric.type === "count" || Boolean(chart.metric.fieldId && ids.has(chart.metric.fieldId))) && (type !== "breakdown" || Boolean(chart.groupByFieldId && ids.has(chart.groupByFieldId))) && (type !== "trend" || Boolean(chart.dateFieldId && ids.has(chart.dateFieldId))) && (type !== "table" || Boolean(chart.columns?.length && chart.columns.every((id) => ids.has(id))));
}
function getEffectiveSeries(widget: SavedDashboardWidget | null): DashboardChartSeriesDefinition[] { if (!widget?.chart) return []; return widget.chart.series?.length ? widget.chart.series : [{ id: "primary", label: "Primary", metric: { ...widget.chart.metric }, displayType: "bar", color: "primary", axis: "left" }]; }

import { useEffect, useMemo, useState } from "react";
import { ArrowLeft, ArrowRight, BarChart3, Check, Clock3, Hash, LayoutList, Plus, Search, Table2, TrendingUp } from "lucide-react";
import { Alert } from "../../../components/ui/Alert";
import { Badge } from "../../../components/ui/Badge";
import { Button } from "../../../components/ui/Button";
import { Checkbox } from "../../../components/ui/Checkbox";
import { Input } from "../../../components/ui/Input";
import { Select } from "../../../components/ui/Select";
import type { FormSummary } from "../../forms/drafts";
import type { ReportableField } from "../../forms/reportableFields";
import type { ListReportSummary } from "../../reports/types";
import { runDashboardAnalytics } from "../api";
import { buildChartConfigFromDashboardAnalytics, buildDashboardAnalyticsRequest, hasRequiredDashboardAnalyticsConfig } from "../analytics";
import { dashboardVisualizationCatalog, filterDashboardVisualizations, getVisualizationAvailability, readRecentDashboardVisualizations, saveRecentDashboardVisualization } from "../addWidgetWizard";
import { dashboardWidgetWidths, type ChartMetricType, type DashboardAnalyticsResponse, type DashboardAnalyticsWidgetType, type DashboardWidgetWidth, type SavedDashboardSection } from "../types";
import { ChartWidgetPreview } from "./ChartWidgetPreview";

type Props = {
  canAdd: boolean; dateFieldId: string; fields: ReportableField[]; forms: FormSummary[]; groupByFieldId: string; metricFieldId: string; metricType: ChartMetricType;
  onAdd: () => Promise<boolean>; onColumnsChange: (fieldId: string, selected: boolean) => void; onDateFieldChange: (value: string) => void; onFormChange: (value: string) => void;
  onGroupFieldChange: (value: string) => void; onMetricFieldChange: (value: string) => void; onMetricTypeChange: (value: ChartMetricType) => void; onReportChange: (value: string) => void;
  onSectionChange: (value: string) => void; onTitleChange: (value: string) => void; onTypeChange: (value: DashboardAnalyticsWidgetType) => void; onWidthChange: (value: DashboardWidgetWidth) => void;
  reports: ListReportSummary[]; sections: SavedDashboardSection[]; selectedColumns: string[]; selectedFormId: string; selectedReportId: string; selectedSectionId: string;
  title: string; type: DashboardAnalyticsWidgetType; width: DashboardWidgetWidth;
};

const steps = ["Source", "Visualization", "Data", "Review"] as const;
const icons = { summary: Hash, breakdown: BarChart3, trend: TrendingUp, table: Table2 };
const metricOptions = [{ label: "Count records", value: "count" }, { label: "Sum numeric field", value: "sum" }, { label: "Average numeric field", value: "average" }];

export function DashboardAddWidgetWizard(props: Props) {
  const [step, setStep] = useState(0);
  const [query, setQuery] = useState("");
  const [recent, setRecent] = useState<DashboardAnalyticsWidgetType[]>(() => readRecentDashboardVisualizations(typeof window === "undefined" ? null : window.localStorage));
  const [preview, setPreview] = useState<DashboardAnalyticsResponse | null>(null);
  const [previewError, setPreviewError] = useState<string | null>(null);
  const [previewing, setPreviewing] = useState(false);
  const numericFields = props.fields.filter((field) => field.supportsAggregation);
  const groupFields = props.fields.filter((field) => field.supportsChoiceGrouping);
  const dateFields = props.fields.filter((field) => field.type === "date" || field.type === "datetime");
  const availability = useMemo(() => getVisualizationAvailability(props.fields), [props.fields]);
  const visualizations = useMemo(() => filterDashboardVisualizations(query), [query]);
  const builderConfig = { widgetType: props.type, metricType: props.metricType, metricFieldId: props.metricFieldId, groupByFieldId: props.groupByFieldId, dateFieldId: props.dateFieldId, columns: props.selectedColumns, limit: 10, reportId: props.selectedReportId || null };
  const configValid = hasRequiredDashboardAnalyticsConfig(builderConfig);

  useEffect(() => {
    if (step !== 3 || !props.selectedFormId || !configValid) { setPreview(null); setPreviewing(false); return; }
    let active = true; setPreviewing(true); setPreviewError(null);
    const chart = buildChartConfigFromDashboardAnalytics(builderConfig);
    runDashboardAnalytics(buildDashboardAnalyticsRequest(props.selectedFormId, chart)).then((result) => { if (active) setPreview(result); }).catch((error: unknown) => { if (active) setPreviewError(error instanceof Error ? error.message : "Could not generate the sample preview."); }).finally(() => { if (active) setPreviewing(false); });
    return () => { active = false; };
  }, [step, props.selectedFormId, props.selectedReportId, props.type, props.metricType, props.metricFieldId, props.groupByFieldId, props.dateFieldId, props.selectedColumns.join("|")]);

  const canContinue = step === 0 ? Boolean(props.selectedFormId && props.fields.length) : step === 1 ? availability[props.type].available : step === 2 ? props.canAdd : true;
  return <section aria-label="Add widget wizard" className="grid gap-5 rounded-xl border border-border bg-muted/10 p-4">
    <div className="flex flex-wrap items-center justify-between gap-3"><div><p className="font-bold">Add analytics widget</p><p className="text-xs text-muted-foreground">Choose a source first, then build a field-compatible visualization.</p></div><Badge tone="info">Step {step + 1} of 4</Badge></div>
    <div aria-label="Widget setup progress" className="grid grid-cols-2 gap-2 sm:grid-cols-4">{steps.map((label, index) => <button aria-current={index === step ? "step" : undefined} className={`rounded-lg border px-2 py-2 text-xs font-bold transition ${index === step ? "border-primary bg-primary/10 text-primary" : index < step ? "border-success/40 bg-success/10 text-success" : "border-border text-muted-foreground"}`} key={label} onClick={() => { if (index <= step || canContinue) setStep(index); }} type="button">{index < step ? <Check className="mx-auto mb-1 size-3" /> : null}{label}</button>)}</div>

    {step === 0 ? <div className="grid gap-4 sm:grid-cols-2"><Select disabled={props.forms.length === 0} label="Source form" onChange={(event) => props.onFormChange(event.target.value)} value={props.selectedFormId}>{props.forms.map((form) => <option key={form.id} value={form.id}>{form.name}</option>)}</Select><Select disabled={!props.selectedFormId} label="Saved report filter" onChange={(event) => props.onReportChange(event.target.value)} value={props.selectedReportId}><option value="">All permitted records</option>{props.reports.map((report) => <option key={report.id} value={report.id}>{report.name}</option>)}</Select>{props.selectedFormId && props.fields.length === 0 ? <p className="text-xs font-semibold text-muted-foreground sm:col-span-2">Loading reportable fields…</p> : null}</div> : null}

    {step === 1 ? <div className="grid gap-4"><Input icon={<Search className="size-4" />} label="Search visualizations" onChange={(event) => setQuery(event.target.value)} placeholder="Try trend, KPI, table…" value={query} />{recent.length ? <div className="flex flex-wrap items-center gap-2"><span className="flex items-center gap-1 text-xs font-bold text-muted-foreground"><Clock3 className="size-3" />Recent</span>{recent.map((type) => <Button key={type} onClick={() => props.onTypeChange(type)} size="sm" variant="outline">{dashboardVisualizationCatalog.find((item) => item.type === type)?.name}</Button>)}</div> : null}<div className="grid gap-3 sm:grid-cols-2">{visualizations.map((item) => { const Icon = icons[item.type]; const state = availability[item.type]; const selected = props.type === item.type; return <button aria-pressed={selected} className={`rounded-xl border p-4 text-left transition ${selected ? "border-primary bg-primary/10 ring-2 ring-primary/20" : state.available ? "border-border bg-card hover:border-primary/60" : "cursor-not-allowed border-border bg-muted/40 opacity-60"}`} disabled={!state.available} key={item.type} onClick={() => props.onTypeChange(item.type)} type="button"><div className="flex items-start justify-between gap-3"><span className="rounded-lg bg-primary/10 p-2 text-primary"><Icon className="size-5" /></span>{selected ? <Badge tone="info">Selected</Badge> : state.available && state.recommendation.startsWith("Recommended:") ? <Badge tone="success">Recommended</Badge> : null}</div><p className="mt-3 font-bold">{item.name}</p><p className="mt-1 text-sm text-muted-foreground">{item.description}</p><p className="mt-3 text-xs font-semibold text-muted-foreground">{state.recommendation}</p></button>; })}</div>{visualizations.length === 0 ? <Alert title="No visualizations found">Try a different search term.</Alert> : null}</div> : null}

    {step === 2 ? <div className="grid gap-4"><div className="grid gap-4 sm:grid-cols-2"><Input label="Widget title" onChange={(event) => props.onTitleChange(event.target.value)} value={props.title} /><Select label="Section" onChange={(event) => props.onSectionChange(event.target.value)} options={props.sections.map((section) => ({ label: section.title, value: section.id }))} value={props.selectedSectionId} /><Select label="Metric" onChange={(event) => props.onMetricTypeChange(event.target.value as ChartMetricType)} options={metricOptions} value={props.metricType} /><Select label="Width" onChange={(event) => props.onWidthChange(event.target.value as DashboardWidgetWidth)} options={dashboardWidgetWidths.map((width) => ({ label: width, value: width }))} value={props.width} />{props.metricType !== "count" ? <Select disabled={!numericFields.length} label="Numeric metric field" onChange={(event) => props.onMetricFieldChange(event.target.value)} value={props.metricFieldId}>{numericFields.map((field) => <option key={field.id} value={field.id}>{field.label}</option>)}</Select> : null}{props.type === "breakdown" ? <Select disabled={!groupFields.length} label="Group by" onChange={(event) => props.onGroupFieldChange(event.target.value)} value={props.groupByFieldId}>{groupFields.map((field) => <option key={field.id} value={field.id}>{field.label}</option>)}</Select> : null}{props.type === "trend" ? <Select disabled={!dateFields.length} label="Trend date" onChange={(event) => props.onDateFieldChange(event.target.value)} value={props.dateFieldId}>{dateFields.map((field) => <option key={field.id} value={field.id}>{field.label}</option>)}</Select> : null}</div>{props.type === "table" ? <div className="grid gap-2"><p className="text-sm font-bold">Table columns</p><div className="grid gap-2 sm:grid-cols-2 xl:grid-cols-3">{props.fields.map((field) => <Checkbox checked={props.selectedColumns.includes(field.id)} key={field.id} label={field.label} onChange={(event) => props.onColumnsChange(field.id, event.target.checked)} />)}</div></div> : null}{!props.title.trim() ? <p className="text-xs font-semibold text-danger">Widget title is required.</p> : !configValid ? <p className="text-xs font-semibold text-danger">Complete the required metric and visualization fields.</p> : null}</div> : null}

    {step === 3 ? <div className="grid gap-4"><div className="grid gap-3 rounded-xl border border-border bg-card p-4 sm:grid-cols-2"><div><p className="text-xs font-bold text-muted-foreground">Source</p><p className="font-bold">{props.forms.find((form) => form.id === props.selectedFormId)?.name}</p></div><div><p className="text-xs font-bold text-muted-foreground">Visualization</p><p className="font-bold">{dashboardVisualizationCatalog.find((item) => item.type === props.type)?.name}</p></div><div><p className="text-xs font-bold text-muted-foreground">Destination</p><p className="font-bold">{props.sections.find((section) => section.id === props.selectedSectionId)?.title} · {props.width}</p></div><div><p className="text-xs font-bold text-muted-foreground">Metric</p><p className="font-bold">{metricOptions.find((item) => item.value === props.metricType)?.label}</p></div></div><div className="rounded-xl border border-border bg-card p-4"><div className="mb-3 flex items-center justify-between"><p className="font-bold">Sample preview</p>{previewing ? <Badge>Loading…</Badge> : null}</div>{previewError ? <Alert title="Preview unavailable">{previewError}</Alert> : preview ? <ChartWidgetPreview preview={preview} /> : <p className="text-sm text-muted-foreground">Preparing a permission-checked preview…</p>}</div></div> : null}

    <div className="flex flex-wrap justify-between gap-2 border-t border-border pt-4"><Button disabled={step === 0} onClick={() => setStep((current) => Math.max(0, current - 1))} variant="outline"><ArrowLeft className="size-4" />Back</Button>{step < 3 ? <Button disabled={!canContinue} onClick={() => setStep((current) => Math.min(3, current + 1))}>Continue<ArrowRight className="size-4" /></Button> : <Button disabled={!props.canAdd || previewing} onClick={async () => { if (await props.onAdd()) { const next = saveRecentDashboardVisualization(window.localStorage, props.type, recent); setRecent(next); setStep(1); } }}><Plus className="size-4" />Add to dashboard</Button>}</div>
  </section>;
}

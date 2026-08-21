import { useEffect, useMemo, useState } from "react";
import { Filter, RefreshCw } from "lucide-react";
import { Alert } from "../../../components/ui/Alert";
import { Badge } from "../../../components/ui/Badge";
import { Button } from "../../../components/ui/Button";
import { Input } from "../../../components/ui/Input";
import { Select } from "../../../components/ui/Select";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "../../../components/ui/Card";
import { EmptyState } from "../../../components/ui/EmptyState";
import { runDashboardAnalytics } from "../api";
import { useLocalization } from "../../../context/LocalizationContext";
import { buildDashboardAnalyticsRequest } from "../analytics";
import { getDashboardAdapter } from "../adapters";
import { getDashboardWidgetGridClass, orderDashboardLayoutWidgets } from "../layout";
import { normalizeDashboardSections } from "../sections";
import type { DashboardAnalyticsFilterValue, DashboardAnalyticsResponse, DashboardDetail, DashboardFilterDefinition, SavedDashboardWidget } from "../types";
import { ChartWidgetPreview } from "./ChartWidgetPreview";

type WidgetState = { status: "loading" | "ready" | "error"; preview?: DashboardAnalyticsResponse; error?: string };
type FilterSelections = Record<string, DashboardAnalyticsFilterValue | undefined>;

export function SavedDashboardViewer({ dashboard }: { dashboard: DashboardDetail }) {
  const { formatDate } = useLocalization();
  const sections = useMemo(() => {
    return normalizeDashboardSections(dashboard.config.sections);
  }, [dashboard.config.sections]);
  const [activeSectionId, setActiveSectionId] = useState(sections[0]?.id ?? "overview");
  const [states, setStates] = useState<Record<string, WidgetState>>({});
  const [draftFilters, setDraftFilters] = useState<FilterSelections>({});
  const [appliedFilters, setAppliedFilters] = useState<FilterSelections>({});
  const orderedLayout = orderDashboardLayoutWidgets(dashboard.layout.widgets);
  const visibleLayouts = orderedLayout.filter((layout) => {
    const widget = dashboard.config.widgets.find((item) => item.id === layout.id);
    return (widget?.sectionId ?? sections[0]?.id) === activeSectionId;
  });

  useEffect(() => {
    setActiveSectionId(sections[0]?.id ?? "overview");
    setStates({});
    setDraftFilters({});
    setAppliedFilters({});
  }, [dashboard.id]);

  useEffect(() => {
    const visibleWidgetIds = new Set(visibleLayouts.map((layout) => layout.id));
    for (const widget of dashboard.config.widgets) {
      if (visibleWidgetIds.has(widget.id) && widget.chart) void refresh(widget);
    }
  }, [activeSectionId, dashboard.id, appliedFilters]);

  async function refresh(widget: SavedDashboardWidget) {
    if (!widget.chart) return;
    if (!widget.sourceFormId) {
      setStates((current) => ({ ...current, [widget.id]: { status: "error", error: "Widget source form is unavailable." } }));
      return;
    }
    setStates((current) => ({ ...current, [widget.id]: { status: "loading" } }));
    try {
      const preview = await runDashboardAnalytics(buildDashboardAnalyticsRequest(widget.sourceFormId, widget.chart, buildWidgetFilters(widget, dashboard.config.filters, appliedFilters)));
      setStates((current) => ({ ...current, [widget.id]: { status: "ready", preview } }));
    } catch (error) {
      setStates((current) => ({ ...current, [widget.id]: { status: "error", error: error instanceof Error ? error.message : "Widget request failed." } }));
    }
  }

  return (
    <div className="grid gap-6">
      <header className="grid gap-2">
        <div className="flex flex-wrap items-center gap-2"><Badge tone="success">Published</Badge>{dashboard.isDefault ? <Badge>Default</Badge> : null}</div>
        <h1 className="text-3xl font-extrabold tracking-tight text-foreground">{dashboard.name}</h1>
        {dashboard.description ? <p className="max-w-3xl text-sm leading-6 text-muted-foreground">{dashboard.description}</p> : null}
        <p className="text-xs font-semibold text-muted-foreground">Published {formatDate(dashboard.publishedAt ?? dashboard.updatedAt ?? dashboard.createdAt)}</p>
      </header>

      {sections.length > 1 ? (
        <div className="flex gap-2 overflow-x-auto border-b border-border" role="tablist">
          {sections.map((section) => <button aria-controls={`dashboard-panel-${section.id}`} aria-selected={activeSectionId === section.id} className={`min-h-11 shrink-0 border-b-2 px-3 py-2 text-sm font-bold focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary ${activeSectionId === section.id ? "border-primary text-foreground" : "border-transparent text-muted-foreground"}`} id={`dashboard-tab-${section.id}`} key={section.id} onClick={() => setActiveSectionId(section.id)} role="tab" type="button">{section.title}</button>)}
        </div>
      ) : null}

      {(dashboard.config.filters?.length ?? 0) > 0 ? <DashboardFilters definitions={dashboard.config.filters!} draft={draftFilters} onChange={setDraftFilters} onApply={() => setAppliedFilters({ ...draftFilters })} onReset={() => { setDraftFilters({}); setAppliedFilters({}); }} /> : null}

      <section aria-labelledby={`dashboard-tab-${activeSectionId}`} className="grid gap-4 md:grid-cols-12" id={`dashboard-panel-${activeSectionId}`} role="tabpanel">
        {visibleLayouts.length === 0 ? <div className="md:col-span-12"><EmptyState title="No widgets in this section" description="This published section has no visible widgets." /></div> : visibleLayouts.map((layout) => {
          const widget = dashboard.config.widgets.find((item) => item.id === layout.id);
          if (!widget) return null;
          return <ViewerWidget key={widget.id} layoutWidth={layout.width} onRefresh={() => void refresh(widget)} state={states[widget.id]} widget={widget} />;
        })}
      </section>
    </div>
  );
}

function buildWidgetFilters(widget: SavedDashboardWidget, definitions: DashboardFilterDefinition[] | null | undefined, selections: FilterSelections): DashboardAnalyticsFilterValue[] {
  return (definitions ?? []).filter((definition) => definition.sourceFormId === widget.sourceFormId && (!definition.applyToWidgetIds || definition.applyToWidgetIds.includes(widget.id)))
    .map((definition) => selections[definition.id]).filter((value): value is DashboardAnalyticsFilterValue => Boolean(value && ((value.values?.length ?? 0) > 0 || value.start || value.end)));
}

function DashboardFilters({ definitions, draft, onChange, onApply, onReset }: { definitions: DashboardFilterDefinition[]; draft: FilterSelections; onChange: (value: FilterSelections) => void; onApply: () => void; onReset: () => void }) {
  const activeCount = Object.values(draft).filter((value) => value && ((value.values?.length ?? 0) > 0 || value.start || value.end)).length;
  return <section aria-label="Dashboard filters" className="grid gap-4 rounded-xl border border-border bg-muted/20 p-4"><div className="flex flex-wrap items-center justify-between gap-3"><div className="flex items-center gap-2"><Filter className="size-4 text-primary" /><p className="text-sm font-bold">Filters</p>{activeCount ? <Badge tone="info">{activeCount} active</Badge> : null}</div><div className="flex gap-2"><Button onClick={onReset} size="sm" variant="outline">Reset all</Button><Button onClick={onApply} size="sm">Apply</Button></div></div><div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">{definitions.map((definition) => definition.type === "date_range" ? <div className="grid grid-cols-2 gap-2" key={definition.id}><Input aria-label={`${definition.label} start`} onChange={(event) => onChange({ ...draft, [definition.id]: { fieldId: definition.fieldId, ...draft[definition.id], start: event.target.value || null } })} type="date" value={draft[definition.id]?.start ?? ""} /><Input aria-label={`${definition.label} end exclusive`} onChange={(event) => onChange({ ...draft, [definition.id]: { fieldId: definition.fieldId, ...draft[definition.id], end: event.target.value || null } })} type="date" value={draft[definition.id]?.end ?? ""} /></div> : <Select key={definition.id} label={definition.label} onChange={(event) => onChange({ ...draft, [definition.id]: event.target.value ? { fieldId: definition.fieldId, values: [event.target.value] } : undefined })} value={draft[definition.id]?.values?.[0] ?? ""}><option value="">All</option>{(definition.options ?? []).map((option) => <option key={option} value={option}>{option}</option>)}</Select>)}</div></section>;
}

function ViewerWidget({ layoutWidth, onRefresh, state, widget }: { layoutWidth: "small" | "medium" | "wide" | "full"; onRefresh: () => void; state?: WidgetState; widget: SavedDashboardWidget }) {
  if (widget.adapter) {
    const registration = getDashboardAdapter(widget.adapter.adapterId);
    if (!registration) return <Card className={getDashboardWidgetGridClass(layoutWidth)}><CardHeader><CardTitle>{widget.title}</CardTitle></CardHeader><CardContent><Alert title="Adapter unavailable">The “{widget.adapter.adapterId}” dashboard adapter is not installed.</Alert></CardContent></Card>;
    const Renderer = registration.render;
    return <Card className={getDashboardWidgetGridClass(layoutWidth)}><CardHeader><CardTitle>{widget.title}</CardTitle><CardDescription>{registration.name}</CardDescription></CardHeader><CardContent><Renderer widget={widget} /></CardContent></Card>;
  }
  return <Card className={getDashboardWidgetGridClass(layoutWidth)}><CardHeader><div className="flex items-center justify-between gap-3"><CardTitle>{widget.title}</CardTitle><Button aria-label={`Refresh ${widget.title}`} disabled={state?.status === "loading"} onClick={onRefresh} size="icon" variant="outline"><RefreshCw className={`size-4 ${state?.status === "loading" ? "animate-spin" : ""}`} /></Button></div></CardHeader><CardContent>{state?.status === "ready" && state.preview ? <ChartWidgetPreview preview={state.preview} /> : state?.status === "error" ? <Alert title="Widget unavailable">{state.error}</Alert> : <div className="flex items-center gap-2 py-6 text-sm font-semibold text-muted-foreground"><RefreshCw className="size-4 animate-spin" /> Loading widget…</div>}</CardContent></Card>;
}

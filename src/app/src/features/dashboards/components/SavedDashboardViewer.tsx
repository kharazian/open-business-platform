import { useEffect, useMemo, useRef, useState } from "react";
import { Copy, Filter, Maximize2, Minimize2, RefreshCw, RotateCcw } from "lucide-react";
import { useNavigate } from "react-router-dom";
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
import { getDashboardAccentColor, resolveDashboardChartAppearance } from "../appearance";
import { getDashboardAdapter } from "../adapters";
import { getDashboardWidgetGridClass, orderDashboardLayoutWidgets } from "../layout";
import { normalizeDashboardSections } from "../sections";
import { getDashboardFilterDefaults, getMissingRequiredDashboardFilters, hasDashboardFilterValue } from "../filterAuthoring";
import { buildDashboardDrillThroughPath, type DashboardPointSelection } from "../drillThrough";
import type { DashboardAnalyticsFilterValue, DashboardAnalyticsResponse, DashboardDetail, DashboardFilterDefinition, SavedDashboardWidget } from "../types";
import { ChartWidgetPreview } from "./ChartWidgetPreview";
import { DashboardSectionTabs } from "./DashboardSectionTabs";

type WidgetState = { status: "loading" | "ready" | "error"; preview?: DashboardAnalyticsResponse; error?: string };
type FilterSelections = Record<string, DashboardAnalyticsFilterValue | undefined>;

export function SavedDashboardViewer({ dashboard, preview = false }: { dashboard: DashboardDetail; preview?: boolean }) {
  const navigate = useNavigate();
  const { formatDate } = useLocalization();
  const sections = useMemo(() => {
    return normalizeDashboardSections(dashboard.config.sections);
  }, [dashboard.config.sections]);
  const [activeSectionId, setActiveSectionId] = useState(sections[0]?.id ?? "overview");
  const [states, setStates] = useState<Record<string, WidgetState>>({});
  const [draftFiltersBySection, setDraftFiltersBySection] = useState<Record<string, FilterSelections>>({});
  const [appliedFiltersBySection, setAppliedFiltersBySection] = useState<Record<string, FilterSelections>>({});
  const [lastRefreshByWidget, setLastRefreshByWidget] = useState<Record<string, string>>({});
  const [focusMode, setFocusMode] = useState(false);
  const requestSequences = useRef<Record<string, number>>({});
  const orderedLayout = orderDashboardLayoutWidgets(dashboard.layout.widgets);
  const visibleLayouts = orderedLayout.filter((layout) => {
    const widget = dashboard.config.widgets.find((item) => item.id === layout.id);
    return (widget?.sectionId ?? sections[0]?.id) === activeSectionId;
  });
  const visibleWidgets = visibleLayouts.map((layout) => dashboard.config.widgets.find((item) => item.id === layout.id)).filter((widget): widget is SavedDashboardWidget => Boolean(widget));
  const visibleSourceIds = new Set(visibleWidgets.map((widget) => widget.sourceFormId).filter(Boolean));
  const visibleFilters = (dashboard.config.filters ?? []).filter((filter) => visibleSourceIds.has(filter.sourceFormId));
  const dashboardDefaults = useMemo(() => getDashboardFilterDefaults(dashboard.config.filters ?? []), [dashboard.config.filters]);
  const draftFilters = draftFiltersBySection[activeSectionId] ?? dashboardDefaults;
  const appliedFilters = appliedFiltersBySection[activeSectionId] ?? dashboardDefaults;

  useEffect(() => {
    setActiveSectionId(sections[0]?.id ?? "overview");
    setStates({});
    setDraftFiltersBySection(Object.fromEntries(sections.map((section) => [section.id, { ...dashboardDefaults }])));
    setAppliedFiltersBySection(Object.fromEntries(sections.map((section) => [section.id, { ...dashboardDefaults }])));
    setLastRefreshByWidget({});
    setFocusMode(false);
  }, [dashboard.id, dashboardDefaults, sections]);

  useEffect(() => {
    void refreshVisibleWidgets();
  }, [activeSectionId, dashboard.id, appliedFilters]);

  useEffect(() => {
    if (!focusMode) return;
    const exit = (event: KeyboardEvent) => { if (event.key === "Escape") setFocusMode(false); };
    document.addEventListener("keydown", exit);
    return () => document.removeEventListener("keydown", exit);
  }, [focusMode]);

  async function refresh(widget: SavedDashboardWidget) {
    if (!widget.chart) return;
    if (!widget.sourceFormId) {
      setStates((current) => ({ ...current, [widget.id]: { status: "error", error: "Widget source form is unavailable." } }));
      return;
    }
    const sequence = (requestSequences.current[widget.id] ?? 0) + 1;
    requestSequences.current[widget.id] = sequence;
    setStates((current) => ({ ...current, [widget.id]: { status: "loading", preview: current[widget.id]?.preview } }));
    try {
      const preview = await runDashboardAnalytics(buildDashboardAnalyticsRequest(widget.sourceFormId, widget.chart, buildWidgetFilters(widget, dashboard.config.filters, appliedFilters)));
      if (requestSequences.current[widget.id] !== sequence) return;
      setStates((current) => ({ ...current, [widget.id]: { status: "ready", preview } }));
      setLastRefreshByWidget((current) => ({ ...current, [widget.id]: new Date().toISOString() }));
    } catch (error) {
      if (requestSequences.current[widget.id] !== sequence) return;
      setStates((current) => ({ ...current, [widget.id]: { status: "error", error: error instanceof Error ? error.message : "Widget request failed." } }));
    }
  }

  async function refreshVisibleWidgets() {
    const queue = visibleWidgets.filter((widget) => widget.chart);
    let next = 0;
    await Promise.all(Array.from({ length: Math.min(3, queue.length) }, async () => {
      while (next < queue.length) {
        const widget = queue[next++];
        await refresh(widget);
      }
    }));
  }

  function resetCurrentTab() {
    setDraftFiltersBySection((current) => ({ ...current, [activeSectionId]: { ...dashboardDefaults } }));
    setAppliedFiltersBySection((current) => ({ ...current, [activeSectionId]: { ...dashboardDefaults } }));
  }

  function openDrillThrough(widget: SavedDashboardWidget, point: DashboardPointSelection) {
    const path = buildDashboardDrillThroughPath(widget, dashboard.config.filters, appliedFilters, point);
    if (!path) return;
    if (widget.interaction?.openInNewTab) window.open(path, "_blank", "noopener,noreferrer");
    else navigate(path);
  }

  const successfulRefreshes = visibleWidgets.map((widget) => lastRefreshByWidget[widget.id]).filter(Boolean).sort();
  const lastSuccessfulRefresh = successfulRefreshes.at(-1);

  return (
    <div className={`grid gap-6 ${focusMode ? "fixed inset-0 z-50 overflow-auto bg-background p-4 sm:p-8" : ""}`}>
      <header className="grid gap-2">
        <div className="flex flex-wrap items-center gap-2"><Badge tone={preview ? "warning" : "success"}>{preview ? "Draft preview" : "Published"}</Badge>{dashboard.isDefault ? <Badge>Default</Badge> : null}</div>
        <h1 className="text-3xl font-extrabold tracking-tight text-foreground">{dashboard.name}</h1>
        {dashboard.description ? <p className="max-w-3xl text-sm leading-6 text-muted-foreground">{dashboard.description}</p> : null}
        <p className="text-xs font-semibold text-muted-foreground">{preview ? "Previewing the current editor draft" : `Published ${formatDate(dashboard.publishedAt ?? dashboard.updatedAt ?? dashboard.createdAt)}`}</p>
        <div className="flex flex-wrap gap-2">
          <Button onClick={() => void refreshVisibleWidgets()} size="sm" variant="outline"><RefreshCw className="size-4" />Refresh current tab</Button>
          <Button onClick={resetCurrentTab} size="sm" variant="outline"><RotateCcw className="size-4" />Reset current tab</Button>
          {!preview ? <Button onClick={() => void navigator.clipboard.writeText(window.location.href)} size="sm" variant="outline"><Copy className="size-4" />Copy link</Button> : null}
          <Button aria-pressed={focusMode} onClick={() => setFocusMode((current) => !current)} size="sm" variant="outline">{focusMode ? <Minimize2 className="size-4" /> : <Maximize2 className="size-4" />}{focusMode ? "Exit focus" : "Focus mode"}</Button>
        </div>
        {lastSuccessfulRefresh ? <p className="text-xs font-semibold text-muted-foreground">Last successful refresh {formatDate(lastSuccessfulRefresh)}</p> : null}
      </header>

      {sections.length > 1 ? <DashboardSectionTabs activeSectionId={activeSectionId} onChange={setActiveSectionId} sections={sections} /> : null}

      {visibleFilters.length > 0 ? <DashboardFilters definitions={visibleFilters} draft={draftFilters} onChange={(next) => setDraftFiltersBySection((current) => ({ ...current, [activeSectionId]: next }))} onApply={() => setAppliedFiltersBySection((current) => ({ ...current, [activeSectionId]: { ...draftFilters } }))} onReset={resetCurrentTab} /> : null}

      <section aria-labelledby={`dashboard-tab-${activeSectionId}`} className="grid gap-4 md:grid-cols-12" id={`dashboard-panel-${activeSectionId}`} role="tabpanel">
        {visibleLayouts.length === 0 ? <div className="md:col-span-12"><EmptyState title="No widgets in this section" description="This published section has no visible widgets." /></div> : visibleLayouts.map((layout) => {
          const widget = dashboard.config.widgets.find((item) => item.id === layout.id);
          if (!widget) return null;
          return <ViewerWidget key={widget.id} layoutWidth={layout.width} lastRefresh={lastRefreshByWidget[widget.id]} onDrillThrough={(point) => openDrillThrough(widget, point)} onRefresh={() => void refresh(widget)} state={states[widget.id]} widget={widget} />;
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
  const activeCount = definitions.filter((definition) => hasDashboardFilterValue(draft[definition.id], definition.type)).length;
  const missingRequired = getMissingRequiredDashboardFilters(definitions, draft);
  return <section aria-label="Dashboard filters" className="grid gap-4 rounded-xl border border-border bg-muted/20 p-4"><div className="flex flex-wrap items-center justify-between gap-3"><div className="flex items-center gap-2"><Filter className="size-4 text-primary" /><p className="text-sm font-bold">Filters</p>{activeCount ? <Badge tone="info">{activeCount} active</Badge> : null}</div><div className="flex gap-2"><Button onClick={onReset} size="sm" variant="outline">Reset all</Button><Button disabled={missingRequired.length > 0} onClick={onApply} size="sm">Apply</Button></div></div>{missingRequired.length ? <Alert title="Required filters incomplete">Complete {missingRequired.map((filter) => filter.label).join(", ")} before applying filters.</Alert> : null}<div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">{definitions.map((definition) => definition.type === "date_range" ? <div className="grid gap-2" key={definition.id}><p className="text-sm font-bold text-foreground">{definition.label}{definition.required ? " *" : ""}</p><div className="grid grid-cols-2 gap-2"><Input aria-label={`${definition.label} start`} onChange={(event) => onChange({ ...draft, [definition.id]: { fieldId: definition.fieldId, ...draft[definition.id], start: event.target.value || null } })} type="date" value={draft[definition.id]?.start ?? ""} /><Input aria-label={`${definition.label} end exclusive`} onChange={(event) => onChange({ ...draft, [definition.id]: { fieldId: definition.fieldId, ...draft[definition.id], end: event.target.value || null } })} type="date" value={draft[definition.id]?.end ?? ""} /></div></div> : definition.type === "multi_select" ? <Select className="h-28 py-2" key={definition.id} label={`${definition.label}${definition.required ? " *" : ""}`} multiple onChange={(event) => { const values = Array.from(event.target.selectedOptions, (option) => option.value).slice(0, 20); onChange({ ...draft, [definition.id]: values.length ? { fieldId: definition.fieldId, values } : undefined }); }} value={draft[definition.id]?.values ?? []}>{(definition.options ?? []).map((option) => <option key={option} value={option}>{option}</option>)}</Select> : <Select key={definition.id} label={`${definition.label}${definition.required ? " *" : ""}`} onChange={(event) => onChange({ ...draft, [definition.id]: event.target.value ? { fieldId: definition.fieldId, values: [event.target.value] } : undefined })} value={draft[definition.id]?.values?.[0] ?? ""}><option value="">{definition.required ? "Choose…" : "All"}</option>{(definition.options ?? []).map((option) => <option key={option} value={option}>{option}</option>)}</Select>)}</div>{activeCount ? <div aria-label="Active filter chips" className="flex flex-wrap gap-2">{definitions.flatMap((definition) => (draft[definition.id]?.values ?? []).map((value) => <button className="rounded-full border border-border bg-card px-3 py-1 text-xs font-bold" key={`${definition.id}-${value}`} onClick={() => { const values = (draft[definition.id]?.values ?? []).filter((item) => item !== value); onChange({ ...draft, [definition.id]: values.length ? { fieldId: definition.fieldId, values } : undefined }); }} type="button">{definition.label}: {value} ×</button>))}</div> : null}</section>;
}

function ViewerWidget({ layoutWidth, lastRefresh, onDrillThrough, onRefresh, state, widget }: { layoutWidth: "small" | "medium" | "wide" | "full"; lastRefresh?: string; onDrillThrough: (point: DashboardPointSelection) => void; onRefresh: () => void; state?: WidgetState; widget: SavedDashboardWidget }) {
  if (widget.adapter) {
    const registration = getDashboardAdapter(widget.adapter.adapterId);
    if (!registration) return <Card className={getDashboardWidgetGridClass(layoutWidth)}><CardHeader><CardTitle>{widget.title}</CardTitle></CardHeader><CardContent><Alert title="Adapter unavailable">The “{widget.adapter.adapterId}” dashboard adapter is not installed.</Alert></CardContent></Card>;
    const Renderer = registration.render;
    return <Card className={getDashboardWidgetGridClass(layoutWidth)}><CardHeader><CardTitle>{widget.title}</CardTitle><CardDescription>{registration.name}</CardDescription></CardHeader><CardContent><Renderer widget={widget} /></CardContent></Card>;
  }
  const appearance = resolveDashboardChartAppearance(widget.chart?.appearance);
  const accent = getDashboardAccentColor(appearance.cardAccent, appearance.palette);
  return <Card className={getDashboardWidgetGridClass(layoutWidth)} style={accent ? { borderTopColor: accent, borderTopWidth: 4 } : undefined}><CardHeader><div className="flex items-start justify-between gap-3"><div><CardTitle>{widget.title}</CardTitle>{widget.subtitle ? <CardDescription>{widget.subtitle}</CardDescription> : null}{widget.interaction ? <p className="mt-1 text-xs font-semibold text-primary">Select data to open {widget.interaction.destination === "report" ? "the linked report" : "source records"}.</p> : null}{lastRefresh ? <p className="mt-1 text-xs font-semibold text-muted-foreground">Refreshed {new Date(lastRefresh).toLocaleTimeString()}</p> : null}</div><Button aria-label={`Refresh ${widget.title}`} disabled={state?.status === "loading"} onClick={onRefresh} size="icon" variant="outline"><RefreshCw className={`size-4 ${state?.status === "loading" ? "animate-spin" : ""}`} /></Button></div></CardHeader><CardContent>{state?.status === "ready" && state.preview ? <ChartWidgetPreview appearance={appearance} interactionLabel={`Open ${widget.interaction?.destination === "report" ? "linked report" : "source records"}`} onSelect={widget.interaction ? onDrillThrough : undefined} preview={state.preview} /> : state?.status === "error" ? <Alert title="Widget unavailable">{state.error}</Alert> : <div className="flex items-center gap-2 py-6 text-sm font-semibold text-muted-foreground"><RefreshCw className="size-4 animate-spin" /> Loading widget…</div>}</CardContent></Card>;
}

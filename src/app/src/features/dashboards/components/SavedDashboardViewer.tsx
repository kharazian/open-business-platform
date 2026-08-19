import { useEffect, useMemo, useState } from "react";
import { RefreshCw } from "lucide-react";
import { Alert } from "../../../components/ui/Alert";
import { Badge } from "../../../components/ui/Badge";
import { Button } from "../../../components/ui/Button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "../../../components/ui/Card";
import { EmptyState } from "../../../components/ui/EmptyState";
import { runDashboardAnalytics } from "../api";
import { buildDashboardAnalyticsRequest } from "../analytics";
import { getDashboardAdapter } from "../adapters";
import { getDashboardWidgetGridClass, orderDashboardLayoutWidgets } from "../layout";
import { normalizeDashboardSections } from "../sections";
import type { DashboardAnalyticsResponse, DashboardDetail, SavedDashboardWidget } from "../types";
import { ChartWidgetPreview } from "./ChartWidgetPreview";

type WidgetState = { status: "loading" | "ready" | "error"; preview?: DashboardAnalyticsResponse; error?: string };

export function SavedDashboardViewer({ dashboard }: { dashboard: DashboardDetail }) {
  const sections = useMemo(() => {
    return normalizeDashboardSections(dashboard.config.sections);
  }, [dashboard.config.sections]);
  const [activeSectionId, setActiveSectionId] = useState(sections[0]?.id ?? "overview");
  const [states, setStates] = useState<Record<string, WidgetState>>({});
  const orderedLayout = orderDashboardLayoutWidgets(dashboard.layout.widgets);
  const visibleLayouts = orderedLayout.filter((layout) => {
    const widget = dashboard.config.widgets.find((item) => item.id === layout.id);
    return (widget?.sectionId ?? sections[0]?.id) === activeSectionId;
  });

  useEffect(() => {
    setActiveSectionId(sections[0]?.id ?? "overview");
    for (const widget of dashboard.config.widgets) {
      if (widget.chart) void refresh(widget);
    }
  }, [dashboard.id]);

  async function refresh(widget: SavedDashboardWidget) {
    if (!widget.chart) return;
    if (!widget.sourceFormId) {
      setStates((current) => ({ ...current, [widget.id]: { status: "error", error: "Widget source form is unavailable." } }));
      return;
    }
    setStates((current) => ({ ...current, [widget.id]: { status: "loading" } }));
    try {
      const preview = await runDashboardAnalytics(buildDashboardAnalyticsRequest(widget.sourceFormId, widget.chart));
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
        <p className="text-xs font-semibold text-muted-foreground">Published {new Date(dashboard.publishedAt ?? dashboard.updatedAt ?? dashboard.createdAt).toLocaleDateString()}</p>
      </header>

      {sections.length > 1 ? (
        <div className="flex gap-2 overflow-x-auto border-b border-border" role="tablist">
          {sections.map((section) => <button className={`border-b-2 px-3 py-2 text-sm font-bold ${activeSectionId === section.id ? "border-primary text-foreground" : "border-transparent text-muted-foreground"}`} key={section.id} onClick={() => setActiveSectionId(section.id)} role="tab" type="button">{section.title}</button>)}
        </div>
      ) : null}

      <section className="grid gap-4 md:grid-cols-12">
        {visibleLayouts.length === 0 ? <div className="md:col-span-12"><EmptyState title="No widgets in this section" description="This published section has no visible widgets." /></div> : visibleLayouts.map((layout) => {
          const widget = dashboard.config.widgets.find((item) => item.id === layout.id);
          if (!widget) return null;
          return <ViewerWidget key={widget.id} layoutWidth={layout.width} onRefresh={() => void refresh(widget)} state={states[widget.id]} widget={widget} />;
        })}
      </section>
    </div>
  );
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

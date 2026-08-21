import { useEffect, useMemo, useState } from "react";
import { ArrowDown, ArrowUp, Copy, ExternalLink, Plus, RefreshCw, Save, Trash2 } from "lucide-react";
import { useNavigate, useParams } from "react-router-dom";
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
import { createDashboard, DashboardApiError, getDashboard, listDashboards, publishDashboard, runDashboardAnalytics, unpublishDashboard, updateDashboard } from "../api";
import {
  buildChartConfigFromDashboardAnalytics,
  buildDashboardAnalyticsRequest,
  createDashboardPreviewStates,
  getDashboardAnalyticsWidgetLabel,
  getDashboardVisibilityLabel,
  hasRequiredDashboardAnalyticsConfig,
  normalizeDashboardSettings,
  toDashboardAnalyticsWidgetType,
  type DashboardPreviewState
} from "../analytics";
import { ChartWidgetPreview } from "../components/ChartWidgetPreview";
import { DashboardAdapterSettingsEditor } from "../components/DashboardAdapterSettingsEditor";
import { DashboardTemplateGallery } from "../components/DashboardTemplateGallery";
import { createDashboardAdapterWidget, getDashboardAdapter, isDashboardAdapterWidgetConfigured, listDashboardAdapters } from "../adapters";
import { getDashboardWidgetGridClass, orderDashboardLayoutWidgets } from "../layout";
import { dispatchDashboardsChanged } from "../events";
import { assignWidgetsToDashboardSections, createDashboardSectionId, defaultDashboardSection, normalizeDashboardSections } from "../sections";
import { instantiateDashboardTemplate } from "../templateEngine";
import { dashboardTemplateCatalog, validateTemplateFieldCapabilities } from "../templates/catalog";
import {
  dashboardWidgetWidths,
  type ChartMetricType,
  type ChartWidgetConfig,
  type DashboardAnalyticsResponse,
  type DashboardAdapterWidget,
  type DashboardAnalyticsWidgetType,
  type DashboardDetail,
  type DashboardSummaryItem,
  type DashboardValidationError,
  type DashboardVisibility,
  type DashboardWidgetWidth,
  type SavedDashboardSection,
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
const visibilityOptions: Array<{ label: string; value: DashboardVisibility }> = [
  { label: "Workspace", value: "workspace" },
  { label: "Private", value: "private" }
];

export function DashboardsPage() {
  const navigate = useNavigate();
  const { id: routeDashboardId } = useParams();
  const [dashboards, setDashboards] = useState<DashboardSummaryItem[]>([]);
  const [selectedDashboardId, setSelectedDashboardId] = useState("");
  const [dashboardDetail, setDashboardDetail] = useState<DashboardDetail | null>(null);
  const [forms, setForms] = useState<FormSummary[]>([]);
  const [formDetail, setFormDetail] = useState<FormDetail | null>(null);
  const [reports, setReports] = useState<ListReportSummary[]>([]);
  const [previewStates, setPreviewStates] = useState<Record<string, DashboardPreviewState | undefined>>({});
  const [dashboardName, setDashboardName] = useState("New dashboard");
  const [dashboardDescription, setDashboardDescription] = useState("");
  const [dashboardVisibility, setDashboardVisibility] = useState<DashboardVisibility>("workspace");
  const [dashboardIsDefault, setDashboardIsDefault] = useState(false);
  const [slug, setSlug] = useState("");
  const [showInNavigation, setShowInNavigation] = useState(false);
  const [menuLabel, setMenuLabel] = useState("");
  const [menuIcon, setMenuIcon] = useState("layout-dashboard");
  const [menuOrder, setMenuOrder] = useState(0);
  const [viewPermission, setViewPermission] = useState("");
  const [selectedFormId, setSelectedFormId] = useState("");
  const [widgetSourceType, setWidgetSourceType] = useState<"analytics" | "adapter">("analytics");
  const adapters = useMemo(() => listDashboardAdapters(), []);
  const [adapterWidget, setAdapterWidget] = useState<DashboardAdapterWidget | null>(() => {
    const adapter = listDashboardAdapters()[0];
    return adapter ? createDashboardAdapterWidget(adapter) : null;
  });
  const [selectedReportId, setSelectedReportId] = useState("");
  const [widgetTitle, setWidgetTitle] = useState("New widget");
  const [widgetType, setWidgetType] = useState<DashboardAnalyticsWidgetType>("summary");
  const [metricType, setMetricType] = useState<ChartMetricType>("count");
  const [metricFieldId, setMetricFieldId] = useState("");
  const [groupByFieldId, setGroupByFieldId] = useState("status");
  const [dateFieldId, setDateFieldId] = useState("created_at");
  const [selectedColumns, setSelectedColumns] = useState<string[]>([]);
  const [widgetWidth, setWidgetWidth] = useState<DashboardWidgetWidth>("medium");
  const [sections, setSections] = useState<SavedDashboardSection[]>([defaultDashboardSection]);
  const [selectedSectionId, setSelectedSectionId] = useState(defaultDashboardSection.id);
  const [newSectionTitle, setNewSectionTitle] = useState("");
  const [widgets, setWidgets] = useState<SavedDashboardWidget[]>([]);
  const [layoutWidgets, setLayoutWidgets] = useState<SavedDashboardWidgetLayout[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [validationErrors, setValidationErrors] = useState<DashboardValidationError[]>([]);
  const [notice, setNotice] = useState<string | null>(null);
  const [templateGalleryOpen, setTemplateGalleryOpen] = useState(false);
  const [selectedTemplateId, setSelectedTemplateId] = useState("");

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
      .catch(setRequestError);
  }, [selectedFormId]);

  useEffect(() => {
    if (dashboardVisibility === "private") {
      setDashboardIsDefault(false);
    }
  }, [dashboardVisibility]);

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
  const selectedAdapter = adapterWidget ? adapters.find((item) => item.id === adapterWidget.adapterId) : undefined;
  const canAddWidget = Boolean(widgetTitle.trim()) && (widgetSourceType === "adapter" ? isDashboardAdapterWidgetConfigured(selectedAdapter, adapterWidget) : Boolean(selectedFormId) && hasRequiredDashboardAnalyticsConfig(builderConfig));
  const selectedTemplate = dashboardTemplateCatalog.find((template) => template.id === selectedTemplateId) ?? null;
  const templateCapabilityErrors = selectedTemplate && formDetail?.id === selectedFormId
    ? validateTemplateFieldCapabilities(selectedTemplate, new Set(fieldOptions.map((field) => field.id)))
    : selectedTemplate && selectedFormId ? [{ path: "source", code: "template.source.loading", message: "Checking reportable fields…" }] : [];

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
    setValidationErrors([]);

    try {
      const [dashboardItems, formItems] = await Promise.all([listDashboards(), listForms()]);
      setDashboards(dashboardItems);
      setForms(formItems);
      setSelectedDashboardId((current) => current || routeDashboardId || dashboardItems[0]?.id || "");
      setSelectedFormId((current) => current || formItems[0]?.id || "");
    } catch (caught) {
      setRequestError(caught);
    } finally {
      setLoading(false);
    }
  }

  async function loadDashboard(dashboardId: string) {
    setError(null);
    setValidationErrors([]);

    try {
      const detail = await getDashboard(dashboardId);
      setDashboardDetail(detail);
      setDashboardName(detail.name);
      setDashboardDescription(detail.description ?? "");
      const settings = normalizeDashboardSettings({ visibility: detail.visibility, isDefault: detail.isDefault });
      setDashboardVisibility(settings.visibility);
      setDashboardIsDefault(settings.isDefault);
      setSlug(detail.publication.slug ?? "");
      setShowInNavigation(detail.publication.showInNavigation);
      setMenuLabel(detail.publication.menuLabel ?? "");
      setMenuIcon(detail.publication.menuIcon ?? "layout-dashboard");
      setMenuOrder(detail.publication.menuOrder);
      setViewPermission(detail.publication.viewPermission ?? "");
      const nextSections = normalizeDashboardSections(detail.config.sections);
      const nextWidgets = assignWidgetsToDashboardSections(detail.config.widgets, nextSections);
      setSections(nextSections);
      setSelectedSectionId(nextSections[0].id);
      setWidgets(nextWidgets);
      setLayoutWidgets(detail.layout.widgets);
      await loadPreviews(nextWidgets);
    } catch (caught) {
      setRequestError(caught);
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
    if (!widget.chart) {
      setPreviewStates((current) => ({ ...current, [widget.id]: { status: "error", error: widget.adapter ? `Adapter '${widget.adapter.adapterId}' is not installed in the builder.` : "Widget configuration is missing." } }));
      return;
    }
    if (!widget.sourceFormId) {
      setPreviewStates((current) => ({ ...current, [widget.id]: { status: "error", error: "Widget source form is unavailable." } }));
      return;
    }
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
    const chart = widgetSourceType === "analytics" ? buildChartConfig() : null;
    const widget: SavedDashboardWidget = { id, title: widgetTitle.trim(), sourceFormId: widgetSourceType === "analytics" ? selectedFormId : null, chart, sectionId: selectedSectionId, adapter: widgetSourceType === "adapter" ? adapterWidget : null };

    setError(null);

    try {
      const preview = chart ? await runDashboardAnalytics(buildDashboardAnalyticsRequest(selectedFormId, chart)) : null;
      setWidgets((current) => [...current, widget]);
      setLayoutWidgets((current) => [...current, { id, width: widgetWidth, order: current.length + 1 }]);
      if (preview) setPreviewStates((current) => ({ ...current, [id]: { status: "ready", preview } }));
      setNotice("Widget added. Save the dashboard to persist it.");
    } catch (caught) {
      setRequestError(caught);
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

  function handleAddSection() {
    const title = newSectionTitle.trim();
    if (!title) return;
    const section = { id: createDashboardSectionId(title, sections), title, order: sections.length };
    setSections((current) => [...current, section]);
    setSelectedSectionId(section.id);
    setNewSectionTitle("");
  }

  function handleRenameSection(sectionId: string, title: string) {
    setSections((current) => current.map((section) => section.id === sectionId ? { ...section, title } : section));
  }

  function handleMoveSection(sectionId: string, direction: -1 | 1) {
    setSections((current) => {
      const index = current.findIndex((section) => section.id === sectionId);
      const targetIndex = index + direction;
      if (index < 0 || targetIndex < 0 || targetIndex >= current.length) return current;
      const next = [...current];
      [next[index], next[targetIndex]] = [next[targetIndex], next[index]];
      return next.map((section, order) => ({ ...section, order }));
    });
  }

  function handleRemoveSection(sectionId: string) {
    if (sections.length === 1) return;
    const nextSections = sections.filter((section) => section.id !== sectionId).map((section, order) => ({ ...section, order }));
    const fallbackSectionId = nextSections[0].id;
    setSections(nextSections);
    setWidgets((current) => current.map((widget) => widget.sectionId === sectionId ? { ...widget, sectionId: fallbackSectionId } : widget));
    if (selectedSectionId === sectionId) setSelectedSectionId(fallbackSectionId);
  }

  async function handleSave() {
    setSaving(true);
    setError(null);
    setValidationErrors([]);
    setNotice(null);

    const request = buildSaveRequest();

    try {
      const saved = dashboardDetail
        ? await updateDashboard(dashboardDetail.id, { ...request, concurrencyStamp: dashboardDetail.concurrencyStamp })
        : await createDashboard(request);
      setDashboardDetail(saved);
      setSelectedDashboardId(saved.id);
      navigate(`/dashboard-builder/${saved.id}`, { replace: true });
      const settings = normalizeDashboardSettings({ visibility: saved.visibility, isDefault: saved.isDefault });
      setDashboardVisibility(settings.visibility);
      setDashboardIsDefault(settings.isDefault);
      setSections(normalizeDashboardSections(saved.config.sections));
      setWidgets(saved.config.widgets);
      setLayoutWidgets(saved.layout.widgets);
      setDashboards(await listDashboards());
      dispatchDashboardsChanged();
      await loadPreviews(saved.config.widgets);
      setNotice("Dashboard saved.");
    } catch (caught) {
      setRequestError(caught);
    } finally {
      setSaving(false);
    }
  }

  function handleNewDashboard() {
    setSelectedDashboardId("");
    setDashboardDetail(null);
    setDashboardName("New dashboard");
    setDashboardDescription("");
    setDashboardVisibility("workspace");
    setDashboardIsDefault(false);
    setSlug(""); setShowInNavigation(false); setMenuLabel(""); setMenuIcon("layout-dashboard"); setMenuOrder(0); setViewPermission("");
    setSections([defaultDashboardSection]);
    setSelectedSectionId(defaultDashboardSection.id);
    setNewSectionTitle("");
    setWidgets([]);
    setLayoutWidgets([]);
    setPreviewStates({});
    setNotice("New dashboard draft started.");
  }

  async function handleCreateFromTemplate() {
    if (!selectedTemplate || !selectedFormId || templateCapabilityErrors.length > 0) return;
    const instantiated = instantiateDashboardTemplate(selectedTemplate, {
      sources: { primary: { formId: selectedFormId, reportId: selectedReportId || null } }
    });
    if (!instantiated.ok) {
      setError(instantiated.errors.map((item) => item.message).join(" "));
      return;
    }
    setSaving(true);
    setError(null);
    try {
      const saved = await createDashboard(instantiated.dashboard);
      setTemplateGalleryOpen(false);
      setSelectedDashboardId(saved.id);
      setDashboards(await listDashboards());
      navigate(`/dashboard-builder/${saved.id}`);
      setNotice(`${selectedTemplate.name} created as an independent draft.`);
      dispatchDashboardsChanged();
    } catch (caught) {
      setRequestError(caught);
    } finally {
      setSaving(false);
    }
  }

  function handleSelectDashboard(dashboardId: string) {
    if (!dashboardId) {
      handleNewDashboard();
      return;
    }

    setSelectedDashboardId(dashboardId);
    navigate(`/dashboard-builder/${dashboardId}`);
  }

  async function handlePublicationAction() {
    if (!dashboardDetail) return;
    setSaving(true); setError(null); setValidationErrors([]);
    try {
      const nextSlug = slug || createDashboardSlug(dashboardName);
      const pending = await updateDashboard(dashboardDetail.id, {
        ...buildSaveRequest(nextSlug),
        concurrencyStamp: dashboardDetail.concurrencyStamp
      });
      const saved = pending.publication.status === "published"
        ? await unpublishDashboard(pending.id, pending.concurrencyStamp)
        : await publishDashboard(pending.id, pending.concurrencyStamp);
      setSlug(saved.publication.slug ?? nextSlug);
      setDashboardDetail(saved); setShowInNavigation(saved.publication.showInNavigation); setNotice(saved.publication.status === "published" ? "Dashboard published." : "Dashboard returned to draft.");
      setDashboards(await listDashboards());
      dispatchDashboardsChanged();
    } catch (caught) { setRequestError(caught); } finally { setSaving(false); }
  }

  function buildSaveRequest(slugOverride = slug) {
    const normalizedSections = normalizeDashboardSections(sections);
    const normalizedWidgets = assignWidgetsToDashboardSections(widgets, normalizedSections);
    return {
      name: dashboardName,
      description: dashboardDescription || null,
      config: { schemaVersion: 1 as const, sections: normalizedSections, widgets: normalizedWidgets, templateProvenance: dashboardDetail?.config.templateProvenance ?? null, filters: dashboardDetail?.config.filters ?? null },
      layout: { schemaVersion: 1 as const, widgets: layoutWidgets },
      settings: normalizeDashboardSettings({ visibility: dashboardVisibility, isDefault: dashboardIsDefault }),
      publication: { status: dashboardDetail?.publication.status ?? "draft", slug: slugOverride || null, showInNavigation, menuLabel: showInNavigation ? (menuLabel.trim() || dashboardName.trim()) : null, menuIcon: menuIcon || null, menuOrder, viewPermission: viewPermission || null }
    };
  }

  function setRequestError(caught: unknown) {
    setError(getErrorMessage(caught));
    setValidationErrors(caught instanceof DashboardApiError ? caught.errors : []);
  }

  function getValidationError(path: string): string | null {
    return validationErrors.find((item) => item.path === path)?.message ?? null;
  }

  const slugError = slug && (slug.length < 2 || slug.length > 100 || !/^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(slug)) ? "Use 2-100 lowercase letters, numbers, and single hyphens only." : ["new", "builder", "settings"].includes(slug) ? "This slug is reserved." : null;
  const publicationDirty = Boolean(dashboardDetail && (slug !== (dashboardDetail.publication.slug ?? "") || showInNavigation !== dashboardDetail.publication.showInNavigation || menuLabel !== (dashboardDetail.publication.menuLabel ?? "") || menuIcon !== (dashboardDetail.publication.menuIcon ?? "layout-dashboard") || menuOrder !== dashboardDetail.publication.menuOrder || viewPermission !== (dashboardDetail.publication.viewPermission ?? "")));

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
            <Button onClick={() => { setSelectedTemplateId(""); setTemplateGalleryOpen(true); }} variant="outline">
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
            {getValidationError("name") ? <p className="text-xs font-semibold text-danger">{getValidationError("name")}</p> : null}
            <Input label="Description" onChange={(event) => setDashboardDescription(event.target.value)} value={dashboardDescription} />
            <Select
              label="Visibility"
              onChange={(event) => setDashboardVisibility(event.target.value as DashboardVisibility)}
              options={visibilityOptions}
              value={dashboardVisibility}
            />
            <Checkbox
              checked={dashboardIsDefault}
              description="Workspace defaults are selected first for dashboard viewers."
              disabled={dashboardVisibility === "private"}
              label="Default dashboard"
              onChange={(event) => setDashboardIsDefault(event.target.checked)}
            />
            <div className="flex flex-wrap gap-2">
              <Badge>{widgets.length} widgets</Badge>
              <Badge>{sections.length} sections</Badge>
              <Badge tone={dashboardVisibility === "workspace" ? "info" : "warning"}>{getDashboardVisibilityLabel(dashboardVisibility)}</Badge>
              {dashboardIsDefault ? <Badge tone="success">Default</Badge> : null}
            </div>
            {dashboardDetail?.config.templateProvenance ? <p className="text-xs font-semibold text-muted-foreground">Created from {dashboardTemplateCatalog.find((item) => item.id === dashboardDetail.config.templateProvenance?.templateId)?.name ?? dashboardDetail.config.templateProvenance.templateId} v{dashboardDetail.config.templateProvenance.templateVersion}. This dashboard is independently editable.</p> : null}
            <div className="grid gap-3 border-t border-border pt-4">
              <div><p className="text-sm font-bold text-foreground">Publishing and navigation</p><p className="text-xs text-muted-foreground">Status: {dashboardDetail?.publication.status ?? "draft"}{dashboardDetail?.publishedAt ? ` · Published ${new Date(dashboardDetail.publishedAt).toLocaleString()}` : ""}</p></div>
              <Input label="URL slug" onChange={(event) => setSlug(event.target.value.toLowerCase())} value={slug} />
              {slugError || getValidationError("publication.slug") ? <p className="text-xs font-semibold text-danger">{slugError ?? getValidationError("publication.slug")}</p> : dashboardDetail?.publication.slug && dashboardDetail.publication.slug !== slug ? <p className="text-xs font-semibold text-warning">Changing the slug will break existing dashboard links.</p> : null}
              <Checkbox checked={showInNavigation} label="Show in navigation" onChange={(event) => setShowInNavigation(event.target.checked)} />
              {showInNavigation ? <><Input label="Menu label" onChange={(event) => setMenuLabel(event.target.value)} value={menuLabel} />{getValidationError("publication.menuLabel") ? <p className="text-xs font-semibold text-danger">{getValidationError("publication.menuLabel")}</p> : !menuLabel.trim() ? <p className="text-xs font-semibold text-muted-foreground">Defaults to the dashboard name.</p> : null}<Select label="Menu icon" onChange={(event) => setMenuIcon(event.target.value)} options={[{label:"Dashboard",value:"layout-dashboard"},{label:"Factory",value:"factory"},{label:"Landmark",value:"landmark"},{label:"Bar chart",value:"chart-column"},{label:"Trend",value:"chart-line"},{label:"Activity",value:"activity"},{label:"Business",value:"briefcase-business"}]} value={menuIcon} />{getValidationError("publication.menuIcon") ? <p className="text-xs font-semibold text-danger">{getValidationError("publication.menuIcon")}</p> : null}<Input label="Menu order" onChange={(event) => setMenuOrder(Number(event.target.value))} type="number" value={menuOrder} /></> : null}
              <Input label="Required view permission" onChange={(event) => setViewPermission(event.target.value)} value={viewPermission} />
              <div className="flex flex-wrap gap-2">
                {dashboardDetail?.publication.slug ? <Button onClick={() => window.open(`/dashboards/${dashboardDetail.publication.slug}`, "_blank")} variant="outline"><ExternalLink className="size-4" />Open dashboard</Button> : null}
                {dashboardDetail?.publication.slug ? <Button onClick={() => void navigator.clipboard.writeText(`${window.location.origin}/dashboards/${dashboardDetail.publication.slug}`)} variant="outline"><Copy className="size-4" />Copy link</Button> : null}
                <Button disabled={!dashboardDetail || saving || Boolean(slugError)} onClick={() => void handlePublicationAction()} variant={dashboardDetail?.publication.status === "published" ? "danger" : "primary"}>{dashboardDetail?.publication.status === "published" ? "Unpublish" : "Publish dashboard"}</Button>
              </div>
              {publicationDirty ? <p className="text-xs font-semibold text-warning">Pending publishing settings will be saved with the publish action.</p> : null}
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Widget builder</CardTitle>
            <CardDescription>Add analytics widgets to the saved dashboard layout.</CardDescription>
          </CardHeader>
          <CardContent className="grid gap-4">
            <div className="grid gap-3 rounded-xl border border-border p-4">
              <div>
                <p className="text-sm font-bold text-foreground">Sections</p>
                <p className="text-xs text-muted-foreground">Organize widgets into tabs in the published dashboard.</p>
              </div>
              {sections.map((section, index) => (
                <div className="grid gap-2 sm:grid-cols-[minmax(0,1fr)_auto]" key={section.id}>
                  <Input
                    aria-label={`Section ${index + 1} title`}
                    onChange={(event) => handleRenameSection(section.id, event.target.value)}
                    value={section.title}
                  />
                  <div className="flex gap-2">
                    <Button aria-label={`Move ${section.title} up`} disabled={index === 0} onClick={() => handleMoveSection(section.id, -1)} size="icon" variant="outline"><ArrowUp className="size-4" /></Button>
                    <Button aria-label={`Move ${section.title} down`} disabled={index === sections.length - 1} onClick={() => handleMoveSection(section.id, 1)} size="icon" variant="outline"><ArrowDown className="size-4" /></Button>
                    <Button aria-label={`Remove ${section.title}`} disabled={sections.length === 1} onClick={() => handleRemoveSection(section.id)} size="icon" variant="outline"><Trash2 className="size-4" /></Button>
                  </div>
                  {!section.title.trim() ? <p className="text-xs font-semibold text-danger sm:col-span-2">Section title is required.</p> : null}
                </div>
              ))}
              {getValidationError("config.sections") ? <p className="text-xs font-semibold text-danger">{getValidationError("config.sections")}</p> : null}
              <div className="grid gap-2 sm:grid-cols-[minmax(0,1fr)_auto]">
                <Input label="New section" onChange={(event) => setNewSectionTitle(event.target.value)} value={newSectionTitle} />
                <Button className="self-end" disabled={!newSectionTitle.trim()} onClick={handleAddSection} variant="outline"><Plus className="size-4" />Add section</Button>
              </div>
            </div>
            {adapters.length > 0 ? <Select label="Widget source" onChange={(event) => setWidgetSourceType(event.target.value as "analytics" | "adapter")} options={[{ label: "Platform analytics", value: "analytics" }, { label: "Installed adapter", value: "adapter" }]} value={widgetSourceType} /> : null}
            {widgetSourceType === "adapter" && adapterWidget ? <><Select label="Module" onChange={(event) => { const adapter = adapters.find((item) => item.id === event.target.value); const next = adapter ? createDashboardAdapterWidget(adapter) : null; if (next) setAdapterWidget(next); }} options={adapters.map((item) => ({ label: item.name, value: item.id }))} value={adapterWidget.adapterId} />{selectedAdapter ? <DashboardAdapterSettingsEditor adapter={selectedAdapter} onChange={setAdapterWidget} value={adapterWidget} /> : null}{!isDashboardAdapterWidgetConfigured(selectedAdapter, adapterWidget) ? <p className="text-xs font-semibold text-danger">Complete all required adapter settings before adding this widget.</p> : null}</> : null}
            {widgetSourceType === "analytics" ? <>
            <div className="grid gap-4 lg:grid-cols-4">
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
              <Select label="Section" onChange={(event) => setSelectedSectionId(event.target.value)} options={sections.map((section) => ({ label: section.title, value: section.id }))} value={selectedSectionId} />
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
            </> : <div className="grid gap-4 sm:grid-cols-2"><Input label="Widget title" onChange={(event) => setWidgetTitle(event.target.value)} value={widgetTitle} /><Select label="Section" onChange={(event) => setSelectedSectionId(event.target.value)} options={sections.map((section) => ({ label: section.title, value: section.id }))} value={selectedSectionId} /><Select label="Width" onChange={(event) => setWidgetWidth(event.target.value as DashboardWidgetWidth)} options={widthOptions} value={widgetWidth} /><Button disabled={!canAddWidget} onClick={() => void handleAddWidget()}><Plus className="size-4" />Add adapter widget</Button></div>}
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

            const analyticsWidgetType = widget.chart ? toDashboardAnalyticsWidgetType(widget.chart.widgetType) : null;
            const statusTone = getPreviewStatusTone(previewState);

            return (
              <Card className={getDashboardWidgetGridClass(layout.width)} key={layout.id}>
                <CardHeader>
                  <div className="flex flex-wrap items-start justify-between gap-3">
                    <div className="min-w-0">
                      <CardTitle className="break-words text-base">{widget.title}</CardTitle>
                      <CardDescription className="break-words">
                        {analyticsWidgetType && widget.chart ? `${getDashboardAnalyticsWidgetLabel(analyticsWidgetType)} · ${getMetricLabel(widget.chart.metric.type)}` : `Adapter · ${widget.adapter?.adapterId ?? "Unavailable"}`}
                      </CardDescription>
                    </div>
                    <div className="flex flex-wrap justify-end gap-2">
                      <Badge tone={widget.adapter ? (getDashboardAdapter(widget.adapter.adapterId) ? "success" : "danger") : statusTone}>{widget.adapter ? (getDashboardAdapter(widget.adapter.adapterId) ? "Adapter ready" : "Adapter unavailable") : getPreviewStatusLabel(previewState)}</Badge>
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
                <CardContent className="grid min-w-0 gap-4">
                  <Select
                    label="Section"
                    onChange={(event) => setWidgets((current) => current.map((item) => item.id === widget.id ? { ...item, sectionId: event.target.value } : item))}
                    options={sections.map((section) => ({ label: section.title, value: section.id }))}
                    value={widget.sectionId ?? sections[0].id}
                  />
                  {widget.adapter && getDashboardAdapter(widget.adapter.adapterId) ? (() => { const Renderer = getDashboardAdapter(widget.adapter!.adapterId)!.render; return <Renderer widget={widget} />; })() : <DashboardWidgetPreviewStateView state={previewState} onRefresh={() => void refreshWidgetPreview(widget)} />}
                </CardContent>
              </Card>
            );
          })
        )}
      </section>
      <DashboardTemplateGallery
        capabilityErrors={templateCapabilityErrors}
        creating={saving}
        forms={forms}
        onClose={() => setTemplateGalleryOpen(false)}
        onCreate={() => void handleCreateFromTemplate()}
        onSelectForm={setSelectedFormId}
        onSelectReport={setSelectedReportId}
        onSelectTemplate={setSelectedTemplateId}
        onStartBlank={() => { setTemplateGalleryOpen(false); handleNewDashboard(); }}
        open={templateGalleryOpen}
        reports={reports}
        selectedFormId={selectedFormId}
        selectedReportId={selectedReportId}
        selectedTemplateId={selectedTemplateId}
        templates={dashboardTemplateCatalog}
      />
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
  if (error instanceof DashboardApiError && error.errors.length > 0) {
    return `${error.message} ${error.errors.map((item) => `${item.path}: ${item.message}`).join(" ")}`;
  }

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

function createDashboardSlug(value: string): string {
  const slug = value.trim().toLowerCase().replace(/[^a-z0-9]+/g, "-").replace(/^-+|-+$/g, "").slice(0, 100).replace(/-+$/g, "");
  return slug.length >= 2 && !["new", "builder", "settings"].includes(slug) ? slug : `dashboard-${Date.now()}`;
}

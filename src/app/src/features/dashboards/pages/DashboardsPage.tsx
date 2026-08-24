import { useEffect, useMemo, useState } from "react";
import { ArrowDown, ArrowUp, Copy, ExternalLink, GripVertical, Pencil, Plus, RefreshCw, Save, Trash2 } from "lucide-react";
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
import { getDashboardAccentColor, resolveDashboardChartAppearance } from "../appearance";
import { DashboardAdapterSettingsEditor } from "../components/DashboardAdapterSettingsEditor";
import { DashboardAddWidgetWizard } from "../components/DashboardAddWidgetWizard";
import { DashboardTemplateGallery } from "../components/DashboardTemplateGallery";
import { DashboardWidgetPropertiesDrawer } from "../components/DashboardWidgetPropertiesDrawer";
import { createDashboardAdapterWidget, getDashboardAdapter, isDashboardAdapterWidgetConfigured, listDashboardAdapters } from "../adapters";
import { getDashboardWidgetGridClass, moveDashboardLayoutWidget, orderDashboardLayoutWidgets } from "../layout";
import { dispatchDashboardsChanged } from "../events";
import { assignWidgetsToDashboardSections, createDashboardSectionId, defaultDashboardSection, moveDashboardSection, normalizeDashboardSections } from "../sections";
import { instantiateDashboardTemplate, type DashboardTemplateSourceBinding } from "../templateEngine";
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

const widthOptions = dashboardWidgetWidths.map((width) => ({ label: width, value: width }));
const sectionIconOptions = ["activity", "badge-dollar-sign", "chart-column", "clipboard-list", "factory", "gauge", "heart-pulse", "package-check", "shield-check", "trending-up", "wrench"].map((icon) => ({ label: icon.replaceAll("-", " "), value: icon }));
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
  const [templateBindings, setTemplateBindings] = useState<Record<string, DashboardTemplateSourceBinding | undefined>>({});
  const [templateFieldIdsBySlot, setTemplateFieldIdsBySlot] = useState<Record<string, ReadonlySet<string> | undefined>>({});
  const [templateReportsBySlot, setTemplateReportsBySlot] = useState<Record<string, ListReportSummary[] | undefined>>({});
  const [templateLoadingSlots, setTemplateLoadingSlots] = useState<ReadonlySet<string>>(new Set());
  const [draggedSectionId, setDraggedSectionId] = useState<string | null>(null);
  const [draggedWidgetId, setDraggedWidgetId] = useState<string | null>(null);
  const [dropTargetId, setDropTargetId] = useState<string | null>(null);
  const [editingWidgetId, setEditingWidgetId] = useState<string | null>(null);

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
  const templateCapabilityErrors = selectedTemplate ? [
    ...validateTemplateFieldCapabilities(selectedTemplate, templateFieldIdsBySlot),
    ...selectedTemplate.sourceSlots.filter((slot) => templateLoadingSlots.has(slot.key)).map((slot) => ({ path: `sources.${slot.key}`, code: "template.source.loading", message: `Checking ${slot.label} reportable fields…` })),
    ...(selectedTemplate.requiredAdapterIds ?? []).filter((id) => !adapters.some((adapter) => adapter.id === id)).map((id) => ({ path: "requiredAdapterIds", code: "template.adapter.unavailable", message: `Required adapter '${id}' is not installed.` }))
  ] : [];

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

  async function handleAddWidget(): Promise<boolean> {
    if (!canAddWidget) return false;

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
      return true;
    } catch (caught) {
      setRequestError(caught);
      return false;
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

  function handleDuplicateWidget(widgetId: string) {
    const widget = widgets.find((item) => item.id === widgetId);
    const layout = layoutWidgets.find((item) => item.id === widgetId);
    if (!widget || !layout || widgets.length >= 48) return;
    const id = `widget-${Date.now()}`;
    setWidgets((current) => [...current, { ...widget, id, title: `${widget.title} copy`, chart: widget.chart ? { ...widget.chart, metric: { ...widget.chart.metric }, columns: [...(widget.chart.columns ?? [])], series: widget.chart.series?.map((series) => ({ ...series, metric: { ...series.metric } })) ?? null, appearance: widget.chart.appearance ? { ...widget.chart.appearance } : null } : null, adapter: widget.adapter ? { ...widget.adapter, settings: { ...widget.adapter.settings } } : null }]);
    setLayoutWidgets((current) => [...current, { id, width: layout.width, order: current.length + 1 }]);
    if (previewStates[widgetId]) setPreviewStates((current) => ({ ...current, [id]: current[widgetId] }));
    setNotice("Widget duplicated. Save the dashboard to persist it.");
  }

  function handleApplyWidgetProperties(nextWidget: SavedDashboardWidget, width: DashboardWidgetWidth, preview?: DashboardAnalyticsResponse) {
    setWidgets((current) => current.map((widget) => widget.id === nextWidget.id ? nextWidget : widget));
    setLayoutWidgets((current) => current.map((layout) => layout.id === nextWidget.id ? { ...layout, width } : layout));
    if (preview) setPreviewStates((current) => ({ ...current, [nextWidget.id]: { status: "ready", preview } }));
    else if (nextWidget.adapter) setPreviewStates((current) => ({ ...current, [nextWidget.id]: undefined }));
    setEditingWidgetId(null);
    setNotice("Widget properties applied. Save the dashboard to persist them.");
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

  function handleSectionIcon(sectionId: string, icon: string) {
    setSections((current) => current.map((section) => section.id === sectionId ? { ...section, icon: icon || null } : section));
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

  function handleDropSection(targetSectionId: string, sourceSectionId = draggedSectionId) {
    if (!sourceSectionId) return;
    setSections((current) => moveDashboardSection(current, sourceSectionId, targetSectionId));
    setDraggedSectionId(null);
    setDropTargetId(null);
    setNotice("Section order changed. Save the dashboard to persist it.");
  }

  function handleDropWidget(sectionId: string, targetWidgetId: string | null, sourceWidgetId = draggedWidgetId) {
    if (!sourceWidgetId) return;
    setWidgets((current) => current.map((widget) => widget.id === sourceWidgetId ? { ...widget, sectionId } : widget));
    setLayoutWidgets((current) => moveDashboardLayoutWidget(current, sourceWidgetId, targetWidgetId));
    setDraggedWidgetId(null);
    setDropTargetId(null);
    setNotice("Widget position changed. Save the dashboard to persist it.");
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
    if (!selectedTemplate || templateCapabilityErrors.length > 0) return;
    const sources = Object.fromEntries(Object.entries(templateBindings).filter((entry): entry is [string, DashboardTemplateSourceBinding] => Boolean(entry[1])));
    const instantiated = instantiateDashboardTemplate(selectedTemplate, {
      sources
    }, { availableAdapterIds: new Set(adapters.map((adapter) => adapter.id)) });
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

  async function handleSelectTemplateSource(slotKey: string, formId: string) {
    setTemplateBindings((current) => ({ ...current, [slotKey]: formId ? { formId, reportId: null } : undefined }));
    setTemplateFieldIdsBySlot((current) => ({ ...current, [slotKey]: undefined }));
    setTemplateReportsBySlot((current) => ({ ...current, [slotKey]: undefined }));
    if (!formId) return;

    setTemplateLoadingSlots((current) => new Set([...current, slotKey]));
    try {
      const [form, reportItems] = await Promise.all([getForm(formId), listReports(formId)]);
      setTemplateFieldIdsBySlot((current) => ({ ...current, [slotKey]: new Set(getReportableFields(form.draftSchema).map((field) => field.id)) }));
      setTemplateReportsBySlot((current) => ({ ...current, [slotKey]: reportItems }));
    } catch (caught) {
      setRequestError(caught);
    } finally {
      setTemplateLoadingSlots((current) => {
        const next = new Set(current);
        next.delete(slotKey);
        return next;
      });
    }
  }

  function handleSelectTemplateReport(slotKey: string, reportId: string) {
    setTemplateBindings((current) => {
      const binding = current[slotKey];
      return binding ? { ...current, [slotKey]: { ...binding, reportId: reportId || null } } : current;
    });
  }

  function handleSelectTemplate(templateId: string) {
    setSelectedTemplateId(templateId);
    setTemplateBindings({});
    setTemplateFieldIdsBySlot({});
    setTemplateReportsBySlot({});
    setTemplateLoadingSlots(new Set());
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
            <Button onClick={() => { handleSelectTemplate(""); setTemplateGalleryOpen(true); }} variant="outline">
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
                <div
                  className={`grid gap-2 rounded-lg border p-2 transition sm:grid-cols-[auto_minmax(0,1fr)_12rem_auto] ${dropTargetId === `section-${section.id}` ? "border-primary bg-primary/5" : "border-transparent"}`}
                  key={section.id}
                  onDragOver={(event) => { if (!draggedSectionId) return; event.preventDefault(); setDropTargetId(`section-${section.id}`); }}
                  onDrop={(event) => { event.preventDefault(); handleDropSection(section.id, event.dataTransfer.getData("application/x-dashboard-section") || draggedSectionId); }}
                >
                  <button aria-label={`Drag ${section.title} section`} className="flex cursor-grab items-center justify-center rounded-md px-2 text-muted-foreground hover:bg-muted active:cursor-grabbing" draggable onDragEnd={() => { setDraggedSectionId(null); setDropTargetId(null); }} onDragStart={(event) => { event.dataTransfer.setData("application/x-dashboard-section", section.id); setDraggedSectionId(section.id); }} type="button"><GripVertical className="size-5" /></button>
                  <Input
                    aria-label={`Section ${index + 1} title`}
                    onChange={(event) => handleRenameSection(section.id, event.target.value)}
                    value={section.title}
                  />
                  <Select aria-label={`${section.title} icon`} onChange={(event) => handleSectionIcon(section.id, event.target.value)} value={section.icon ?? ""}><option value="">No icon</option>{sectionIconOptions.map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}</Select>
                  <div className="flex gap-2">
                    <Button aria-label={`Move ${section.title} up`} disabled={index === 0} onClick={() => handleMoveSection(section.id, -1)} size="icon" variant="outline"><ArrowUp className="size-4" /></Button>
                    <Button aria-label={`Move ${section.title} down`} disabled={index === sections.length - 1} onClick={() => handleMoveSection(section.id, 1)} size="icon" variant="outline"><ArrowDown className="size-4" /></Button>
                    <Button aria-label={`Remove ${section.title}`} disabled={sections.length === 1} onClick={() => handleRemoveSection(section.id)} size="icon" variant="outline"><Trash2 className="size-4" /></Button>
                  </div>
                  {!section.title.trim() ? <p className="text-xs font-semibold text-danger sm:col-span-4">Section title is required.</p> : null}
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
            {widgetSourceType === "analytics" ? <DashboardAddWidgetWizard canAdd={canAddWidget} dateFieldId={dateFieldId} fields={fieldOptions} forms={forms} groupByFieldId={groupByFieldId} metricFieldId={metricFieldId} metricType={metricType} onAdd={handleAddWidget} onColumnsChange={handleToggleColumn} onDateFieldChange={setDateFieldId} onFormChange={setSelectedFormId} onGroupFieldChange={setGroupByFieldId} onMetricFieldChange={setMetricFieldId} onMetricTypeChange={setMetricType} onReportChange={setSelectedReportId} onSectionChange={setSelectedSectionId} onTitleChange={setWidgetTitle} onTypeChange={setWidgetType} onWidthChange={setWidgetWidth} reports={reports} sections={sections} selectedColumns={selectedColumns} selectedFormId={selectedFormId} selectedReportId={selectedReportId} selectedSectionId={selectedSectionId} title={widgetTitle} type={widgetType} width={widgetWidth} /> : <div className="grid gap-4 sm:grid-cols-2"><Input label="Widget title" onChange={(event) => setWidgetTitle(event.target.value)} value={widgetTitle} /><Select label="Section" onChange={(event) => setSelectedSectionId(event.target.value)} options={sections.map((section) => ({ label: section.title, value: section.id }))} value={selectedSectionId} /><Select label="Width" onChange={(event) => setWidgetWidth(event.target.value as DashboardWidgetWidth)} options={widthOptions} value={widgetWidth} /><Button disabled={!canAddWidget} onClick={() => void handleAddWidget()}><Plus className="size-4" />Add adapter widget</Button></div>}
          </CardContent>
        </Card>
      </section>

      <section className="grid gap-4 md:grid-cols-12">
        <div className="md:col-span-12 flex flex-wrap items-end justify-between gap-3">
          <div><p className="text-lg font-bold text-foreground">Layout canvas</p><p className="mt-1 text-sm text-muted-foreground">Drag widget handles to reorder cards or move them between section drop zones. Arrow controls remain available for keyboard-friendly ordering.</p></div>
          <Badge tone="info">Drag-and-drop enabled</Badge>
        </div>
        {orderedLayout.length === 0 ? (
          <div className="md:col-span-12">
            <EmptyState title="No dashboard widgets" description="Add a widget and save the dashboard." action={<Button disabled={!canAddWidget} onClick={() => void handleAddWidget()} variant="outline">Add widget</Button>} />
          </div>
        ) : (
          sections.flatMap((section) => [
            <div
              aria-label={`Move widget to ${section.title}`}
              className={`md:col-span-12 flex min-h-16 items-center justify-between rounded-xl border-2 border-dashed px-4 py-3 transition ${dropTargetId === `widget-section-${section.id}` ? "border-primary bg-primary/10" : "border-border bg-muted/20"}`}
              key={`canvas-${section.id}`}
              onDragOver={(event) => { if (!draggedWidgetId) return; event.preventDefault(); setDropTargetId(`widget-section-${section.id}`); }}
              onDrop={(event) => { event.preventDefault(); handleDropWidget(section.id, null, event.dataTransfer.getData("application/x-dashboard-widget") || draggedWidgetId); }}
            >
              <div><p className="text-sm font-bold text-foreground">{section.title}</p><p className="text-xs text-muted-foreground">Drop a widget here to move it into this section.</p></div>
              <Badge>{widgets.filter((widget) => widget.sectionId === section.id).length} widgets</Badge>
            </div>,
            ...orderedLayout.filter((layout) => widgets.find((widget) => widget.id === layout.id)?.sectionId === section.id).map((layout) => {
            const widget = widgets.find((candidate) => candidate.id === layout.id);
            const previewState = previewStates[layout.id];

            if (!widget) return null;

            const analyticsWidgetType = widget.chart ? toDashboardAnalyticsWidgetType(widget.chart.widgetType) : null;
            const statusTone = getPreviewStatusTone(previewState);
            const appearance = resolveDashboardChartAppearance(widget.chart?.appearance);
            const accent = getDashboardAccentColor(appearance.cardAccent, appearance.palette);

            return (
              <Card
                className={`${getDashboardWidgetGridClass(layout.width)} min-w-0 transition ${dropTargetId === `widget-${layout.id}` ? "ring-2 ring-primary ring-offset-2" : ""}`}
                key={layout.id}
                onDragOver={(event) => { if (!draggedWidgetId) return; event.preventDefault(); event.stopPropagation(); setDropTargetId(`widget-${layout.id}`); }}
                onDrop={(event) => { event.preventDefault(); event.stopPropagation(); handleDropWidget(section.id, layout.id, event.dataTransfer.getData("application/x-dashboard-widget") || draggedWidgetId); }}
                style={accent ? { borderTopColor: accent, borderTopWidth: 4 } : undefined}
              >
                <CardHeader>
                  <div className="flex flex-wrap items-start justify-between gap-3">
                    <div className="flex min-w-0 items-start gap-2">
                      <button aria-label={`Drag ${widget.title} widget`} className="mt-0.5 cursor-grab rounded p-1 text-muted-foreground hover:bg-muted active:cursor-grabbing" draggable onDragEnd={() => { setDraggedWidgetId(null); setDropTargetId(null); }} onDragStart={(event) => { event.dataTransfer.effectAllowed = "move"; event.dataTransfer.setData("application/x-dashboard-widget", layout.id); setDraggedWidgetId(layout.id); }} type="button"><GripVertical className="size-5" /></button>
                      <div className="min-w-0">
                      <CardTitle className="break-words text-base">{widget.title}</CardTitle>
                      <CardDescription className="break-words">
                        {analyticsWidgetType && widget.chart ? `${getDashboardAnalyticsWidgetLabel(analyticsWidgetType)} · ${getMetricLabel(widget.chart.metric.type)}` : `Adapter · ${widget.adapter?.adapterId ?? "Unavailable"}`}
                      </CardDescription>
                      </div>
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
                      <Button aria-label="Duplicate widget" disabled={widgets.length >= 48} onClick={() => handleDuplicateWidget(layout.id)} size="icon" variant="outline"><Copy className="size-4" /></Button>
                      <Button aria-label={`Edit ${widget.title} properties`} onClick={() => setEditingWidgetId(layout.id)} size="icon" variant="outline"><Pencil className="size-4" /></Button>
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
                  <Select label="Width" onChange={(event) => setLayoutWidgets((current) => current.map((item) => item.id === layout.id ? { ...item, width: event.target.value as DashboardWidgetWidth } : item))} options={widthOptions} value={layout.width} />
                  {widget.adapter && getDashboardAdapter(widget.adapter.adapterId) ? (() => { const Renderer = getDashboardAdapter(widget.adapter!.adapterId)!.render; return <Renderer widget={widget} />; })() : <DashboardWidgetPreviewStateView appearance={widget.chart?.appearance} state={previewState} onRefresh={() => void refreshWidgetPreview(widget)} />}
                </CardContent>
              </Card>
            );
          })])
        )}
      </section>
      <DashboardTemplateGallery
        capabilityErrors={templateCapabilityErrors}
        creating={saving}
        forms={forms}
        onClose={() => setTemplateGalleryOpen(false)}
        onCreate={() => void handleCreateFromTemplate()}
        onSelectSource={(slotKey, formId) => void handleSelectTemplateSource(slotKey, formId)}
        onSelectReport={handleSelectTemplateReport}
        onSelectTemplate={handleSelectTemplate}
        onStartBlank={() => { setTemplateGalleryOpen(false); handleNewDashboard(); }}
        open={templateGalleryOpen}
        sourceBindings={templateBindings}
        sourceReports={templateReportsBySlot}
        selectedTemplateId={selectedTemplateId}
        templates={dashboardTemplateCatalog}
      />
      <DashboardWidgetPropertiesDrawer
        adapters={adapters}
        forms={forms}
        layout={layoutWidgets.find((layout) => layout.id === editingWidgetId) ?? null}
        onApply={handleApplyWidgetProperties}
        onClose={() => setEditingWidgetId(null)}
        open={Boolean(editingWidgetId)}
        sections={sections}
        widget={widgets.find((widget) => widget.id === editingWidgetId) ?? null}
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

function DashboardWidgetPreviewStateView({ appearance, state, onRefresh }: { appearance?: ChartWidgetConfig["appearance"]; state?: DashboardPreviewState; onRefresh: () => void }) {
  if (state?.status === "ready" && state.preview) {
    return <ChartWidgetPreview appearance={appearance} preview={state.preview} />;
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

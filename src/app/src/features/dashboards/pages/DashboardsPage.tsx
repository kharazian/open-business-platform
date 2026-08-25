import { lazy, Suspense, useEffect, useMemo, useState, type Dispatch, type KeyboardEvent, type SetStateAction } from "react";
import { ArrowDown, ArrowLeft, ArrowRight, ArrowUp, ChevronDown, ChevronRight, Copy, Eye, ExternalLink, GitCompare, GripVertical, History, Keyboard, Move, Pencil, Plus, Redo2, RefreshCw, RotateCcw, Save, Trash2, Undo2, X } from "lucide-react";
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
import { createDashboard, DashboardApiError, deleteDashboard, getDashboard, getDashboardPublishedComparison, getDashboardSharing, getDashboardSharingOptions, listDashboardRevisions, listDashboards, publishDashboard, restoreDashboardRevision, runDashboardAnalytics, unpublishDashboard, updateDashboard } from "../api";
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
import { DashboardFilterEditor } from "../components/DashboardFilterEditor";
import { DashboardTemplateGallery } from "../components/DashboardTemplateGallery";
import { DashboardWidgetPropertiesDrawer } from "../components/DashboardWidgetPropertiesDrawer";
import { SavedDashboardViewer } from "../components/SavedDashboardViewer";
import { createDashboardAdapterWidget, getDashboardAdapter, isDashboardAdapterWidgetConfigured, listDashboardAdapters } from "../adapters";
import { getDashboardWidgetGridClass, moveDashboardLayoutWidget, orderDashboardLayoutWidgets } from "../layout";
import { appendBoundedCanvasHistory, canDuplicateDashboardSection, dashboardCanvasQualityLimits, getAdjacentDashboardSectionId, moveDashboardWidgetWithinSection, runDashboardTasksWithConcurrency, toggleDashboardWidgetSelection } from "../canvasProductivity";
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
  type DashboardFilterDefinition,
  type DashboardPublishedComparison,
  type DashboardRevisionSnapshot,
  type DashboardRevisionSummary,
  type DashboardSharingOption,
  type DashboardSharingOptions,
  type DashboardSettings,
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
type DashboardAudience = "workspace" | "restricted" | "private";
const audienceOptions: Array<{ label: string; value: DashboardAudience }> = [
  { label: "Everyone in this workspace", value: "workspace" },
  { label: "Specific people, roles, or groups", value: "restricted" },
  { label: "Private — only me and dashboard managers", value: "private" }
];
const emptySharingOptions: DashboardSharingOptions = { users: [], roles: [], groups: [] };
const DashboardRecycleBinModal = lazy(() => import("../components/DashboardRecycleBinModal").then((module) => ({ default: module.DashboardRecycleBinModal })));

type CanvasSnapshot = { sections: SavedDashboardSection[]; widgets: SavedDashboardWidget[]; filters: DashboardFilterDefinition[]; layout: SavedDashboardWidgetLayout[]; previews: Record<string, DashboardPreviewState | undefined> };

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
  const [dashboardAudience, setDashboardAudience] = useState<DashboardAudience>("workspace");
  const [dashboardIsDefault, setDashboardIsDefault] = useState(false);
  const [viewerUserIds, setViewerUserIds] = useState<string[]>([]);
  const [viewerRoleIds, setViewerRoleIds] = useState<string[]>([]);
  const [viewerGroupIds, setViewerGroupIds] = useState<string[]>([]);
  const [sharingOptions, setSharingOptions] = useState<DashboardSharingOptions>(emptySharingOptions);
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
  const [filters, setFilters] = useState<DashboardFilterDefinition[]>([]);
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
  const [undoStack, setUndoStack] = useState<CanvasSnapshot[]>([]);
  const [redoStack, setRedoStack] = useState<CanvasSnapshot[]>([]);
  const [selectedWidgetIds, setSelectedWidgetIds] = useState<Set<string>>(new Set());
  const [collapsedSectionIds, setCollapsedSectionIds] = useState<Set<string>>(new Set());
  const [bulkSectionId, setBulkSectionId] = useState(defaultDashboardSection.id);
  const [bulkWidth, setBulkWidth] = useState<DashboardWidgetWidth>("medium");
  const [canvasDensity, setCanvasDensity] = useState<"comfortable" | "compact">("comfortable");
  const [canvasZoom, setCanvasZoom] = useState(100);
  const [publishedComparison, setPublishedComparison] = useState<DashboardPublishedComparison | null>(null);
  const [revisions, setRevisions] = useState<DashboardRevisionSummary[]>([]);
  const [restoringRevisionId, setRestoringRevisionId] = useState("");
  const [previewOpen, setPreviewOpen] = useState(false);
  const [savedSignature, setSavedSignature] = useState("");
  const [canvasAnnouncement, setCanvasAnnouncement] = useState("");
  const [keyboardGrabbed, setKeyboardGrabbed] = useState<{ kind: "section" | "widget"; id: string } | null>(null);
  const [touchReorderEnabled, setTouchReorderEnabled] = useState(false);
  const [recycleBinOpen, setRecycleBinOpen] = useState(false);

  const draftSignature = JSON.stringify(buildSaveRequest());
  const isDirty = dashboardDetail
    ? savedSignature !== draftSignature
    : dashboardName !== "New dashboard" || dashboardDescription !== "" || widgets.length > 0 || filters.length > 0 || sections.length > 1;

  useEffect(() => {
    if (!isDirty) return;
    const warnBeforeUnload = (event: BeforeUnloadEvent) => { event.preventDefault(); event.returnValue = ""; };
    window.addEventListener("beforeunload", warnBeforeUnload);
    return () => window.removeEventListener("beforeunload", warnBeforeUnload);
  }, [isDirty]);

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
      const [dashboardItems, formItems, availableSharingOptions] = await Promise.all([listDashboards(), listForms(), getDashboardSharingOptions()]);
      setDashboards(dashboardItems);
      setForms(formItems);
      setSharingOptions(availableSharingOptions);
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
      const [detail, sharing] = await Promise.all([getDashboard(dashboardId), getDashboardSharing(dashboardId)]);
      setDashboardDetail(detail);
      setDashboardName(detail.name);
      setDashboardDescription(detail.description ?? "");
      const settings = normalizeDashboardSettings({ visibility: detail.visibility, isDefault: detail.isDefault, viewerUserIds: sharing.userIds, viewerRoleIds: sharing.roleIds, viewerGroupIds: sharing.groupIds });
      setDashboardVisibility(settings.visibility);
      setDashboardAudience(settings.visibility === "private" ? "private" : settings.viewerUserIds.length + settings.viewerRoleIds.length + settings.viewerGroupIds.length > 0 ? "restricted" : "workspace");
      setDashboardIsDefault(settings.isDefault);
      setViewerUserIds(settings.viewerUserIds); setViewerRoleIds(settings.viewerRoleIds); setViewerGroupIds(settings.viewerGroupIds);
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
      setBulkSectionId(nextSections[0].id);
      setWidgets(nextWidgets);
      setFilters(detail.config.filters ?? []);
      setLayoutWidgets(detail.layout.widgets);
      setSavedSignature(JSON.stringify(toDashboardSaveRequest(detail, settings)));
      setKeyboardGrabbed(null); setCanvasAnnouncement("");
      setUndoStack([]); setRedoStack([]); setSelectedWidgetIds(new Set()); setCollapsedSectionIds(new Set());
      await loadPublishingMetadata(detail.id);
      await loadPreviews(nextWidgets);
    } catch (caught) {
      setRequestError(caught);
    }
  }

  async function loadPublishingMetadata(dashboardId: string) {
    const [comparison, revisionItems] = await Promise.all([
      getDashboardPublishedComparison(dashboardId),
      listDashboardRevisions(dashboardId)
    ]);
    setPublishedComparison(comparison);
    setRevisions(revisionItems);
  }

  async function loadPreviews(nextWidgets: SavedDashboardWidget[]) {
    if (nextWidgets.length === 0) {
      setPreviewStates({});
      return;
    }

    setPreviewStates(createDashboardPreviewStates(nextWidgets));

    await runDashboardTasksWithConcurrency(nextWidgets, (widget) => refreshWidgetPreview(widget, false));
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

  function currentCanvasSnapshot(): CanvasSnapshot { return { sections: [...sections], widgets: [...widgets], filters: [...filters], layout: [...layoutWidgets], previews: { ...previewStates } }; }
  function recordCanvasHistory() { const snapshot = currentCanvasSnapshot(); setUndoStack((current) => appendBoundedCanvasHistory(current, snapshot)); setRedoStack([]); }
  function restoreCanvasSnapshot(snapshot: CanvasSnapshot) { setSections(snapshot.sections); setWidgets(snapshot.widgets); setFilters(snapshot.filters); setLayoutWidgets(snapshot.layout); setPreviewStates(snapshot.previews); setSelectedWidgetIds(new Set()); }
  function handleUndo() { const snapshot = undoStack.at(-1); if (!snapshot) return; const current = currentCanvasSnapshot(); setUndoStack((items) => items.slice(0, -1)); setRedoStack((items) => appendBoundedCanvasHistory(items, current)); restoreCanvasSnapshot(snapshot); setNotice("Canvas change undone."); }
  function handleRedo() { const snapshot = redoStack.at(-1); if (!snapshot) return; const current = currentCanvasSnapshot(); setRedoStack((items) => items.slice(0, -1)); setUndoStack((items) => appendBoundedCanvasHistory(items, current)); restoreCanvasSnapshot(snapshot); setNotice("Canvas change restored."); }

  async function handleAddWidget(): Promise<boolean> {
    if (!canAddWidget) return false;

    const id = `widget-${Date.now()}`;
    const chart = widgetSourceType === "analytics" ? buildChartConfig() : null;
    const widget: SavedDashboardWidget = { id, title: widgetTitle.trim(), sourceFormId: widgetSourceType === "analytics" ? selectedFormId : null, chart, sectionId: selectedSectionId, adapter: widgetSourceType === "adapter" ? adapterWidget : null };

    setError(null);

    try {
      const preview = chart ? await runDashboardAnalytics(buildDashboardAnalyticsRequest(selectedFormId, chart)) : null;
      recordCanvasHistory();
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
    recordCanvasHistory();
    setWidgets((current) => current.filter((widget) => widget.id !== widgetId));
    setFilters((current) => current.map((filter) => filter.applyToWidgetIds ? { ...filter, applyToWidgetIds: filter.applyToWidgetIds.filter((id) => id !== widgetId) } : filter));
    setLayoutWidgets((current) => current.filter((item) => item.id !== widgetId).map((item, index) => ({ ...item, order: index + 1 })));
    setPreviewStates((current) => {
      const next = { ...current };
      delete next[widgetId];
      return next;
    });
  }

  function handleMoveWidget(widgetId: string, direction: -1 | 1) {
    const next = moveDashboardWidgetWithinSection(layoutWidgets, widgets, widgetId, direction);
    if (next === layoutWidgets) return;
    recordCanvasHistory();
    setLayoutWidgets(next);
    const widget = widgets.find((item) => item.id === widgetId);
    announceCanvasChange(`${widget?.title ?? "Widget"} moved ${direction < 0 ? "up" : "down"}.`);
  }

  function handleDuplicateWidget(widgetId: string) {
    const widget = widgets.find((item) => item.id === widgetId);
    const layout = layoutWidgets.find((item) => item.id === widgetId);
    if (!widget || !layout || widgets.length >= dashboardCanvasQualityLimits.maxWidgets) return;
    recordCanvasHistory();
    const id = `widget-${Date.now()}`;
    setWidgets((current) => [...current, { ...widget, id, title: `${widget.title} copy`, chart: widget.chart ? { ...widget.chart, metric: { ...widget.chart.metric }, columns: [...(widget.chart.columns ?? [])], series: widget.chart.series?.map((series) => ({ ...series, metric: { ...series.metric } })) ?? null, appearance: widget.chart.appearance ? { ...widget.chart.appearance } : null } : null, adapter: widget.adapter ? { ...widget.adapter, settings: { ...widget.adapter.settings } } : null }]);
    setLayoutWidgets((current) => [...current, { id, width: layout.width, order: current.length + 1 }]);
    if (previewStates[widgetId]) setPreviewStates((current) => ({ ...current, [id]: current[widgetId] }));
    setNotice("Widget duplicated. Save the dashboard to persist it.");
  }

  function handleApplyWidgetProperties(nextWidget: SavedDashboardWidget, width: DashboardWidgetWidth, preview?: DashboardAnalyticsResponse) {
    recordCanvasHistory();
    const previousWidget = widgets.find((widget) => widget.id === nextWidget.id);
    setWidgets((current) => current.map((widget) => widget.id === nextWidget.id ? nextWidget : widget));
    if (previousWidget?.sourceFormId !== nextWidget.sourceFormId) setFilters((current) => current.map((filter) => filter.applyToWidgetIds && filter.sourceFormId !== nextWidget.sourceFormId ? { ...filter, applyToWidgetIds: filter.applyToWidgetIds.filter((id) => id !== nextWidget.id) } : filter));
    setLayoutWidgets((current) => current.map((layout) => layout.id === nextWidget.id ? { ...layout, width } : layout));
    if (preview) setPreviewStates((current) => ({ ...current, [nextWidget.id]: { status: "ready", preview } }));
    else if (nextWidget.adapter) setPreviewStates((current) => ({ ...current, [nextWidget.id]: undefined }));
    setEditingWidgetId(null);
    setNotice("Widget properties applied. Save the dashboard to persist them.");
  }

  function handleAddSection() {
    const title = newSectionTitle.trim();
    if (!title) return;
    if (sections.length >= dashboardCanvasQualityLimits.maxSections) return;
    recordCanvasHistory();
    const section = { id: createDashboardSectionId(title, sections), title, order: sections.length };
    setSections((current) => [...current, section]);
    setSelectedSectionId(section.id);
    setNewSectionTitle("");
  }

  function handleRenameSection(sectionId: string, title: string) {
    recordCanvasHistory();
    setSections((current) => current.map((section) => section.id === sectionId ? { ...section, title } : section));
  }

  function handleSectionIcon(sectionId: string, icon: string) {
    recordCanvasHistory();
    setSections((current) => current.map((section) => section.id === sectionId ? { ...section, icon: icon || null } : section));
  }

  function handleMoveSection(sectionId: string, direction: -1 | 1) {
    const index = sections.findIndex((section) => section.id === sectionId);
    const targetIndex = index + direction;
    if (index < 0 || targetIndex < 0 || targetIndex >= sections.length) return;
    recordCanvasHistory();
    const next = [...sections];
    [next[index], next[targetIndex]] = [next[targetIndex], next[index]];
    setSections(next.map((section, order) => ({ ...section, order })));
    announceCanvasChange(`${sections[index].title} section moved ${direction < 0 ? "up" : "down"}.`);
  }

  function handleDropSection(targetSectionId: string, sourceSectionId = draggedSectionId) {
    if (!sourceSectionId) return;
    recordCanvasHistory();
    setSections((current) => moveDashboardSection(current, sourceSectionId, targetSectionId));
    setDraggedSectionId(null);
    setDropTargetId(null);
    setNotice("Section order changed. Save the dashboard to persist it.");
    announceCanvasChange(`${sections.find((section) => section.id === sourceSectionId)?.title ?? "Section"} moved.`);
  }

  function handleDropWidget(sectionId: string, targetWidgetId: string | null, sourceWidgetId = draggedWidgetId) {
    if (!sourceWidgetId) return;
    recordCanvasHistory();
    setWidgets((current) => current.map((widget) => widget.id === sourceWidgetId ? { ...widget, sectionId } : widget));
    setLayoutWidgets((current) => moveDashboardLayoutWidget(current, sourceWidgetId, targetWidgetId));
    setDraggedWidgetId(null);
    setDropTargetId(null);
    setNotice("Widget position changed. Save the dashboard to persist it.");
    announceCanvasChange(`${widgets.find((widget) => widget.id === sourceWidgetId)?.title ?? "Widget"} moved to ${sections.find((section) => section.id === sectionId)?.title ?? "section"}.`);
  }

  function handleMoveWidgetToAdjacentSection(widgetId: string, direction: -1 | 1) {
    const widget = widgets.find((item) => item.id === widgetId);
    const sectionId = getAdjacentDashboardSectionId(sections, widget?.sectionId, direction);
    if (!widget || !sectionId) return;
    handleDropWidget(sectionId, null, widgetId);
  }

  function announceCanvasChange(message: string) {
    setCanvasAnnouncement("");
    window.setTimeout(() => setCanvasAnnouncement(message), 20);
  }

  function handleReorderKeyboard(event: KeyboardEvent<HTMLButtonElement>, kind: "section" | "widget", id: string) {
    const grabbed = keyboardGrabbed?.kind === kind && keyboardGrabbed.id === id;
    if (event.key === " " || event.key === "Enter") {
      event.preventDefault();
      const next = grabbed ? null : { kind, id } as const;
      setKeyboardGrabbed(next);
      announceCanvasChange(grabbed ? `${kind} released.` : `${kind} picked up. Use arrow keys to move it, then Space to release.`);
      return;
    }
    if (!grabbed) return;
    if (event.key === "Escape") {
      event.preventDefault(); setKeyboardGrabbed(null); announceCanvasChange(`${kind} reorder cancelled.`); return;
    }
    if (event.key === "ArrowUp" || event.key === "ArrowDown") {
      event.preventDefault();
      const direction = event.key === "ArrowUp" ? -1 : 1;
      if (kind === "section") handleMoveSection(id, direction);
      else handleMoveWidget(id, direction);
    }
    if (kind === "widget" && (event.key === "ArrowLeft" || event.key === "ArrowRight")) {
      event.preventDefault(); handleMoveWidgetToAdjacentSection(id, event.key === "ArrowLeft" ? -1 : 1);
    }
  }

  function handleRemoveSection(sectionId: string) {
    if (sections.length === 1) return;
    recordCanvasHistory();
    const nextSections = sections.filter((section) => section.id !== sectionId).map((section, order) => ({ ...section, order }));
    const fallbackSectionId = nextSections[0].id;
    setSections(nextSections);
    setWidgets((current) => current.map((widget) => widget.sectionId === sectionId ? { ...widget, sectionId: fallbackSectionId } : widget));
    if (selectedSectionId === sectionId) setSelectedSectionId(fallbackSectionId);
  }

  function handleDuplicateSection(sectionId: string) {
    const source = sections.find((section) => section.id === sectionId);
    const sourceWidgets = widgets.filter((widget) => widget.sectionId === sectionId);
    if (!source || !canDuplicateDashboardSection(sections, widgets, sectionId)) return;
    recordCanvasHistory();
    const section = { ...source, id: createDashboardSectionId(`${source.title} copy`, sections), title: `${source.title} copy`, order: sections.length };
    const idMap = new Map(sourceWidgets.map((widget, index) => [widget.id, `widget-${Date.now()}-${index}`]));
    const copies = sourceWidgets.map((widget) => ({ ...widget, id: idMap.get(widget.id)!, title: `${widget.title} copy`, sectionId: section.id, chart: widget.chart ? { ...widget.chart, metric: { ...widget.chart.metric }, columns: [...(widget.chart.columns ?? [])], series: widget.chart.series?.map((series) => ({ ...series, metric: { ...series.metric } })) ?? null, appearance: widget.chart.appearance ? { ...widget.chart.appearance } : null } : null, adapter: widget.adapter ? { ...widget.adapter, settings: { ...widget.adapter.settings } } : null }));
    const nextLayouts = sourceWidgets.map((widget, index) => { const layout = layoutWidgets.find((item) => item.id === widget.id); return { id: idMap.get(widget.id)!, width: layout?.width ?? "medium" as DashboardWidgetWidth, order: layoutWidgets.length + index + 1 }; });
    setSections((current) => [...current, section]); setWidgets((current) => [...current, ...copies]); setLayoutWidgets((current) => [...current, ...nextLayouts]);
    setPreviewStates((current) => { const next = { ...current }; sourceWidgets.forEach((widget) => { next[idMap.get(widget.id)!] = current[widget.id]; }); return next; });
    setCollapsedSectionIds((current) => { const next = new Set(current); next.delete(section.id); return next; });
    setNotice("Section duplicated. Save the dashboard to persist it.");
  }

  function toggleWidgetSelection(widgetId: string) { setSelectedWidgetIds((current) => toggleDashboardWidgetSelection(current, widgetId)); }
  function handleBulkMove() { if (!selectedWidgetIds.size || !sections.some((section) => section.id === bulkSectionId)) return; if (widgets.filter((widget) => widget.sectionId === bulkSectionId && !selectedWidgetIds.has(widget.id)).length + selectedWidgetIds.size > 16) { setError("The destination section supports at most 16 widgets."); return; } recordCanvasHistory(); setWidgets((current) => current.map((widget) => selectedWidgetIds.has(widget.id) ? { ...widget, sectionId: bulkSectionId } : widget)); setNotice(`${selectedWidgetIds.size} widgets moved.`); }
  function handleBulkResize() { if (!selectedWidgetIds.size) return; recordCanvasHistory(); setLayoutWidgets((current) => current.map((layout) => selectedWidgetIds.has(layout.id) ? { ...layout, width: bulkWidth } : layout)); setNotice(`${selectedWidgetIds.size} widgets resized.`); }
  function handleBulkDelete() { if (!selectedWidgetIds.size) return; recordCanvasHistory(); setWidgets((current) => current.filter((widget) => !selectedWidgetIds.has(widget.id))); setFilters((current) => current.map((filter) => filter.applyToWidgetIds ? { ...filter, applyToWidgetIds: filter.applyToWidgetIds.filter((id) => !selectedWidgetIds.has(id)) } : filter)); setLayoutWidgets((current) => current.filter((layout) => !selectedWidgetIds.has(layout.id)).map((layout, order) => ({ ...layout, order: order + 1 }))); setPreviewStates((current) => Object.fromEntries(Object.entries(current).filter(([id]) => !selectedWidgetIds.has(id)))); setSelectedWidgetIds(new Set()); setNotice("Selected widgets removed. Use Undo to restore them."); }
  function handleWidgetSectionChange(widgetId: string, sectionId: string) { recordCanvasHistory(); setWidgets((current) => current.map((item) => item.id === widgetId ? { ...item, sectionId } : item)); }
  function handleWidgetWidthChange(widgetId: string, width: DashboardWidgetWidth) { recordCanvasHistory(); setLayoutWidgets((current) => current.map((item) => item.id === widgetId ? { ...item, width } : item)); }
  function toggleSectionCollapsed(sectionId: string) { setCollapsedSectionIds((current) => { const next = new Set(current); if (next.has(sectionId)) next.delete(sectionId); else next.add(sectionId); return next; }); }

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
      setSavedSignature(JSON.stringify(toDashboardSaveRequest(saved, request.settings)));
      setSelectedDashboardId(saved.id);
      navigate(`/dashboard-builder/${saved.id}`, { replace: true });
      const settings = request.settings;
      setDashboardVisibility(settings.visibility);
      setDashboardIsDefault(settings.isDefault);
      setSections(normalizeDashboardSections(saved.config.sections));
      setWidgets(saved.config.widgets);
      setFilters(saved.config.filters ?? []);
      setLayoutWidgets(saved.layout.widgets);
      setDashboards(await listDashboards());
      await loadPublishingMetadata(saved.id);
      dispatchDashboardsChanged();
      await loadPreviews(saved.config.widgets);
      setNotice("Dashboard saved.");
    } catch (caught) {
      setRequestError(caught);
    } finally {
      setSaving(false);
    }
  }

  function handleNewDashboard(force = false) {
    if (!force && !confirmDiscardChanges()) return;
    setSelectedDashboardId("");
    setDashboardDetail(null);
    setDashboardName("New dashboard");
    setDashboardDescription("");
    setDashboardVisibility("workspace");
    setDashboardAudience("workspace");
    setDashboardIsDefault(false);
    setViewerUserIds([]); setViewerRoleIds([]); setViewerGroupIds([]);
    setSlug(""); setShowInNavigation(false); setMenuLabel(""); setMenuIcon("layout-dashboard"); setMenuOrder(0); setViewPermission("");
    setSections([defaultDashboardSection]);
    setSelectedSectionId(defaultDashboardSection.id);
    setBulkSectionId(defaultDashboardSection.id);
    setNewSectionTitle("");
    setWidgets([]);
    setFilters([]);
    setLayoutWidgets([]);
    setPreviewStates({});
    setPublishedComparison(null); setRevisions([]); setSavedSignature("");
    setKeyboardGrabbed(null); setCanvasAnnouncement("");
    setUndoStack([]); setRedoStack([]); setSelectedWidgetIds(new Set()); setCollapsedSectionIds(new Set());
    setNotice("New dashboard draft started.");
  }

  async function handleDuplicateDashboard() {
    if (!dashboardDetail || !window.confirm(`Create an independent draft copy of “${dashboardName}” using the current editor content?`)) return;
    setSaving(true); setError(null); setValidationErrors([]);
    try {
      const request = buildSaveRequest();
      const saved = await createDashboard({
        ...request,
        name: `${dashboardName.trim() || "Dashboard"} copy`,
        settings: { ...request.settings, isDefault: false },
        publication: { ...request.publication, status: "draft", slug: null, showInNavigation: false, menuLabel: null }
      });
      setDashboards(await listDashboards());
      setSelectedDashboardId(saved.id);
      navigate(`/dashboard-builder/${saved.id}`);
      setNotice("Independent dashboard draft created.");
      dispatchDashboardsChanged();
    } catch (caught) { setRequestError(caught); } finally { setSaving(false); }
  }

  async function handleArchiveDashboard() {
    if (!dashboardDetail) return;
    const consequence = dashboardDetail.publication.status === "published" ? " It will immediately disappear from live links, the directory, and navigation." : "";
    if (!window.confirm(`Archive “${dashboardName}”?${consequence} This action is recorded in the audit log.`)) return;
    setSaving(true); setError(null); setValidationErrors([]);
    try {
      await deleteDashboard(dashboardDetail.id, dashboardDetail.concurrencyStamp);
      const remaining = await listDashboards();
      setDashboards(remaining);
      const next = remaining[0];
      if (next) {
        setSelectedDashboardId(next.id);
        navigate(`/dashboard-builder/${next.id}`, { replace: true });
        setNotice(`“${dashboardName}” archived.`);
      } else {
        handleNewDashboard(true);
        navigate("/dashboard-builder", { replace: true });
        setNotice(`“${dashboardName}” archived. Start a new dashboard when ready.`);
      }
      dispatchDashboardsChanged();
    } catch (caught) { setRequestError(caught); } finally { setSaving(false); }
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
    if (dashboardId === selectedDashboardId) return;
    if (!confirmDiscardChanges()) return;
    if (!dashboardId) {
      handleNewDashboard();
      return;
    }

    setSelectedDashboardId(dashboardId);
    navigate(`/dashboard-builder/${dashboardId}`);
  }

  async function handlePublicationAction(action: "publish" | "unpublish") {
    if (!dashboardDetail) return;
    if (action === "unpublish" && isDirty && !window.confirm("Unpublish the live dashboard and discard the unsaved editor changes?")) return;
    const liveSlug = publishedComparison?.published?.publication.slug;
    if (action === "publish") {
      const message = liveSlug && liveSlug !== (slug || createDashboardSlug(dashboardName))
        ? `Publish these changes? The live URL will change from /dashboards/${liveSlug} and the old link will stop working.`
        : dashboardDetail.publication.status === "published"
          ? "Publish the current draft and replace the live dashboard?"
          : "Publish this dashboard and make the saved draft live?";
      if (!window.confirm(message)) return;
    }
    setSaving(true); setError(null); setValidationErrors([]);
    try {
      let saved: DashboardDetail;
      if (action === "publish") {
        const nextSlug = slug || createDashboardSlug(dashboardName);
        const pending = await updateDashboard(dashboardDetail.id, { ...buildSaveRequest(nextSlug), concurrencyStamp: dashboardDetail.concurrencyStamp });
        saved = await publishDashboard(pending.id, pending.concurrencyStamp);
      } else {
        saved = await unpublishDashboard(dashboardDetail.id, dashboardDetail.concurrencyStamp);
      }
      setSlug(saved.publication.slug ?? (slug || createDashboardSlug(dashboardName)));
      const currentSettings = buildSaveRequest().settings;
      setDashboardDetail(saved); setSavedSignature(JSON.stringify(toDashboardSaveRequest(saved, currentSettings))); setShowInNavigation(saved.publication.showInNavigation); setNotice(saved.publication.status === "published" ? "Draft published. The live dashboard now matches this version." : "Dashboard unpublished. Its last published version remains in revision history.");
      setDashboards(await listDashboards());
      await loadPublishingMetadata(saved.id);
      dispatchDashboardsChanged();
    } catch (caught) { setRequestError(caught); } finally { setSaving(false); }
  }

  async function handleRestoreRevision(revision: DashboardRevisionSummary) {
    if (!dashboardDetail || !window.confirm(`Restore revision ${revision.revisionNumber} as the editable draft? The live dashboard will not change until you publish.`)) return;
    setRestoringRevisionId(revision.id); setError(null); setValidationErrors([]);
    try {
      const restored = await restoreDashboardRevision(dashboardDetail.id, revision.id, dashboardDetail.concurrencyStamp);
      setNotice(`Revision ${revision.revisionNumber} restored as a new draft revision.`);
      await loadDashboard(restored.id);
      setDashboards(await listDashboards());
    } catch (caught) { setRequestError(caught); } finally { setRestoringRevisionId(""); }
  }

  function confirmDiscardChanges() {
    return !isDirty || window.confirm("Discard your unsaved dashboard changes?");
  }

  function handleAudienceChange(audience: DashboardAudience) {
    setDashboardAudience(audience);
    setDashboardVisibility(audience === "private" ? "private" : "workspace");
    if (audience === "private") setDashboardIsDefault(false);
    if (audience !== "restricted") { setViewerUserIds([]); setViewerRoleIds([]); setViewerGroupIds([]); }
  }

  function buildSaveRequest(slugOverride = slug) {
    const normalizedSections = normalizeDashboardSections(sections);
    const normalizedWidgets = assignWidgetsToDashboardSections(widgets, normalizedSections);
    return {
      name: dashboardName,
      description: dashboardDescription || null,
      config: { schemaVersion: 1 as const, sections: normalizedSections, widgets: normalizedWidgets, templateProvenance: dashboardDetail?.config.templateProvenance ?? null, filters: filters.length ? filters : null },
      layout: { schemaVersion: 1 as const, widgets: layoutWidgets },
      settings: normalizeDashboardSettings({ visibility: dashboardVisibility, isDefault: dashboardIsDefault, viewerUserIds, viewerRoleIds, viewerGroupIds }),
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
  const sharingSelectionCount = viewerUserIds.length + viewerRoleIds.length + viewerGroupIds.length;
  const sharingError = dashboardAudience === "restricted" && sharingSelectionCount === 0 ? "Choose at least one person, role, or group." : null;
  const publicationDirty = Boolean(dashboardDetail && (slug !== (dashboardDetail.publication.slug ?? "") || showInNavigation !== dashboardDetail.publication.showInNavigation || menuLabel !== (dashboardDetail.publication.menuLabel ?? "") || menuIcon !== (dashboardDetail.publication.menuIcon ?? "layout-dashboard") || menuOrder !== dashboardDetail.publication.menuOrder || viewPermission !== (dashboardDetail.publication.viewPermission ?? "")));
  const liveSnapshot = publishedComparison?.published ?? null;
  const liveSlug = liveSnapshot?.publication.slug ?? null;
  const hasPublishedChanges = Boolean(liveSnapshot && JSON.stringify(normalizeDashboardRevisionSnapshot(liveSnapshot)) !== draftSignature);
  const previewRequest = buildSaveRequest();
  const previewDashboard: DashboardDetail = {
    id: dashboardDetail?.id ?? "draft-preview",
    name: previewRequest.name,
    description: previewRequest.description,
    config: previewRequest.config,
    layout: previewRequest.layout,
    visibility: previewRequest.settings.visibility,
    isDefault: previewRequest.settings.isDefault,
    publication: { ...previewRequest.publication, status: "draft" },
    publishedAt: null,
    publishedById: null,
    widgetCount: previewRequest.config.widgets.length,
    concurrencyStamp: dashboardDetail?.concurrencyStamp ?? "preview",
    createdAt: dashboardDetail?.createdAt ?? new Date().toISOString(),
    createdById: dashboardDetail?.createdById ?? null,
    updatedAt: dashboardDetail?.updatedAt ?? null,
    updatedById: dashboardDetail?.updatedById ?? null
  };

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
            <Button onClick={() => { if (!confirmDiscardChanges()) return; handleSelectTemplate(""); setTemplateGalleryOpen(true); }} variant="outline">
              <Plus className="size-4" />
              New
            </Button>
            <Button onClick={() => setRecycleBinOpen(true)} variant="outline"><History className="size-4" />Recycle bin</Button>
            <Button disabled={widgets.length === 0} onClick={() => setPreviewOpen(true)} variant="outline">
              <Eye className="size-4" />
              Preview draft
            </Button>
            {dashboardDetail ? <Button disabled={saving} onClick={() => void handleDuplicateDashboard()} variant="outline"><Copy className="size-4" />Duplicate</Button> : null}
            {dashboardDetail ? <Button disabled={saving} onClick={() => void handleArchiveDashboard()} variant="danger"><Trash2 className="size-4" />Archive</Button> : null}
            <Button disabled={saving || widgets.length === 0 || Boolean(sharingError)} onClick={() => void handleSave()}>
              <Save className="size-4" />
              {saving ? "Saving..." : "Save"}
            </Button>
          </div>
        }
      />

      {error ? <Alert title="Dashboards">{error}</Alert> : null}
      {notice ? <div className="rounded-xl border border-success/40 bg-success/10 px-4 py-3 text-sm font-semibold text-success">{notice}</div> : null}
      <div aria-atomic="true" aria-live="polite" className="sr-only" role="status">{canvasAnnouncement}</div>

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
              help="Control who can open the published dashboard. Dashboard managers always retain access."
              label="Audience"
              onChange={(event) => handleAudienceChange(event.target.value as DashboardAudience)}
              options={audienceOptions}
              value={dashboardAudience}
            />
            {dashboardAudience === "restricted" ? (
              <div className="grid gap-3 rounded-xl border border-primary/30 bg-primary/5 p-3">
                <div><p className="text-sm font-bold">Choose viewers</p><p className="text-xs text-muted-foreground">Access is granted when a viewer matches any selected person, role, or group.</p></div>
                <SharingChoiceList label="People" options={sharingOptions.users} selectedIds={viewerUserIds} setSelectedIds={setViewerUserIds} />
                <SharingChoiceList label="Roles" options={sharingOptions.roles} selectedIds={viewerRoleIds} setSelectedIds={setViewerRoleIds} />
                <SharingChoiceList label="Groups" options={sharingOptions.groups} selectedIds={viewerGroupIds} setSelectedIds={setViewerGroupIds} />
                {sharingError || getValidationError("settings.sharing") ? <p className="text-xs font-semibold text-danger">{sharingError ?? getValidationError("settings.sharing")}</p> : <p className="text-xs font-semibold text-success">{sharingSelectionCount} audience {sharingSelectionCount === 1 ? "entry" : "entries"} selected.</p>}
              </div>
            ) : null}
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
              <Badge tone={dashboardAudience === "workspace" ? "info" : "warning"}>{dashboardAudience === "restricted" ? `Specific audience (${sharingSelectionCount})` : getDashboardVisibilityLabel(dashboardVisibility)}</Badge>
              {dashboardIsDefault ? <Badge tone="success">Default</Badge> : null}
              {isDirty ? <Badge tone="warning">Unsaved changes</Badge> : <Badge tone="success">Saved</Badge>}
            </div>
            {dashboardDetail?.config.templateProvenance ? <p className="text-xs font-semibold text-muted-foreground">Created from {dashboardTemplateCatalog.find((item) => item.id === dashboardDetail.config.templateProvenance?.templateId)?.name ?? dashboardDetail.config.templateProvenance.templateId} v{dashboardDetail.config.templateProvenance.templateVersion}. This dashboard is independently editable.</p> : null}
            <div className="grid gap-3 border-t border-border pt-4">
              <div><p className="text-sm font-bold text-foreground">Publishing and navigation</p><p className="text-xs text-muted-foreground">Status: {dashboardDetail?.publication.status ?? "draft"}{dashboardDetail?.publishedAt ? ` · Published ${new Date(dashboardDetail.publishedAt).toLocaleString()}` : ""}</p></div>
              <Input label="URL slug" onChange={(event) => setSlug(event.target.value.toLowerCase())} value={slug} />
              {slugError || getValidationError("publication.slug") ? <p className="text-xs font-semibold text-danger">{slugError ?? getValidationError("publication.slug")}</p> : liveSlug && liveSlug !== slug ? <p className="text-xs font-semibold text-warning">The live URL remains /dashboards/{liveSlug} until you publish. Publishing this slug will make the old link stop working.</p> : null}
              <Checkbox checked={showInNavigation} description="Turn this off for an unlisted dashboard that viewers open only from its direct link." label="Show in navigation" onChange={(event) => setShowInNavigation(event.target.checked)} />
              {showInNavigation ? <><Input label="Menu label" onChange={(event) => setMenuLabel(event.target.value)} value={menuLabel} />{getValidationError("publication.menuLabel") ? <p className="text-xs font-semibold text-danger">{getValidationError("publication.menuLabel")}</p> : !menuLabel.trim() ? <p className="text-xs font-semibold text-muted-foreground">Defaults to the dashboard name.</p> : null}<Select label="Menu icon" onChange={(event) => setMenuIcon(event.target.value)} options={[{label:"Dashboard",value:"layout-dashboard"},{label:"Factory",value:"factory"},{label:"Landmark",value:"landmark"},{label:"Bar chart",value:"chart-column"},{label:"Trend",value:"chart-line"},{label:"Activity",value:"activity"},{label:"Business",value:"briefcase-business"}]} value={menuIcon} />{getValidationError("publication.menuIcon") ? <p className="text-xs font-semibold text-danger">{getValidationError("publication.menuIcon")}</p> : null}<Input label="Menu order" onChange={(event) => setMenuOrder(Number(event.target.value))} type="number" value={menuOrder} /></> : null}
              {viewPermission ? <Alert title="Legacy permission rule">This dashboard also requires the backend permission “{viewPermission}”. Clear it to use only the audience selected above.<div className="mt-3"><Button onClick={() => setViewPermission("")} size="sm" variant="outline">Clear legacy rule</Button></div></Alert> : null}
              <div className="flex flex-wrap gap-2">
                {liveSlug ? <Button onClick={() => window.open(`/dashboards/${liveSlug}`, "_blank")} variant="outline"><ExternalLink className="size-4" />Open live</Button> : null}
                {liveSlug ? <Button onClick={() => void navigator.clipboard.writeText(`${window.location.origin}/dashboards/${liveSlug}`)} variant="outline"><Copy className="size-4" />Copy live link</Button> : null}
                <Button disabled={!dashboardDetail || saving || Boolean(slugError) || Boolean(sharingError)} onClick={() => void handlePublicationAction("publish")}>{dashboardDetail?.publication.status === "published" ? "Publish changes" : "Publish dashboard"}</Button>
                {dashboardDetail?.publication.status === "published" ? <Button disabled={saving} onClick={() => void handlePublicationAction("unpublish")} variant="danger">Unpublish</Button> : null}
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
                  <button aria-describedby="canvas-reorder-instructions" aria-keyshortcuts="Space Enter ArrowUp ArrowDown Escape" aria-label={`Reorder ${section.title} section`} aria-pressed={keyboardGrabbed?.kind === "section" && keyboardGrabbed.id === section.id} className="flex min-h-11 min-w-11 cursor-grab items-center justify-center rounded-md px-2 text-muted-foreground hover:bg-muted focus-visible:ring-4 focus-visible:ring-primary/30 active:cursor-grabbing" draggable onDragEnd={() => { setDraggedSectionId(null); setDropTargetId(null); }} onDragStart={(event) => { event.dataTransfer.setData("application/x-dashboard-section", section.id); setDraggedSectionId(section.id); }} onKeyDown={(event) => handleReorderKeyboard(event, "section", section.id)} type="button"><GripVertical className="size-5" /></button>
                  <Input
                    aria-label={`Section ${index + 1} title`}
                    onChange={(event) => handleRenameSection(section.id, event.target.value)}
                    value={section.title}
                  />
                  <Select aria-label={`${section.title} icon`} onChange={(event) => handleSectionIcon(section.id, event.target.value)} value={section.icon ?? ""}><option value="">No icon</option>{sectionIconOptions.map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}</Select>
                  <div className="flex gap-2">
                    <Button aria-label={`Collapse ${section.title}`} onClick={() => toggleSectionCollapsed(section.id)} size="icon" variant="outline">{collapsedSectionIds.has(section.id) ? <ChevronRight className="size-4" /> : <ChevronDown className="size-4" />}</Button>
                    <Button aria-label={`Duplicate ${section.title}`} disabled={!canDuplicateDashboardSection(sections, widgets, section.id)} onClick={() => handleDuplicateSection(section.id)} size="icon" variant="outline"><Copy className="size-4" /></Button>
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
                <Button className="self-end" disabled={!newSectionTitle.trim() || sections.length >= dashboardCanvasQualityLimits.maxSections} onClick={handleAddSection} variant="outline"><Plus className="size-4" />Add section</Button>
              </div>
            </div>
            {adapters.length > 0 ? <Select label="Widget source" onChange={(event) => setWidgetSourceType(event.target.value as "analytics" | "adapter")} options={[{ label: "Platform analytics", value: "analytics" }, { label: "Installed adapter", value: "adapter" }]} value={widgetSourceType} /> : null}
            {widgetSourceType === "adapter" && adapterWidget ? <><Select label="Module" onChange={(event) => { const adapter = adapters.find((item) => item.id === event.target.value); const next = adapter ? createDashboardAdapterWidget(adapter) : null; if (next) setAdapterWidget(next); }} options={adapters.map((item) => ({ label: item.name, value: item.id }))} value={adapterWidget.adapterId} />{selectedAdapter ? <DashboardAdapterSettingsEditor adapter={selectedAdapter} onChange={setAdapterWidget} value={adapterWidget} /> : null}{!isDashboardAdapterWidgetConfigured(selectedAdapter, adapterWidget) ? <p className="text-xs font-semibold text-danger">Complete all required adapter settings before adding this widget.</p> : null}</> : null}
            {widgetSourceType === "analytics" ? <DashboardAddWidgetWizard canAdd={canAddWidget} dateFieldId={dateFieldId} fields={fieldOptions} forms={forms} groupByFieldId={groupByFieldId} metricFieldId={metricFieldId} metricType={metricType} onAdd={handleAddWidget} onColumnsChange={handleToggleColumn} onDateFieldChange={setDateFieldId} onFormChange={setSelectedFormId} onGroupFieldChange={setGroupByFieldId} onMetricFieldChange={setMetricFieldId} onMetricTypeChange={setMetricType} onReportChange={setSelectedReportId} onSectionChange={setSelectedSectionId} onTitleChange={setWidgetTitle} onTypeChange={setWidgetType} onWidthChange={setWidgetWidth} reports={reports} sections={sections} selectedColumns={selectedColumns} selectedFormId={selectedFormId} selectedReportId={selectedReportId} selectedSectionId={selectedSectionId} title={widgetTitle} type={widgetType} width={widgetWidth} /> : <div className="grid gap-4 sm:grid-cols-2"><Input label="Widget title" onChange={(event) => setWidgetTitle(event.target.value)} value={widgetTitle} /><Select label="Section" onChange={(event) => setSelectedSectionId(event.target.value)} options={sections.map((section) => ({ label: section.title, value: section.id }))} value={selectedSectionId} /><Select label="Width" onChange={(event) => setWidgetWidth(event.target.value as DashboardWidgetWidth)} options={widthOptions} value={widgetWidth} /><Button disabled={!canAddWidget} onClick={() => void handleAddWidget()}><Plus className="size-4" />Add adapter widget</Button></div>}
            <DashboardFilterEditor filters={filters} forms={forms} onChange={setFilters} widgets={widgets} />
          </CardContent>
        </Card>
      </section>

      {dashboardDetail ? <section className="grid gap-4 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <div className="flex items-center gap-2"><GitCompare className="size-5 text-primary" /><CardTitle>Draft versus live</CardTitle></div>
            <CardDescription>Editing and saving changes the draft only. Visitors keep seeing the last published snapshot.</CardDescription>
          </CardHeader>
          <CardContent className="grid gap-3">
            {!liveSnapshot ? <Alert title="No published version">This dashboard is not live yet. Preview the draft, then publish when it is ready.</Alert> : <>
              <ComparisonRow draft={dashboardName} label="Name" live={liveSnapshot.name} />
              <ComparisonRow draft={`${widgets.length}`} label="Widgets" live={`${liveSnapshot.config.widgets.length}`} />
              <ComparisonRow draft={`${sections.length}`} label="Sections" live={`${normalizeDashboardSections(liveSnapshot.config.sections).length}`} />
              <ComparisonRow draft={slug || "—"} label="URL slug" live={liveSnapshot.publication.slug ?? "—"} />
              <ComparisonRow draft={showInNavigation ? (menuLabel.trim() || dashboardName) : "Hidden"} label="Navigation" live={liveSnapshot.publication.showInNavigation ? (liveSnapshot.publication.menuLabel || liveSnapshot.name) : "Hidden"} />
              <div className="flex flex-wrap items-center gap-2 border-t border-border pt-3">
                <Badge tone={hasPublishedChanges ? "warning" : "success"}>{hasPublishedChanges ? "Draft differs from live" : "Draft matches live"}</Badge>
                {publishedComparison?.publishedAt ? <span className="text-xs font-semibold text-muted-foreground">Live since {new Date(publishedComparison.publishedAt).toLocaleString()}</span> : null}
              </div>
            </>}
          </CardContent>
        </Card>
        <Card>
          <CardHeader>
            <div className="flex items-center gap-2"><History className="size-5 text-primary" /><CardTitle>Revision history</CardTitle></div>
            <CardDescription>Every save and publishing action creates a revision. Restoring creates a new editable draft and never silently changes live.</CardDescription>
          </CardHeader>
          <CardContent className="grid max-h-80 gap-2 overflow-auto">
            {revisions.length === 0 ? <EmptyState description="Save this dashboard to create its first revision." title="No revisions yet" /> : revisions.map((revision) => <div className="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-border p-3" key={revision.id}>
              <div><div className="flex items-center gap-2"><p className="text-sm font-bold">Revision {revision.revisionNumber}</p>{revision.isPublished ? <Badge tone="success">Published</Badge> : null}</div><p className="text-xs capitalize text-muted-foreground">{revision.reason} · {new Date(revision.createdAt).toLocaleString()}</p></div>
              <Button disabled={Boolean(restoringRevisionId)} onClick={() => void handleRestoreRevision(revision)} size="sm" variant="outline"><RotateCcw className="size-4" />{restoringRevisionId === revision.id ? "Restoring..." : "Restore draft"}</Button>
            </div>)}
          </CardContent>
        </Card>
      </section> : null}

      <section className={`grid md:grid-cols-12 ${canvasDensity === "compact" ? "gap-2" : "gap-4"}`} style={{ zoom: `${canvasZoom}%` }}>
        <div className="md:col-span-12 flex flex-wrap items-end justify-between gap-3">
          <div><p className="text-lg font-bold text-foreground">Layout canvas</p><p className="mt-1 text-sm text-muted-foreground" id="canvas-reorder-instructions">Drag with a pointer, use the arrow buttons, or focus a reorder handle and press Space. While picked up, use Up/Down to reorder and Left/Right to move widgets between sections; press Space again to release.</p></div>
          <div className="flex flex-wrap items-end gap-2"><Button aria-label="Undo canvas change" className="min-h-11 min-w-11" disabled={!undoStack.length} onClick={handleUndo} size="icon" variant="outline"><Undo2 className="size-4" /></Button><Button aria-label="Redo canvas change" className="min-h-11 min-w-11" disabled={!redoStack.length} onClick={handleRedo} size="icon" variant="outline"><Redo2 className="size-4" /></Button><Select aria-label="Canvas density" onChange={(event) => setCanvasDensity(event.target.value as "comfortable" | "compact")} value={canvasDensity}><option value="comfortable">Comfortable</option><option value="compact">Compact</option></Select><Select aria-label="Canvas zoom" onChange={(event) => setCanvasZoom(Number(event.target.value))} value={canvasZoom}><option value="80">80%</option><option value="90">90%</option><option value="100">100%</option></Select><Button aria-pressed={touchReorderEnabled} onClick={() => setTouchReorderEnabled((current) => !current)} variant="outline"><Move className="size-4" />{touchReorderEnabled ? "Hide reorder controls" : "Touch reorder controls"}</Button><Badge tone="info"><Keyboard className="mr-1 inline size-3" />Keyboard ready</Badge></div>
        </div>
        <div className="md:col-span-12 grid gap-3 rounded-xl border border-border bg-muted/20 p-3"><div className="flex flex-wrap items-center justify-between gap-2"><div className="flex items-center gap-2"><Badge tone={selectedWidgetIds.size ? "info" : undefined}>{selectedWidgetIds.size} selected</Badge><Button onClick={() => setSelectedWidgetIds(new Set(widgets.map((widget) => widget.id)))} size="sm" variant="outline">Select all</Button>{selectedWidgetIds.size ? <Button onClick={() => setSelectedWidgetIds(new Set())} size="sm" variant="ghost"><X className="size-4" />Clear</Button> : null}</div></div>{selectedWidgetIds.size ? <div aria-label="Bulk widget actions" className="grid gap-2 sm:grid-cols-[minmax(10rem,1fr)_auto_minmax(8rem,1fr)_auto_auto]"><Select aria-label="Bulk destination section" onChange={(event) => setBulkSectionId(event.target.value)} options={sections.map((section) => ({ label: section.title, value: section.id }))} value={bulkSectionId} /><Button onClick={handleBulkMove} variant="outline">Move selected</Button><Select aria-label="Bulk widget width" onChange={(event) => setBulkWidth(event.target.value as DashboardWidgetWidth)} options={widthOptions} value={bulkWidth} /><Button onClick={handleBulkResize} variant="outline">Resize selected</Button><Button onClick={handleBulkDelete} variant="danger"><Trash2 className="size-4" />Remove selected</Button></div> : <p className="text-xs text-muted-foreground">Select widget cards to move, resize, or remove them together.</p>}</div>
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
              <div><p className="text-sm font-bold text-foreground">{section.title}</p><p className="text-xs text-muted-foreground">{collapsedSectionIds.has(section.id) ? "Section collapsed in the editor." : "Drop a widget here to move it into this section."}</p></div>
              <div className="flex items-center gap-2"><Button aria-label={`${collapsedSectionIds.has(section.id) ? "Expand" : "Collapse"} ${section.title} canvas section`} onClick={() => toggleSectionCollapsed(section.id)} size="icon" variant="ghost">{collapsedSectionIds.has(section.id) ? <ChevronRight className="size-4" /> : <ChevronDown className="size-4" />}</Button><Badge>{widgets.filter((widget) => widget.sectionId === section.id).length} widgets</Badge></div>
            </div>,
            ...(collapsedSectionIds.has(section.id) ? [] : orderedLayout.filter((layout) => widgets.find((widget) => widget.id === layout.id)?.sectionId === section.id).map((layout) => {
            const widget = widgets.find((candidate) => candidate.id === layout.id);
            const previewState = previewStates[layout.id];

            if (!widget) return null;

            const analyticsWidgetType = widget.chart ? toDashboardAnalyticsWidgetType(widget.chart.widgetType) : null;
            const statusTone = getPreviewStatusTone(previewState);
            const appearance = resolveDashboardChartAppearance(widget.chart?.appearance);
            const accent = getDashboardAccentColor(appearance.cardAccent, appearance.palette);

            return (
              <Card
                className={`${getDashboardWidgetGridClass(layout.width)} min-w-0 transition ${selectedWidgetIds.has(layout.id) ? "ring-2 ring-primary ring-offset-2" : dropTargetId === `widget-${layout.id}` ? "ring-2 ring-primary ring-offset-2" : ""}`}
                key={layout.id}
                onDragOver={(event) => { if (!draggedWidgetId) return; event.preventDefault(); event.stopPropagation(); setDropTargetId(`widget-${layout.id}`); }}
                onDrop={(event) => { event.preventDefault(); event.stopPropagation(); handleDropWidget(section.id, layout.id, event.dataTransfer.getData("application/x-dashboard-widget") || draggedWidgetId); }}
                style={accent ? { borderTopColor: accent, borderTopWidth: 4 } : undefined}
              >
                <CardHeader className={canvasDensity === "compact" ? "p-3" : undefined}>
                  <div className="flex flex-wrap items-start justify-between gap-3">
                    <div className="flex min-w-0 items-start gap-2">
                      <input aria-label={`Select ${widget.title}`} checked={selectedWidgetIds.has(layout.id)} className="mt-1 size-4" onChange={() => toggleWidgetSelection(layout.id)} type="checkbox" />
                      <button aria-describedby="canvas-reorder-instructions" aria-keyshortcuts="Space Enter ArrowUp ArrowDown ArrowLeft ArrowRight Escape" aria-label={`Reorder ${widget.title} widget`} aria-pressed={keyboardGrabbed?.kind === "widget" && keyboardGrabbed.id === layout.id} className="mt-0.5 flex min-h-11 min-w-11 cursor-grab items-center justify-center rounded text-muted-foreground hover:bg-muted focus-visible:ring-4 focus-visible:ring-primary/30 active:cursor-grabbing" draggable onDragEnd={() => { setDraggedWidgetId(null); setDropTargetId(null); }} onDragStart={(event) => { event.dataTransfer.effectAllowed = "move"; event.dataTransfer.setData("application/x-dashboard-widget", layout.id); setDraggedWidgetId(layout.id); }} onKeyDown={(event) => handleReorderKeyboard(event, "widget", layout.id)} type="button"><GripVertical className="size-5" /></button>
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
                      <Button aria-label={`Move ${widget.title} up`} className="min-h-11 min-w-11" onClick={() => handleMoveWidget(layout.id, -1)} size="icon" variant="outline">
                        <ArrowUp className="size-4" />
                      </Button>
                      <Button aria-label={`Move ${widget.title} down`} className="min-h-11 min-w-11" onClick={() => handleMoveWidget(layout.id, 1)} size="icon" variant="outline">
                        <ArrowDown className="size-4" />
                      </Button>
                      <Button aria-label="Duplicate widget" disabled={widgets.length >= dashboardCanvasQualityLimits.maxWidgets} onClick={() => handleDuplicateWidget(layout.id)} size="icon" variant="outline"><Copy className="size-4" /></Button>
                      <Button aria-label={`Edit ${widget.title} properties`} onClick={() => setEditingWidgetId(layout.id)} size="icon" variant="outline"><Pencil className="size-4" /></Button>
                      <Button aria-label="Remove widget" onClick={() => handleRemoveWidget(layout.id)} size="icon" variant="outline">
                        <Trash2 className="size-4" />
                      </Button>
                    </div>
                  </div>
                </CardHeader>
                <CardContent className={`grid min-w-0 ${canvasDensity === "compact" ? "gap-2 p-3 pt-0" : "gap-4"}`}>
                  {touchReorderEnabled ? <div aria-label={`Touch reorder ${widget.title}`} className="grid grid-cols-2 gap-2 rounded-xl border border-border bg-muted/20 p-2 sm:grid-cols-4"><Button className="min-h-11" disabled={moveDashboardWidgetWithinSection(layoutWidgets, widgets, widget.id, -1) === layoutWidgets} onClick={() => handleMoveWidget(widget.id, -1)} size="sm" variant="outline"><ArrowUp className="size-4" />Up</Button><Button className="min-h-11" disabled={moveDashboardWidgetWithinSection(layoutWidgets, widgets, widget.id, 1) === layoutWidgets} onClick={() => handleMoveWidget(widget.id, 1)} size="sm" variant="outline"><ArrowDown className="size-4" />Down</Button><Button className="min-h-11" disabled={!getAdjacentDashboardSectionId(sections, widget.sectionId, -1)} onClick={() => handleMoveWidgetToAdjacentSection(widget.id, -1)} size="sm" variant="outline"><ArrowLeft className="size-4" />Previous section</Button><Button className="min-h-11" disabled={!getAdjacentDashboardSectionId(sections, widget.sectionId, 1)} onClick={() => handleMoveWidgetToAdjacentSection(widget.id, 1)} size="sm" variant="outline"><ArrowRight className="size-4" />Next section</Button></div> : null}
                  <Select
                    label="Section"
                    onChange={(event) => handleWidgetSectionChange(widget.id, event.target.value)}
                    options={sections.map((section) => ({ label: section.title, value: section.id }))}
                    value={widget.sectionId ?? sections[0].id}
                  />
                  <Select label="Width" onChange={(event) => handleWidgetWidthChange(layout.id, event.target.value as DashboardWidgetWidth)} options={widthOptions} value={layout.width} />
                  {widget.adapter && getDashboardAdapter(widget.adapter.adapterId) ? (() => { const Renderer = getDashboardAdapter(widget.adapter!.adapterId)!.render; return <Renderer widget={widget} />; })() : <DashboardWidgetPreviewStateView appearance={widget.chart?.appearance} state={previewState} onRefresh={() => void refreshWidgetPreview(widget)} />}
                </CardContent>
              </Card>
            );
          }))])
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
      {recycleBinOpen ? <Suspense fallback={null}>
        <DashboardRecycleBinModal
          onClose={() => setRecycleBinOpen(false)}
          onRestored={(restored) => { setRecycleBinOpen(false); setSelectedDashboardId(restored.id); navigate(`/dashboard-builder/${restored.id}`); setNotice(`“${restored.name}” restored as a draft.`); void listDashboards().then(setDashboards).catch(setRequestError); dispatchDashboardsChanged(); }}
          open={recycleBinOpen}
        />
      </Suspense> : null}
      {previewOpen ? <div className="fixed inset-0 z-50 overflow-auto bg-background">
        <div className="sticky top-0 z-10 flex flex-wrap items-center justify-between gap-3 border-b border-border bg-background/95 px-4 py-3 backdrop-blur sm:px-8">
          <div><div className="flex items-center gap-2"><Badge tone="warning">Draft preview</Badge>{isDirty ? <Badge tone="warning">Unsaved</Badge> : <Badge tone="success">Saved draft</Badge>}</div><p className="mt-1 text-xs text-muted-foreground">This is not the live dashboard. Close preview to continue editing.</p></div>
          <Button onClick={() => setPreviewOpen(false)} variant="outline"><X className="size-4" />Close preview</Button>
        </div>
        <main className="p-4 sm:p-8"><SavedDashboardViewer dashboard={previewDashboard} preview /></main>
      </div> : null}
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

function toDashboardSaveRequest(dashboard: DashboardDetail, sharing?: DashboardSettings) {
  const sections = normalizeDashboardSections(dashboard.config.sections);
  const widgets = assignWidgetsToDashboardSections(dashboard.config.widgets, sections);
  const showInNavigation = dashboard.publication.showInNavigation;
  return {
    name: dashboard.name,
    description: dashboard.description ?? null,
    config: {
      schemaVersion: 1 as const,
      sections,
      widgets,
      templateProvenance: dashboard.config.templateProvenance ?? null,
      filters: dashboard.config.filters?.length ? dashboard.config.filters : null
    },
    layout: { schemaVersion: 1 as const, widgets: dashboard.layout.widgets },
    settings: normalizeDashboardSettings(sharing ?? { visibility: dashboard.visibility, isDefault: dashboard.isDefault }),
    publication: {
      status: dashboard.publication.status,
      slug: dashboard.publication.slug ?? null,
      showInNavigation,
      menuLabel: showInNavigation ? (dashboard.publication.menuLabel?.trim() || dashboard.name.trim()) : null,
      menuIcon: dashboard.publication.menuIcon ?? "layout-dashboard",
      menuOrder: dashboard.publication.menuOrder,
      viewPermission: dashboard.publication.viewPermission ?? null
    }
  };
}

function SharingChoiceList({ label, options, selectedIds, setSelectedIds }: {
  label: string;
  options: DashboardSharingOption[];
  selectedIds: string[];
  setSelectedIds: Dispatch<SetStateAction<string[]>>;
}) {
  const [query, setQuery] = useState("");
  const normalizedQuery = query.trim().toLowerCase();
  const filtered = normalizedQuery ? options.filter((option) => `${option.label} ${option.description ?? ""}`.toLowerCase().includes(normalizedQuery)) : options;
  return <fieldset className="grid gap-2"><legend className="text-xs font-bold uppercase tracking-wide text-muted-foreground">{label}</legend>{options.length > 3 ? <Input aria-label={`Search ${label.toLowerCase()}`} onChange={(event) => setQuery(event.target.value)} placeholder={`Search ${label.toLowerCase()}…`} value={query} /> : null}<div className="grid max-h-44 gap-2 overflow-y-auto pr-1">{filtered.length ? filtered.map((option) => <Checkbox checked={selectedIds.includes(option.id)} description={option.description ?? undefined} key={option.id} label={option.label} onChange={(event) => setSelectedIds((current) => event.target.checked ? [...new Set([...current, option.id])] : current.filter((id) => id !== option.id))} />) : <p className="rounded-lg border border-dashed border-border p-3 text-xs text-muted-foreground">{options.length ? `No ${label.toLowerCase()} match your search.` : `No active ${label.toLowerCase()} are available.`}</p>}</div></fieldset>;
}

function normalizeDashboardRevisionSnapshot(snapshot: DashboardRevisionSnapshot) {
  const sections = normalizeDashboardSections(snapshot.config.sections);
  const widgets = assignWidgetsToDashboardSections(snapshot.config.widgets, sections);
  const showInNavigation = snapshot.publication.showInNavigation;
  return {
    name: snapshot.name,
    description: snapshot.description ?? null,
    config: { schemaVersion: 1 as const, sections, widgets, templateProvenance: snapshot.config.templateProvenance ?? null, filters: snapshot.config.filters?.length ? snapshot.config.filters : null },
    layout: { schemaVersion: 1 as const, widgets: snapshot.layout.widgets },
    settings: normalizeDashboardSettings(snapshot.settings),
    publication: { status: "published", slug: snapshot.publication.slug ?? null, showInNavigation, menuLabel: showInNavigation ? (snapshot.publication.menuLabel?.trim() || snapshot.name.trim()) : null, menuIcon: snapshot.publication.menuIcon ?? "layout-dashboard", menuOrder: snapshot.publication.menuOrder, viewPermission: snapshot.publication.viewPermission ?? null }
  };
}

function ComparisonRow({ draft, label, live }: { draft: string; label: string; live: string }) {
  const changed = draft !== live;
  return <div className="grid gap-2 rounded-xl border border-border p-3 sm:grid-cols-[7rem_1fr_1fr_auto] sm:items-center"><p className="text-xs font-bold uppercase tracking-wide text-muted-foreground">{label}</p><div><p className="text-[0.65rem] font-bold uppercase text-muted-foreground">Draft</p><p className="break-words text-sm font-semibold">{draft}</p></div><div><p className="text-[0.65rem] font-bold uppercase text-muted-foreground">Live</p><p className="break-words text-sm font-semibold">{live}</p></div><Badge tone={changed ? "warning" : "success"}>{changed ? "Changed" : "Same"}</Badge></div>;
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

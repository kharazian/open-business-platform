import type {
  ChartWidgetConfig,
  CreateDashboardRequest,
  DashboardAdapterWidget,
  DashboardTemplateProvenance,
  DashboardWidgetWidth
} from "./types";
import type { DashboardFilterType } from "./types";

export type DashboardTemplateSourceSlot = {
  key: string;
  label: string;
  description?: string;
  kind: "form";
  required: boolean;
  allowReport?: boolean;
};

export type DashboardTemplateSourceBinding = { formId: string; reportId?: string | null };
export type DashboardTemplateBindings = { sources: Record<string, DashboardTemplateSourceBinding> };
export type DashboardTemplateSection = { key: string; title: string; icon?: string };

export type DashboardTemplateAnalyticsWidget = {
  kind: "analytics";
  sourceSlot: string;
  chart: Omit<ChartWidgetConfig, "reportId">;
};

export type DashboardTemplateAdapterWidget = { kind: "adapter"; adapter: DashboardAdapterWidget };

export type DashboardTemplateWidget = {
  key: string;
  title: string;
  subtitle?: string;
  sectionKey: string;
  width: DashboardWidgetWidth;
  source: DashboardTemplateAnalyticsWidget | DashboardTemplateAdapterWidget;
};

export type DashboardTemplateDefinition = {
  id: string;
  version: number;
  name: string;
  description: string;
  category: string;
  tags: string[];
  sourceSlots: DashboardTemplateSourceSlot[];
  sections: DashboardTemplateSection[];
  widgets: DashboardTemplateWidget[];
  requiredAdapterIds?: string[];
  filters?: Array<{ key: string; label: string; type: DashboardFilterType; sourceSlot: string; fieldId: string; options?: string[]; applyToWidgetKeys?: string[] | null }>;
};

export type DashboardTemplateError = { path: string; code: string; message: string };
export type DashboardTemplateInstantiationResult =
  | { ok: true; dashboard: CreateDashboardRequest }
  | { ok: false; errors: DashboardTemplateError[] };

export type DashboardTemplateInstantiationOptions = {
  idGenerator?: () => string;
  now?: () => string;
  name?: string;
  availableAdapterIds?: ReadonlySet<string>;
};

const uuidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export function validateDashboardTemplate(template: DashboardTemplateDefinition): DashboardTemplateError[] {
  const errors: DashboardTemplateError[] = [];
  requireText(template.id, "id", errors);
  requireText(template.name, "name", errors);
  requireText(template.description, "description", errors);
  requireText(template.category, "category", errors);
  if (!Number.isInteger(template.version) || template.version < 1) add(errors, "version", "template.version.invalid", "Template version must be a positive integer.");
  validateUnique(template.sourceSlots.map((slot) => slot.key), "sourceSlots", "source slot", errors);
  validateUnique(template.sections.map((section) => section.key), "sections", "section", errors);
  validateUnique(template.widgets.map((widget) => widget.key), "widgets", "widget", errors);

  const sections = new Set(template.sections.map((section) => section.key));
  const slots = new Set(template.sourceSlots.map((slot) => slot.key));
  validateUnique((template.filters ?? []).map((filter) => filter.key), "filters", "filter", errors);
  template.widgets.forEach((widget, index) => {
    if (!sections.has(widget.sectionKey)) add(errors, `widgets[${index}].sectionKey`, "template.widget.section_missing", `Section '${widget.sectionKey}' does not exist.`);
    const rawSource = widget.source as { chart?: unknown; adapter?: unknown };
    if ((widget.source.kind === "analytics" && rawSource.adapter) || (widget.source.kind === "adapter" && rawSource.chart)) {
      add(errors, `widgets[${index}].source`, "template.widget.source_ambiguous", "A template widget cannot mix analytics and adapter sources.");
    }
    if (widget.source.kind === "analytics" && !slots.has(widget.source.sourceSlot)) add(errors, `widgets[${index}].sourceSlot`, "template.widget.source_missing", `Source slot '${widget.source.sourceSlot}' does not exist.`);
  });
  (template.filters ?? []).forEach((filter, index) => {
    if (!slots.has(filter.sourceSlot)) add(errors, `filters[${index}].sourceSlot`, "template.filter.source_missing", `Source slot '${filter.sourceSlot}' does not exist.`);
    for (const key of filter.applyToWidgetKeys ?? []) if (!template.widgets.some((widget) => widget.key === key)) add(errors, `filters[${index}].applyToWidgetKeys`, "template.filter.widget_missing", `Widget '${key}' does not exist.`);
  });
  return errors;
}

export function instantiateDashboardTemplate(
  template: DashboardTemplateDefinition,
  bindings: DashboardTemplateBindings,
  options: DashboardTemplateInstantiationOptions = {}
): DashboardTemplateInstantiationResult {
  const errors = validateDashboardTemplate(template);
  const slots = new Map(template.sourceSlots.map((slot) => [slot.key, slot]));

  for (const adapterId of template.requiredAdapterIds ?? []) {
    if (options.availableAdapterIds && !options.availableAdapterIds.has(adapterId)) {
      add(errors, "requiredAdapterIds", "template.adapter.unavailable", `Required adapter '${adapterId}' is not installed.`);
    }
  }

  for (const key of Object.keys(bindings.sources)) {
    if (!slots.has(key)) add(errors, `bindings.sources.${key}`, "template.binding.unknown", `Source binding '${key}' is not defined by this template.`);
  }
  for (const slot of template.sourceSlots) {
    const binding = bindings.sources[slot.key];
    if (slot.required && !binding) add(errors, `bindings.sources.${slot.key}`, "template.binding.required", `${slot.label} is required.`);
    if (!binding) continue;
    if (!uuidPattern.test(binding.formId)) add(errors, `bindings.sources.${slot.key}.formId`, "template.binding.form_id_invalid", "Choose a valid source form.");
    if (binding.reportId && !uuidPattern.test(binding.reportId)) add(errors, `bindings.sources.${slot.key}.reportId`, "template.binding.report_id_invalid", "Choose a valid saved report.");
    if (binding.reportId && !slot.allowReport) add(errors, `bindings.sources.${slot.key}.reportId`, "template.binding.report_not_allowed", "This source slot does not allow a saved report.");
  }
  if (errors.length > 0) return { ok: false, errors };

  const makeId = options.idGenerator ?? (() => crypto.randomUUID());
  const sectionIds = new Map(template.sections.map((section) => [section.key, `section-${makeId()}`]));
  const widgetIds = new Map(template.widgets.map((widget) => [widget.key, `widget-${makeId()}`]));
  const provenance: DashboardTemplateProvenance = {
    templateId: template.id,
    templateVersion: template.version,
    instantiatedAt: (options.now ?? (() => new Date().toISOString()))()
  };

  return {
    ok: true,
    dashboard: {
      name: options.name?.trim() || template.name,
      description: template.description,
      config: {
        schemaVersion: 1,
        templateProvenance: provenance,
        filters: (template.filters ?? []).map((filter) => ({
          id: `filter-${makeId()}`,
          label: filter.label,
          type: filter.type,
          sourceFormId: bindings.sources[filter.sourceSlot]!.formId,
          fieldId: filter.fieldId,
          options: [...(filter.options ?? [])],
          applyToWidgetIds: filter.applyToWidgetKeys?.map((key) => widgetIds.get(key)!) ?? null
        })),
        sections: template.sections.map((section, order) => ({ id: sectionIds.get(section.key)!, title: section.title, order, icon: section.icon ?? null })),
        widgets: template.widgets.map((widget) => {
          const analyticsBinding = widget.source.kind === "analytics" ? bindings.sources[widget.source.sourceSlot] : null;
          return {
            id: widgetIds.get(widget.key)!,
            title: widget.title,
            subtitle: widget.subtitle ?? null,
            sectionId: sectionIds.get(widget.sectionKey)!,
            sourceFormId: analyticsBinding?.formId ?? null,
            chart: widget.source.kind === "analytics" ? { ...cloneChart(widget.source.chart), reportId: analyticsBinding?.reportId ?? null } : null,
            adapter: widget.source.kind === "adapter" ? cloneAdapter(widget.source.adapter) : null
          };
        })
      },
      layout: {
        schemaVersion: 1,
        widgets: template.widgets.map((widget, order) => ({ id: widgetIds.get(widget.key)!, width: widget.width, order }))
      },
      settings: { visibility: "workspace", isDefault: false },
      publication: { status: "draft", slug: null, showInNavigation: false, menuLabel: null, menuIcon: "layout-dashboard", menuOrder: 0, viewPermission: null }
    }
  };
}

function cloneChart(chart: Omit<ChartWidgetConfig, "reportId">): Omit<ChartWidgetConfig, "reportId"> {
  return { ...chart, metric: { ...chart.metric }, columns: [...(chart.columns ?? [])], series: chart.series?.map((series) => ({ ...series, metric: { ...series.metric } })) ?? null, appearance: chart.appearance ? { ...chart.appearance } : null };
}

function cloneAdapter(adapter: DashboardAdapterWidget): DashboardAdapterWidget {
  return { ...adapter, settings: { ...adapter.settings } };
}

function validateUnique(values: string[], path: string, label: string, errors: DashboardTemplateError[]) {
  const seen = new Set<string>();
  values.forEach((value, index) => {
    requireText(value, `${path}[${index}].key`, errors);
    if (seen.has(value)) add(errors, path, `template.${path}.duplicate`, `Duplicate ${label} key '${value}'.`);
    seen.add(value);
  });
}

function requireText(value: string, path: string, errors: DashboardTemplateError[]) {
  if (!value?.trim()) add(errors, path, "template.value.required", "A value is required.");
}

function add(errors: DashboardTemplateError[], path: string, code: string, message: string) {
  errors.push({ path, code, message });
}

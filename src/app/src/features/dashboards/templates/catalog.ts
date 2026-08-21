import type { DashboardTemplateDefinition, DashboardTemplateError } from "../templateEngine";
import { businessPerformanceSampleTemplate } from "./businessPerformanceSample";

export function createDashboardTemplateCatalog(templates: DashboardTemplateDefinition[]): DashboardTemplateDefinition[] {
  const ids = new Set<string>();
  for (const template of templates) {
    if (ids.has(template.id)) throw new Error(`Dashboard template id '${template.id}' is already registered.`);
    ids.add(template.id);
  }
  return [...templates].sort((left, right) => left.category.localeCompare(right.category) || left.name.localeCompare(right.name) || left.id.localeCompare(right.id));
}

export const dashboardTemplateCatalog = createDashboardTemplateCatalog([businessPerformanceSampleTemplate]);

export function validateTemplateFieldCapabilities(template: DashboardTemplateDefinition, fieldIds: ReadonlySet<string>): DashboardTemplateError[] {
  const errors: DashboardTemplateError[] = [];
  for (const widget of template.widgets) {
    if (widget.source.kind !== "analytics") continue;
    const requiredIds = [widget.source.chart.metric.fieldId, widget.source.chart.groupByFieldId, widget.source.chart.dateFieldId, ...(widget.source.chart.columns ?? [])];
    for (const fieldId of requiredIds) {
      if (fieldId && !fieldIds.has(fieldId)) errors.push({ path: `widgets.${widget.key}`, code: "template.field.missing", message: `${widget.title} requires the reportable field '${fieldId}'.` });
    }
  }
  for (const filter of template.filters ?? []) {
    if (!fieldIds.has(filter.fieldId)) errors.push({ path: `filters.${filter.key}`, code: "template.filter.field_missing", message: `${filter.label} requires the reportable field '${filter.fieldId}'.` });
  }
  return errors;
}

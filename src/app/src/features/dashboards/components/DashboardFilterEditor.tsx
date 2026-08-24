import { useEffect, useMemo, useState } from "react";
import { ArrowDown, ArrowUp, Filter, Plus, Trash2 } from "lucide-react";
import { Alert } from "../../../components/ui/Alert";
import { Badge } from "../../../components/ui/Badge";
import { Button } from "../../../components/ui/Button";
import { Checkbox } from "../../../components/ui/Checkbox";
import { Input } from "../../../components/ui/Input";
import { Select } from "../../../components/ui/Select";
import { getForm } from "../../forms/api";
import type { FormSummary } from "../../forms/drafts";
import { getReportableFields, type ReportableField } from "../../forms/reportableFields";
import {
  createDashboardFilter,
  dashboardFilterLimit,
  getCompatibleDashboardFilterTypes,
  getCompatibleFilterWidgets,
  moveDashboardFilter,
  updateDashboardFilterField
} from "../filterAuthoring";
import type { DashboardAnalyticsFilterValue, DashboardFilterDefinition, DashboardFilterType, SavedDashboardWidget } from "../types";

type Props = {
  filters: DashboardFilterDefinition[];
  forms: FormSummary[];
  widgets: SavedDashboardWidget[];
  onChange: (filters: DashboardFilterDefinition[]) => void;
};

export function DashboardFilterEditor({ filters, forms, widgets, onChange }: Props) {
  const [fieldsByForm, setFieldsByForm] = useState<Record<string, ReportableField[] | undefined>>({});
  const [loadError, setLoadError] = useState<string | null>(null);
  const eligibleFormIds = useMemo(() => new Set(widgets.filter((widget) => widget.chart && widget.sourceFormId).map((widget) => widget.sourceFormId!)), [widgets]);
  const eligibleForms = forms.filter((form) => eligibleFormIds.has(form.id));

  useEffect(() => {
    const ids = [...new Set([...eligibleForms.map((form) => form.id), ...filters.map((filter) => filter.sourceFormId)])].filter((id) => !fieldsByForm[id]);
    if (!ids.length) return;
    let active = true;
    Promise.all(ids.map(async (id) => [id, getReportableFields((await getForm(id)).draftSchema)] as const))
      .then((items) => { if (active) setFieldsByForm((current) => ({ ...current, ...Object.fromEntries(items) })); })
      .catch((error: unknown) => { if (active) setLoadError(error instanceof Error ? error.message : "Filter fields could not be loaded."); });
    return () => { active = false; };
  }, [eligibleForms.map((form) => form.id).join("|"), filters.map((filter) => filter.sourceFormId).join("|"), fieldsByForm]);

  function patchFilter(id: string, patch: Partial<DashboardFilterDefinition>) {
    onChange(filters.map((filter) => filter.id === id ? { ...filter, ...patch } : filter));
  }

  function addFilter() {
    const form = eligibleForms[0];
    const field = form ? chooseInitialField(fieldsByForm[form.id] ?? []) : undefined;
    if (!form || !field || filters.length >= dashboardFilterLimit) return;
    onChange([...filters, createDashboardFilter(form.id, field, filters)]);
  }

  function changeSource(filter: DashboardFilterDefinition, sourceFormId: string) {
    const field = chooseInitialField(fieldsByForm[sourceFormId] ?? []);
    if (!field) return;
    onChange(filters.map((item) => item.id === filter.id ? { ...updateDashboardFilterField({ ...item, sourceFormId }, field), applyToWidgetIds: null } : item));
  }

  return (
    <section className="grid gap-4 rounded-xl border border-border bg-muted/20 p-4" aria-label="Dashboard filter editor">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div><div className="flex items-center gap-2"><Filter className="size-4 text-primary" /><p className="text-sm font-bold text-foreground">Dashboard filters</p><Badge>{filters.length}/{dashboardFilterLimit}</Badge></div><p className="mt-1 text-xs text-muted-foreground">Configure viewer controls, defaults, required values, and exactly which compatible widgets they affect.</p></div>
        <Button disabled={filters.length >= dashboardFilterLimit || eligibleForms.length === 0 || !fieldsByForm[eligibleForms[0]?.id]} onClick={addFilter} size="sm" variant="outline"><Plus className="size-4" />Add filter</Button>
      </div>
      {loadError ? <Alert title="Filter fields unavailable">{loadError}</Alert> : null}
      {eligibleForms.length === 0 ? <Alert title="Add an analytics widget first">Filters need at least one analytics widget with a form source.</Alert> : null}
      {filters.length === 0 ? <p className="rounded-xl border border-dashed border-border p-4 text-sm text-muted-foreground">No filters yet. Add one to give viewers interactive dashboard controls.</p> : null}
      <div className="grid gap-3">
        {filters.map((filter, index) => {
          const fields = (fieldsByForm[filter.sourceFormId] ?? []).filter((field) => field.filterable);
          const field = fields.find((item) => item.id === filter.fieldId);
          const compatibleTypes = field ? getCompatibleDashboardFilterTypes(field) : [filter.type];
          const compatibleWidgets = getCompatibleFilterWidgets(filter, widgets);
          const allTargets = filter.applyToWidgetIds == null;
          return <article className="grid gap-4 rounded-xl border border-border bg-card p-4" key={filter.id}>
            <div className="flex flex-wrap items-center justify-between gap-2"><div className="flex items-center gap-2"><Badge tone="info">{index + 1}</Badge><p className="font-bold text-foreground">{filter.label || "Untitled filter"}</p>{filter.required ? <Badge tone="warning">Required</Badge> : null}</div><div className="flex gap-1"><Button aria-label={`Move ${filter.label} up`} disabled={index === 0} onClick={() => onChange(moveDashboardFilter(filters, filter.id, -1))} size="icon" variant="ghost"><ArrowUp className="size-4" /></Button><Button aria-label={`Move ${filter.label} down`} disabled={index === filters.length - 1} onClick={() => onChange(moveDashboardFilter(filters, filter.id, 1))} size="icon" variant="ghost"><ArrowDown className="size-4" /></Button><Button aria-label={`Remove ${filter.label}`} onClick={() => onChange(filters.filter((item) => item.id !== filter.id))} size="icon" variant="ghost"><Trash2 className="size-4" /></Button></div></div>
            <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
              <Input label="Filter label" maxLength={100} onChange={(event) => patchFilter(filter.id, { label: event.target.value })} value={filter.label} />
              <Select label="Source form" onChange={(event) => changeSource(filter, event.target.value)} options={eligibleForms.map((form) => ({ label: form.name, value: form.id }))} value={filter.sourceFormId} />
              <Select label="Field" onChange={(event) => { const nextField = fields.find((item) => item.id === event.target.value); if (nextField) onChange(filters.map((item) => item.id === filter.id ? updateDashboardFilterField(item, nextField) : item)); }} options={fields.map((item) => ({ label: item.label, value: item.id }))} value={filter.fieldId} />
              <Select label="Control" onChange={(event) => patchFilter(filter.id, { type: event.target.value as DashboardFilterType, defaultValue: null })} options={compatibleTypes.map((type) => ({ label: filterTypeLabel(type), value: type }))} value={filter.type} />
            </div>
            <Checkbox checked={Boolean(filter.required)} description="Viewers must complete this control before filters can be applied." label="Required filter" onChange={(event) => patchFilter(filter.id, { required: event.target.checked })} />
            {filter.type === "date_range" ? <DateDefaultEditor filter={filter} onChange={(defaultValue) => patchFilter(filter.id, { defaultValue })} /> : <ChoiceEditor filter={filter} onChange={(patch) => patchFilter(filter.id, patch)} />}
            <div className="grid gap-2"><div><p className="text-sm font-bold text-foreground">Widget targeting</p><p className="text-xs text-muted-foreground">Only widgets using {eligibleForms.find((form) => form.id === filter.sourceFormId)?.name ?? "this source"} are available.</p></div><label className="flex items-center gap-2 text-sm font-semibold"><input checked={allTargets} onChange={(event) => patchFilter(filter.id, { applyToWidgetIds: event.target.checked ? null : [] })} type="checkbox" />All compatible widgets</label>{!allTargets ? <div className="grid gap-2 sm:grid-cols-2">{compatibleWidgets.map((widget) => <label className="flex items-center gap-2 rounded-lg border border-border px-3 py-2 text-sm" key={widget.id}><input checked={(filter.applyToWidgetIds ?? []).includes(widget.id)} onChange={(event) => { const targets = filter.applyToWidgetIds ?? []; patchFilter(filter.id, { applyToWidgetIds: event.target.checked ? [...targets, widget.id] : targets.filter((id) => id !== widget.id) }); }} type="checkbox" />{widget.title}</label>)}</div> : <p className="text-xs font-semibold text-success">Targets all {compatibleWidgets.length} compatible widget{compatibleWidgets.length === 1 ? "" : "s"}.</p>}</div>
          </article>;
        })}
      </div>
    </section>
  );
}

function ChoiceEditor({ filter, onChange }: { filter: DashboardFilterDefinition; onChange: (patch: Partial<DashboardFilterDefinition>) => void }) {
  const options = filter.options ?? [];
  const defaults = filter.defaultValue?.values ?? [];
  return <div className="grid gap-3"><label className="block"><span className="mb-2 block text-sm font-bold text-foreground">Options</span><textarea className="min-h-20 w-full rounded-xl border border-border bg-card px-3 py-2 text-sm text-foreground outline-none focus:ring-4 focus:ring-primary/20" maxLength={2019} onChange={(event) => { const next = [...new Set(event.target.value.split(/[\n,]/).map((value) => value.trim()).filter(Boolean))].slice(0, 20); onChange({ options: next, defaultValue: filter.defaultValue?.values?.every((value) => next.includes(value)) ? filter.defaultValue : null }); }} placeholder="One option per line" value={options.join("\n")} /></label><div className="flex flex-wrap gap-2">{options.slice(0, 20).map((option) => <Badge key={option}>{option}</Badge>)}{options.length === 0 ? <span className="text-xs text-muted-foreground">No schema options are available. Add bounded options above.</span> : null}</div><Select className={filter.type === "multi_select" ? "h-24 py-2" : undefined} label="Default value" multiple={filter.type === "multi_select"} onChange={(event) => { const values = filter.type === "multi_select" ? Array.from(event.target.selectedOptions, (option) => option.value) : [event.target.value].filter(Boolean); onChange({ defaultValue: values.length ? { fieldId: filter.fieldId, values } : null }); }} value={filter.type === "multi_select" ? defaults : defaults[0] ?? ""}>{filter.type !== "multi_select" ? <option value="">No default</option> : null}{options.map((option) => <option key={option} value={option}>{option}</option>)}</Select></div>;
}

function DateDefaultEditor({ filter, onChange }: { filter: DashboardFilterDefinition; onChange: (value: DashboardAnalyticsFilterValue | null) => void }) {
  const current = filter.defaultValue;
  const update = (part: "start" | "end", value: string) => { const next = { fieldId: filter.fieldId, start: current?.start ?? null, end: current?.end ?? null, [part]: value || null }; onChange(next.start || next.end ? next : null); };
  return <div className="grid gap-3 sm:grid-cols-2"><Input label="Default start" onChange={(event) => update("start", event.target.value)} type="date" value={current?.start ?? ""} /><Input help="The end date is exclusive." label="Default end" onChange={(event) => update("end", event.target.value)} type="date" value={current?.end ?? ""} /></div>;
}

function filterTypeLabel(type: DashboardFilterType) {
  return ({ date_range: "Date range", single_select: "Single select", multi_select: "Multi select", record_status: "Record status" } as const)[type];
}

function chooseInitialField(fields: ReportableField[]) {
  const filterable = fields.filter((field) => field.filterable);
  return filterable.find((field) => field.options.length > 0) ?? filterable.find((field) => field.id === "status") ?? filterable[0];
}

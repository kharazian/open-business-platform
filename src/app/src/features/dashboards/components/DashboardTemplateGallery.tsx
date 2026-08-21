import { LayoutDashboard, Sparkles } from "lucide-react";
import type { ReactNode } from "react";
import { Badge } from "../../../components/ui/Badge";
import { Button } from "../../../components/ui/Button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "../../../components/ui/Card";
import { Modal } from "../../../components/ui/Modal";
import { Select } from "../../../components/ui/Select";
import type { FormSummary } from "../../forms/drafts";
import type { ListReportSummary } from "../../reports/types";
import type { DashboardTemplateDefinition, DashboardTemplateError, DashboardTemplateSourceBinding } from "../templateEngine";

export function DashboardTemplateGallery({
  open,
  templates,
  selectedTemplateId,
  forms,
  sourceBindings,
  sourceReports,
  capabilityErrors,
  creating,
  onClose,
  onSelectTemplate,
  onSelectSource,
  onSelectReport,
  onStartBlank,
  onCreate
}: {
  open: boolean;
  templates: DashboardTemplateDefinition[];
  selectedTemplateId: string;
  forms: FormSummary[];
  sourceBindings: Record<string, DashboardTemplateSourceBinding | undefined>;
  sourceReports: Record<string, ListReportSummary[] | undefined>;
  capabilityErrors: DashboardTemplateError[];
  creating: boolean;
  onClose: () => void;
  onSelectTemplate: (id: string) => void;
  onSelectSource: (slotKey: string, formId: string) => void;
  onSelectReport: (slotKey: string, reportId: string) => void;
  onStartBlank: () => void;
  onCreate: () => void;
}) {
  const selected = templates.find((template) => template.id === selectedTemplateId) ?? null;
  const missingRequiredBinding = selected?.sourceSlots.some((slot) => slot.required && !sourceBindings[slot.key]?.formId) ?? false;
  return (
    <Modal
      description="Start with an empty editor or generate an independent draft from a reusable template."
      footer={<><Button disabled={creating} onClick={onClose} variant="outline">Cancel</Button>{selected ? <Button disabled={creating || missingRequiredBinding || capabilityErrors.length > 0} onClick={onCreate}>{creating ? "Creating…" : "Create dashboard"}</Button> : <Button disabled={creating} onClick={onStartBlank}>Start blank</Button>}</>}
      onClose={onClose}
      open={open}
      panelClassName="max-w-4xl"
      title="New dashboard"
    >
      <div className="grid max-h-[70vh] gap-5 overflow-y-auto pr-1">
        <div className="grid gap-3 md:grid-cols-2">
          <TemplateCard active={!selected} description="Start with one Overview section and add widgets yourself." icon={<LayoutDashboard className="size-5" />} name="Blank dashboard" onClick={() => onSelectTemplate("")} tags={["Custom"]} />
          {templates.map((template) => <TemplateCard active={selectedTemplateId === template.id} description={template.description} icon={<Sparkles className="size-5" />} key={template.id} name={template.name} onClick={() => onSelectTemplate(template.id)} tags={[template.category, ...template.tags.slice(0, 2)]} />)}
        </div>
        {selected ? (
          <div className="grid gap-4 rounded-xl border border-border bg-muted/20 p-4">
            <div>
              <p className="text-sm font-bold text-foreground">Bind permitted data</p>
              <p className="mt-1 text-xs leading-5 text-muted-foreground">The template stores no form IDs. Choose a form you can access; the API will revalidate form, report, and field permissions when saving and running widgets.</p>
            </div>
            <div className="grid gap-4">{selected.sourceSlots.map((slot) => {
              const binding = sourceBindings[slot.key];
              return <div className="grid gap-3 rounded-lg border border-border bg-card p-3 md:grid-cols-2" key={slot.key}><div className="md:col-span-2"><p className="text-sm font-bold">{slot.label}{slot.required ? " *" : ""}</p>{slot.description ? <p className="mt-1 text-xs text-muted-foreground">{slot.description}</p> : null}</div><Select label="Permitted form" onChange={(event) => onSelectSource(slot.key, event.target.value)} value={binding?.formId ?? ""}><option value="">Choose a form</option>{forms.map((form) => <option key={form.id} value={form.id}>{form.name}</option>)}</Select>{slot.allowReport ? <Select disabled={!binding?.formId} label="Saved report filter (optional)" onChange={(event) => onSelectReport(slot.key, event.target.value)} value={binding?.reportId ?? ""}><option value="">All permitted form records</option>{(sourceReports[slot.key] ?? []).map((report) => <option key={report.id} value={report.id}>{report.name}</option>)}</Select> : null}</div>;
            })}</div>
            {capabilityErrors.length > 0 ? <div className="rounded-lg border border-warning/40 bg-warning/10 p-3"><p className="text-sm font-bold text-warning">This form cannot create every sample widget</p><ul className="mt-2 list-disc space-y-1 pl-5 text-xs text-muted-foreground">{capabilityErrors.slice(0, 8).map((error, index) => <li key={`${error.path}-${index}`}>{error.message}</li>)}</ul></div> : null}
            <div className="grid gap-3 sm:grid-cols-2">
              <div><p className="text-xs font-bold uppercase tracking-wide text-muted-foreground">Sections</p><ol className="mt-2 space-y-1 text-sm text-foreground">{selected.sections.map((section, index) => <li key={section.key}>{index + 1}. {section.title}</li>)}</ol></div>
              <div><p className="text-xs font-bold uppercase tracking-wide text-muted-foreground">Generated content</p><p className="mt-2 text-sm text-foreground">{selected.widgets.length} widgets · {selected.widgets.filter((widget) => widget.source.kind === "analytics").length} analytics · {selected.widgets.filter((widget) => widget.source.kind === "adapter").length} adapter · Draft</p><p className="mt-1 text-xs leading-5 text-muted-foreground">{selected.requiredAdapterIds?.length ? `Requires ${selected.requiredAdapterIds.join(", ")}. ` : ""}Creating does not publish or modify the template.</p></div>
            </div>
          </div>
        ) : null}
      </div>
    </Modal>
  );
}

function TemplateCard({ active, description, icon, name, tags, onClick }: { active: boolean; description: string; icon: ReactNode; name: string; tags: string[]; onClick: () => void }) {
  return <button aria-pressed={active} className="text-left" onClick={onClick} type="button"><Card className={active ? "h-full border-primary ring-2 ring-primary/20" : "h-full hover:border-primary/50"}><CardHeader><div className="flex items-center gap-2 text-primary">{icon}<CardTitle className="text-base">{name}</CardTitle></div><CardDescription>{description}</CardDescription></CardHeader><CardContent className="flex flex-wrap gap-2">{tags.map((tag) => <Badge key={tag}>{tag}</Badge>)}</CardContent></Card></button>;
}

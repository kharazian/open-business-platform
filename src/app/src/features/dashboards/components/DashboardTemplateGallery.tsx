import { LayoutDashboard, Sparkles } from "lucide-react";
import type { ReactNode } from "react";
import { Badge } from "../../../components/ui/Badge";
import { Button } from "../../../components/ui/Button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "../../../components/ui/Card";
import { Modal } from "../../../components/ui/Modal";
import { Select } from "../../../components/ui/Select";
import type { FormSummary } from "../../forms/drafts";
import type { ListReportSummary } from "../../reports/types";
import type { DashboardTemplateDefinition, DashboardTemplateError } from "../templateEngine";

export function DashboardTemplateGallery({
  open,
  templates,
  selectedTemplateId,
  forms,
  selectedFormId,
  reports,
  selectedReportId,
  capabilityErrors,
  creating,
  onClose,
  onSelectTemplate,
  onSelectForm,
  onSelectReport,
  onStartBlank,
  onCreate
}: {
  open: boolean;
  templates: DashboardTemplateDefinition[];
  selectedTemplateId: string;
  forms: FormSummary[];
  selectedFormId: string;
  reports: ListReportSummary[];
  selectedReportId: string;
  capabilityErrors: DashboardTemplateError[];
  creating: boolean;
  onClose: () => void;
  onSelectTemplate: (id: string) => void;
  onSelectForm: (id: string) => void;
  onSelectReport: (id: string) => void;
  onStartBlank: () => void;
  onCreate: () => void;
}) {
  const selected = templates.find((template) => template.id === selectedTemplateId) ?? null;
  return (
    <Modal
      description="Start with an empty editor or generate an independent draft from a reusable template."
      footer={<><Button disabled={creating} onClick={onClose} variant="outline">Cancel</Button>{selected ? <Button disabled={creating || !selectedFormId || capabilityErrors.length > 0} onClick={onCreate}>{creating ? "Creating…" : "Create dashboard"}</Button> : <Button disabled={creating} onClick={onStartBlank}>Start blank</Button>}</>}
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
            <div className="grid gap-4 md:grid-cols-2">
              <Select label={selected.sourceSlots[0]?.label ?? "Source form"} onChange={(event) => onSelectForm(event.target.value)} value={selectedFormId}>
                <option value="">Choose a form</option>
                {forms.map((form) => <option key={form.id} value={form.id}>{form.name}</option>)}
              </Select>
              <Select disabled={!selectedFormId} label="Saved report filter (optional)" onChange={(event) => onSelectReport(event.target.value)} value={selectedReportId}>
                <option value="">All permitted form records</option>
                {reports.map((report) => <option key={report.id} value={report.id}>{report.name}</option>)}
              </Select>
            </div>
            {capabilityErrors.length > 0 ? <div className="rounded-lg border border-warning/40 bg-warning/10 p-3"><p className="text-sm font-bold text-warning">This form cannot create every sample widget</p><ul className="mt-2 list-disc space-y-1 pl-5 text-xs text-muted-foreground">{capabilityErrors.slice(0, 8).map((error, index) => <li key={`${error.path}-${index}`}>{error.message}</li>)}</ul></div> : null}
            <div className="grid gap-3 sm:grid-cols-2">
              <div><p className="text-xs font-bold uppercase tracking-wide text-muted-foreground">Sections</p><ol className="mt-2 space-y-1 text-sm text-foreground">{selected.sections.map((section, index) => <li key={section.key}>{index + 1}. {section.title}</li>)}</ol></div>
              <div><p className="text-xs font-bold uppercase tracking-wide text-muted-foreground">Generated content</p><p className="mt-2 text-sm text-foreground">{selected.widgets.length} widgets · {selected.widgets.filter((widget) => widget.source.kind === "analytics").length} analytics · Draft</p><p className="mt-1 text-xs leading-5 text-muted-foreground">Creating does not publish or modify the template. You can edit the saved dashboard independently.</p></div>
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

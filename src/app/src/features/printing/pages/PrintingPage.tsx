import { useEffect, useMemo, useState } from "react";
import { FileText, Loader2, Plus, RefreshCw, Save, Trash2, Upload } from "lucide-react";
import { Alert } from "../../../components/ui/Alert";
import { Badge } from "../../../components/ui/Badge";
import { Button } from "../../../components/ui/Button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "../../../components/ui/Card";
import { Checkbox } from "../../../components/ui/Checkbox";
import { EmptyState } from "../../../components/ui/EmptyState";
import { Input } from "../../../components/ui/Input";
import { PageHeader } from "../../../components/ui/PageHeader";
import { Select } from "../../../components/ui/Select";
import { Textarea } from "../../../components/ui/Textarea";
import { listForms, getForm, type FormDetail } from "../../forms/api";
import type { FormSummary } from "../../forms/drafts";
import { getReportFieldOptions } from "../../reports/builder";
import { listReports } from "../../reports/api";
import type { ListReportSummary } from "../../reports/types";
import {
  createPrintTemplate,
  deletePrintTemplate,
  getPrintTemplate,
  listPrintTemplateVersions,
  listPrintTemplates,
  PrintingApiError,
  publishPrintTemplateVersion,
  updatePrintTemplate
} from "../api";
import {
  buildPrintTemplateRequest,
  createPrintTemplateDraft,
  createPrintTemplateDraftFromDetail,
  createTemplateSection,
  validatePrintTemplateDraft
} from "../templateBuilder";
import type {
  PrintTemplateDraft,
  PrintTemplateDetail,
  PrintTemplateLayoutConfig,
  PrintTemplateSectionConditionConfig,
  PrintTemplateSectionConfig,
  PrintTemplateSectionPaginationConfig,
  PrintTemplateSummary,
  PrintTemplateType,
  PrintTemplateValidationError,
  PrintTemplateVersionSummary
} from "../types";

export function PrintingPage() {
  const [forms, setForms] = useState<FormSummary[]>([]);
  const [selectedFormId, setSelectedFormId] = useState("");
  const [formDetail, setFormDetail] = useState<FormDetail | null>(null);
  const [reports, setReports] = useState<ListReportSummary[]>([]);
  const [templates, setTemplates] = useState<PrintTemplateSummary[]>([]);
  const [versions, setVersions] = useState<PrintTemplateVersionSummary[]>([]);
  const [selectedTemplateId, setSelectedTemplateId] = useState("");
  const [draft, setDraft] = useState<PrintTemplateDraft>(() => createPrintTemplateDraft("record"));
  const [loading, setLoading] = useState(true);
  const [loadingWorkspace, setLoadingWorkspace] = useState(false);
  const [loadingVersions, setLoadingVersions] = useState(false);
  const [saving, setSaving] = useState(false);
  const [publishing, setPublishing] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [validationErrors, setValidationErrors] = useState<PrintTemplateValidationError[]>([]);

  useEffect(() => {
    void loadInitialData();
  }, []);

  useEffect(() => {
    if (!selectedFormId) {
      setFormDetail(null);
      setReports([]);
      setTemplates([]);
      setVersions([]);
      setSelectedTemplateId("");
      setDraft(createPrintTemplateDraft("record"));
      return;
    }

    void loadWorkspace(selectedFormId);
  }, [selectedFormId]);

  const selectedForm = forms.find((form) => form.id === selectedFormId) ?? null;
  const selectedReport = reports.find((report) => report.id === draft.reportId) ?? null;
  const recordFieldOptions = useMemo(
    () => formDetail?.draftSchema?.fields.map((field) => ({ id: field.id, label: field.label })) ?? [],
    [formDetail]
  );
  const reportFieldOptions = useMemo(() => (formDetail?.draftSchema ? getReportFieldOptions(formDetail.draftSchema) : []), [formDetail]);
  const fieldOptions = draft.type === "record" ? recordFieldOptions : reportFieldOptions;
  const selectedTemplate = templates.find((template) => template.id === selectedTemplateId) ?? null;
  const errorMap = useMemo(() => new Map(validationErrors.map((validationError) => [validationError.path, validationError.message])), [validationErrors]);

  async function loadInitialData() {
    setLoading(true);
    setError(null);

    try {
      const formItems = await listForms();
      setForms(formItems);
      setSelectedFormId((current) => current || formItems[0]?.id || "");
    } catch (caught) {
      setError(getErrorMessage(caught));
      setForms([]);
    } finally {
      setLoading(false);
    }
  }

  async function loadWorkspace(formId: string) {
    setLoadingWorkspace(true);
    setError(null);
    setNotice(null);
    setValidationErrors([]);

    try {
      const [form, reportItems, templateItems] = await Promise.all([
        getForm(formId),
        listReports(formId),
        listPrintTemplates(formId)
      ]);
      setFormDetail(form);
      setReports(reportItems);
      setTemplates(templateItems);
      setVersions([]);
      setSelectedTemplateId("");
      setDraft(createPrintTemplateDraft("record", form.name));
    } catch (caught) {
      setError(getErrorMessage(caught));
      setReports([]);
      setTemplates([]);
      setVersions([]);
    } finally {
      setLoadingWorkspace(false);
    }
  }

  async function handleSelectTemplate(templateId: string) {
    setSelectedTemplateId(templateId);
    setError(null);
    setNotice(null);
    setValidationErrors([]);
    setLoadingVersions(true);

    try {
      const [template, versionItems] = await Promise.all([
        getPrintTemplate(templateId),
        listPrintTemplateVersions(templateId)
      ]);
      setDraft(createPrintTemplateDraftFromDetail(template));
      setVersions(versionItems);
    } catch (caught) {
      setError(getErrorMessage(caught));
      setVersions([]);
    } finally {
      setLoadingVersions(false);
    }
  }

  function handleNewTemplate(type: PrintTemplateType = draft.type) {
    setSelectedTemplateId("");
    setDraft(createPrintTemplateDraft(type, selectedReport?.name ?? selectedForm?.name ?? "Document"));
    setVersions([]);
    setValidationErrors([]);
    setNotice("New print template draft started.");
    setError(null);
  }

  function updateDraft(patch: Partial<PrintTemplateDraft>) {
    setDraft((current) => ({ ...current, ...patch }));
    setNotice(null);
  }

  function updateConfig(patch: Partial<PrintTemplateDraft["config"]>) {
    updateDraft({ config: { ...draft.config, ...patch } });
  }

  function updateHeader(patch: Partial<PrintTemplateDraft["config"]["header"]>) {
    updateConfig({ header: { ...draft.config.header, ...patch } });
  }

  function updateLayout(patch: Partial<PrintTemplateLayoutConfig>) {
    updateConfig({ layout: { ...draft.config.layout, ...patch } });
  }

  function updateFooterText(text: string) {
    updateConfig({ footer: { ...draft.config.footer, text } });
  }

  function updateSection(index: number, patch: Partial<PrintTemplateSectionConfig>) {
    const sections = draft.config.sections.map((section, sectionIndex) => sectionIndex === index ? { ...section, ...patch } : section);
    updateConfig({ sections });
  }

  function updateSectionPagination(index: number, patch: Partial<PrintTemplateSectionPaginationConfig>) {
    const currentPagination = draft.config.sections[index]?.pagination ?? { pageBreakBefore: false, avoidBreakInside: true };
    updateSection(index, { pagination: { ...currentPagination, ...patch } });
  }

  function addSectionCondition(sectionIndex: number) {
    const section = draft.config.sections[sectionIndex];
    const fieldId = fieldOptions[0]?.id ?? "";
    const conditions = [...(section.conditions ?? []), { fieldId, operator: "equals" as const, value: "" }];
    updateSection(sectionIndex, { conditions });
  }

  function updateSectionCondition(sectionIndex: number, conditionIndex: number, patch: Partial<PrintTemplateSectionConditionConfig>) {
    const section = draft.config.sections[sectionIndex];
    const conditions = (section.conditions ?? []).map((condition, index) => index === conditionIndex ? { ...condition, ...patch } : condition);
    updateSection(sectionIndex, { conditions });
  }

  function removeSectionCondition(sectionIndex: number, conditionIndex: number) {
    const section = draft.config.sections[sectionIndex];
    updateSection(sectionIndex, { conditions: (section.conditions ?? []).filter((_condition, index) => index !== conditionIndex) });
  }

  function addSection(kind: "fields" | "table" | "signature") {
    const section = createTemplateSection(draft.type, draft.config.sections.length + 1);
    updateConfig({
      sections: [
        ...draft.config.sections,
        {
          ...section,
          kind,
          id: `${kind}_${draft.config.sections.length + 1}`,
          title: kind === "signature" ? "Signatures" : section.title
        }
      ]
    });
  }

  function removeSection(index: number) {
    updateConfig({ sections: draft.config.sections.filter((_section, sectionIndex) => sectionIndex !== index) });
  }

  function setTemplateType(type: PrintTemplateType) {
    const nextDraft = createPrintTemplateDraft(type, selectedForm?.name ?? "Document");
    setDraft({
      ...nextDraft,
      name: draft.name,
      description: draft.description
    });
    setSelectedTemplateId("");
    setValidationErrors([]);
    setNotice(null);
  }

  async function handleSave() {
    if (!selectedFormId) return;

    const validation = validatePrintTemplateDraft(draft);
    setValidationErrors(validation.errors);
    setError(null);
    setNotice(null);

    if (!validation.valid) {
      setError("Fix the highlighted print template fields before saving.");
      return;
    }

    setSaving(true);

    try {
      const request = buildPrintTemplateRequest(draft);
      const saved = draft.id && draft.concurrencyStamp
        ? await updatePrintTemplate(draft.id, { ...request, concurrencyStamp: draft.concurrencyStamp })
        : await createPrintTemplate(selectedFormId, request);
      const [templateItems, versionItems] = await Promise.all([
        listPrintTemplates(saved.formId),
        listPrintTemplateVersions(saved.id)
      ]);
      setDraft(createPrintTemplateDraftFromDetail(saved));
      setSelectedTemplateId(saved.id);
      setTemplates(templateItems);
      setVersions(versionItems);
      setValidationErrors([]);
      setNotice("Print template saved.");
    } catch (caught) {
      setError(getErrorMessage(caught));
      setValidationErrors(getValidationErrors(caught));
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete() {
    if (!draft.id || !selectedFormId) return;

    setDeleting(true);
    setError(null);
    setNotice(null);

    try {
      await deletePrintTemplate(draft.id);
      setTemplates(await listPrintTemplates(selectedFormId));
      setVersions([]);
      handleNewTemplate(draft.type);
      setNotice("Print template deleted.");
    } catch (caught) {
      setError(getErrorMessage(caught));
    } finally {
      setDeleting(false);
    }
  }

  async function handlePublish() {
    if (!selectedFormId) return;

    const validation = validatePrintTemplateDraft(draft);
    setValidationErrors(validation.errors);
    setError(null);
    setNotice(null);

    if (!validation.valid) {
      setError("Fix the highlighted print template fields before publishing.");
      return;
    }

    setPublishing(true);

    try {
      const request = buildPrintTemplateRequest(draft);
      const saved: PrintTemplateDetail = draft.id && draft.concurrencyStamp
        ? await updatePrintTemplate(draft.id, { ...request, concurrencyStamp: draft.concurrencyStamp })
        : await createPrintTemplate(selectedFormId, request);
      const published = await publishPrintTemplateVersion(saved.id, saved.concurrencyStamp);
      const [templateItems, versionItems] = await Promise.all([
        listPrintTemplates(published.formId),
        listPrintTemplateVersions(published.id)
      ]);
      setDraft(createPrintTemplateDraftFromDetail(published));
      setSelectedTemplateId(published.id);
      setTemplates(templateItems);
      setVersions(versionItems);
      setValidationErrors([]);
      setNotice(`Print template version ${published.currentVersionNumber ?? versionItems[0]?.versionNumber ?? 1} published.`);
    } catch (caught) {
      setError(getErrorMessage(caught));
      setValidationErrors(getValidationErrors(caught));
    } finally {
      setPublishing(false);
    }
  }

  return (
    <div className="grid gap-6">
      <PageHeader
        eyebrow="Printing V6"
        title="Print templates"
        description="Create reusable record and report print layouts for browser PDF generation."
        actions={
          <div className="flex flex-wrap gap-2">
            <Button disabled={loading || loadingWorkspace} onClick={() => selectedFormId ? void loadWorkspace(selectedFormId) : void loadInitialData()} variant="outline">
              <RefreshCw className="size-4" />
              Refresh
            </Button>
            <Button disabled={!selectedFormId} onClick={() => handleNewTemplate("record")} variant="outline">
              <Plus className="size-4" />
              Record template
            </Button>
            <Button disabled={!selectedFormId || reports.length === 0} onClick={() => handleNewTemplate("report")} variant="outline">
              <Plus className="size-4" />
              Report template
            </Button>
          </div>
        }
      />

      {error ? <Alert title="Printing">{error}</Alert> : null}
      {notice ? <div className="rounded-xl border border-success/40 bg-success/10 px-4 py-3 text-sm font-semibold text-success">{notice}</div> : null}

      <section className="grid gap-4 xl:grid-cols-[22rem_minmax(0,1fr)]">
        <Card className="self-start">
          <CardHeader>
            <CardTitle>Templates</CardTitle>
            <CardDescription>Choose a form and edit one print layout.</CardDescription>
          </CardHeader>
          <CardContent className="grid gap-4">
            <Select disabled={loading || forms.length === 0} label="Form" onChange={(event) => setSelectedFormId(event.target.value)} value={selectedFormId}>
              {forms.map((form) => (
                <option key={form.id} value={form.id}>
                  {form.name}
                </option>
              ))}
            </Select>
            <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
              <Metric label="Total" value={templates.length} />
              <Metric label="Record" value={templates.filter((template) => template.type === "record").length} />
              <Metric label="Report" value={templates.filter((template) => template.type === "report").length} />
              <Metric label="Published" value={templates.filter((template) => template.currentVersionId).length} />
            </div>
            {loadingWorkspace ? (
              <p className="text-sm font-semibold text-muted-foreground">Loading templates...</p>
            ) : templates.length > 0 ? (
              <div className="grid gap-2">
                {templates.map((template) => (
                  <button
                    className={[
                      "rounded-xl border p-3 text-left transition hover:bg-muted/50",
                      selectedTemplateId === template.id ? "border-primary bg-primary-soft" : "border-border bg-card/70"
                    ].join(" ")}
                    key={template.id}
                    onClick={() => void handleSelectTemplate(template.id)}
                    type="button"
                  >
                    <div className="flex items-start justify-between gap-3">
                      <div className="min-w-0">
                        <p className="truncate font-bold text-foreground">{template.name}</p>
                        <p className="mt-1 text-xs font-semibold text-muted-foreground">
                          {template.sectionCount} sections{template.currentVersionNumber ? `, v${template.currentVersionNumber}` : ""}
                        </p>
                      </div>
                      <Badge>{template.type}</Badge>
                    </div>
                  </button>
                ))}
              </div>
            ) : (
              <EmptyState title="No templates" description="Create the first print template for this form." />
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <div className="flex flex-wrap items-start justify-between gap-3">
              <div>
                <CardTitle>{selectedTemplate ? "Edit print template" : "New print template"}</CardTitle>
                <CardDescription>{selectedForm ? `${selectedForm.name}.` : "Choose a form to start."}</CardDescription>
              </div>
              <div className="flex flex-wrap gap-2">
                {selectedTemplate?.currentVersionNumber ? <Badge>v{selectedTemplate.currentVersionNumber}</Badge> : null}
                <Badge>{draft.type}</Badge>
              </div>
            </div>
          </CardHeader>
          <CardContent className="grid gap-5">
            <div className="grid gap-4 lg:grid-cols-[minmax(0,1fr)_12rem]">
              <Input error={errorMap.get("name")} label="Name" onChange={(event) => updateDraft({ name: event.target.value })} value={draft.name} />
              <Select label="Type" onChange={(event) => setTemplateType(event.target.value as PrintTemplateType)} value={draft.type}>
                <option value="record">Record</option>
                <option value="report">Report</option>
              </Select>
            </div>
            <Textarea label="Description" onChange={(event) => updateDraft({ description: event.target.value })} value={draft.description} />
            {draft.type === "report" ? (
              <Select error={errorMap.get("reportId")} label="Saved report" onChange={(event) => updateDraft({ reportId: event.target.value || null })} value={draft.reportId ?? ""}>
                <option value="">Select report</option>
                {reports.map((report) => (
                  <option key={report.id} value={report.id}>
                    {report.name}
                  </option>
                ))}
              </Select>
            ) : null}

            <section className="grid gap-3 rounded-xl border border-border bg-muted/20 p-4">
              <h3 className="text-sm font-black text-foreground">Header and footer</h3>
              <Input error={errorMap.get("config.header.title")} label="Header title" onChange={(event) => updateHeader({ title: event.target.value })} value={draft.config.header.title} />
              <Input label="Subtitle" onChange={(event) => updateHeader({ subtitle: event.target.value })} value={draft.config.header.subtitle ?? ""} />
              <Input label="Logo URL" onChange={(event) => updateHeader({ logoUrl: event.target.value })} value={draft.config.header.logoUrl ?? ""} />
              <Checkbox checked={draft.config.header.showGeneratedAt} label="Show generated time" onChange={(event) => updateHeader({ showGeneratedAt: event.target.checked })} />
              <Input label="Footer" onChange={(event) => updateFooterText(event.target.value)} value={draft.config.footer.text ?? ""} />
            </section>

            <section className="grid gap-3 rounded-xl border border-border bg-muted/20 p-4">
              <h3 className="text-sm font-black text-foreground">Page setup</h3>
              <div className="grid gap-4 md:grid-cols-3">
                <Select
                  error={errorMap.get("config.layout.pageSize")}
                  label="Page size"
                  onChange={(event) => updateLayout({ pageSize: event.target.value as PrintTemplateLayoutConfig["pageSize"] })}
                  value={draft.config.layout.pageSize}
                >
                  <option value="letter">Letter</option>
                  <option value="a4">A4</option>
                </Select>
                <Select
                  error={errorMap.get("config.layout.orientation")}
                  label="Orientation"
                  onChange={(event) => updateLayout({ orientation: event.target.value as PrintTemplateLayoutConfig["orientation"] })}
                  value={draft.config.layout.orientation}
                >
                  <option value="portrait">Portrait</option>
                  <option value="landscape">Landscape</option>
                </Select>
                <Select
                  error={errorMap.get("config.layout.margin")}
                  label="Margin"
                  onChange={(event) => updateLayout({ margin: event.target.value as PrintTemplateLayoutConfig["margin"] })}
                  value={draft.config.layout.margin}
                >
                  <option value="narrow">Narrow</option>
                  <option value="normal">Normal</option>
                  <option value="wide">Wide</option>
                </Select>
              </div>
              <Checkbox
                checked={draft.config.layout.repeatTableHeaders}
                label="Repeat table headers"
                onChange={(event) => updateLayout({ repeatTableHeaders: event.target.checked })}
              />
            </section>

            <section className="grid gap-3">
              <div className="flex flex-wrap items-center justify-between gap-3">
                <h3 className="text-sm font-black text-foreground">Sections</h3>
                <div className="flex flex-wrap gap-2">
                  <Button disabled={draft.type !== "record"} onClick={() => addSection("fields")} size="sm" variant="outline">Fields</Button>
                  <Button disabled={draft.type !== "report"} onClick={() => addSection("table")} size="sm" variant="outline">Table</Button>
                  <Button onClick={() => addSection("signature")} size="sm" variant="outline">Signature</Button>
                </div>
              </div>
              {draft.config.sections.map((section, index) => (
                <div className="grid gap-3 rounded-xl border border-border bg-card/80 p-4" key={`${section.id}-${index}`}>
                  <div className="flex items-center justify-between gap-3">
                    <Badge>{section.kind}</Badge>
                    <Button aria-label="Remove section" disabled={draft.config.sections.length <= 1} onClick={() => removeSection(index)} size="icon" variant="ghost">
                      <Trash2 className="size-4" />
                    </Button>
                  </div>
                  <Input label="Section ID" onChange={(event) => updateSection(index, { id: event.target.value })} value={section.id} />
                  <Input label="Title" onChange={(event) => updateSection(index, { title: event.target.value })} value={section.title} />
                  <div className="grid gap-2 sm:grid-cols-2">
                    <Checkbox
                      checked={section.pagination?.pageBreakBefore ?? false}
                      label="Start on new page"
                      onChange={(event) => updateSectionPagination(index, { pageBreakBefore: event.target.checked })}
                    />
                    <Checkbox
                      checked={section.pagination?.avoidBreakInside ?? true}
                      label="Keep section together"
                      onChange={(event) => updateSectionPagination(index, { avoidBreakInside: event.target.checked })}
                    />
                  </div>
                  <SectionConditionsEditor
                    conditions={section.conditions ?? []}
                    fieldOptions={fieldOptions}
                    onAdd={() => addSectionCondition(index)}
                    onRemove={(conditionIndex) => removeSectionCondition(index, conditionIndex)}
                    onUpdate={(conditionIndex, patch) => updateSectionCondition(index, conditionIndex, patch)}
                  />
                  {section.kind === "signature" ? (
                    <Textarea
                      className="min-h-20"
                      label="Signature labels"
                      onChange={(event) => updateSection(index, { signatureLabels: splitLines(event.target.value) })}
                      value={(section.signatureLabels ?? []).join("\n")}
                    />
                  ) : (
                    <FieldCheckboxes
                      fieldIds={section.fieldIds}
                      fieldOptions={fieldOptions}
                      onChange={(fieldIds) => updateSection(index, { fieldIds })}
                    />
                  )}
                </div>
              ))}
            </section>

            {validationErrors.length > 0 ? <ValidationErrorsPanel errors={validationErrors} /> : null}

            {draft.id ? (
              <section className="grid gap-3 rounded-xl border border-border bg-muted/20 p-4">
                <div className="flex flex-wrap items-center justify-between gap-3">
                  <h3 className="text-sm font-black text-foreground">Version history</h3>
                  {loadingVersions ? <span className="text-xs font-bold text-muted-foreground">Loading...</span> : null}
                </div>
                {versions.length > 0 ? (
                  <div className="grid gap-2">
                    {versions.map((version) => (
                      <div className="flex flex-wrap items-center justify-between gap-3 rounded-lg border border-border bg-card/80 px-3 py-2" key={version.id}>
                        <div>
                          <p className="text-sm font-bold text-foreground">Version {version.versionNumber}</p>
                          <p className="text-xs font-semibold text-muted-foreground">{formatDateTime(version.publishedAt ?? version.createdAt)}</p>
                        </div>
                        <Badge>{version.sectionCount} sections</Badge>
                      </div>
                    ))}
                  </div>
                ) : (
                  <p className="rounded-lg border border-dashed border-border bg-card/70 px-3 py-2 text-sm font-semibold text-muted-foreground">
                    No published versions yet.
                  </p>
                )}
              </section>
            ) : null}

            <div className="flex flex-wrap gap-2">
              <Button disabled={!selectedFormId || saving || deleting || publishing} onClick={() => void handleSave()}>
                {saving ? <Loader2 className="size-4 animate-spin" /> : <Save className="size-4" />}
                {saving ? "Saving..." : "Save"}
              </Button>
              <Button disabled={!selectedFormId || saving || deleting || publishing} onClick={() => void handlePublish()} variant="outline">
                {publishing ? <Loader2 className="size-4 animate-spin" /> : <Upload className="size-4" />}
                {publishing ? "Publishing..." : "Publish"}
              </Button>
              <Button disabled={!draft.id || saving || deleting || publishing} onClick={() => void handleDelete()} variant="danger">
                {deleting ? <Loader2 className="size-4 animate-spin" /> : <Trash2 className="size-4" />}
                Delete
              </Button>
            </div>
          </CardContent>
        </Card>
      </section>
    </div>
  );
}

function FieldCheckboxes({
  fieldIds,
  fieldOptions,
  onChange
}: {
  fieldIds: string[];
  fieldOptions: Array<{ id: string; label: string }>;
  onChange: (fieldIds: string[]) => void;
}) {
  if (fieldOptions.length === 0) {
    return <div className="rounded-xl border border-dashed border-border bg-muted/40 p-3 text-sm font-semibold text-muted-foreground">No fields available.</div>;
  }

  return (
    <div className="grid gap-2">
      <p className="text-sm font-bold text-foreground">Fields</p>
      <div className="grid gap-2 sm:grid-cols-2">
        {fieldOptions.map((field) => (
          <Checkbox
            checked={fieldIds.includes(field.id)}
            key={field.id}
            label={field.label}
            onChange={(event) => {
              onChange(event.target.checked ? [...fieldIds, field.id] : fieldIds.filter((fieldId) => fieldId !== field.id));
            }}
          />
        ))}
      </div>
    </div>
  );
}

function SectionConditionsEditor({
  conditions,
  fieldOptions,
  onAdd,
  onRemove,
  onUpdate
}: {
  conditions: PrintTemplateSectionConditionConfig[];
  fieldOptions: Array<{ id: string; label: string }>;
  onAdd: () => void;
  onRemove: (conditionIndex: number) => void;
  onUpdate: (conditionIndex: number, patch: Partial<PrintTemplateSectionConditionConfig>) => void;
}) {
  return (
    <div className="grid gap-2 rounded-lg border border-border bg-muted/20 p-3">
      <div className="flex items-center justify-between gap-3">
        <p className="text-sm font-bold text-foreground">Conditions</p>
        <Button disabled={fieldOptions.length === 0} onClick={onAdd} size="sm" variant="outline">
          <Plus className="size-4" />
          Add
        </Button>
      </div>
      {conditions.length > 0 ? (
        <div className="grid gap-2">
          {conditions.map((condition, conditionIndex) => (
            <div className="grid gap-2 rounded-lg border border-border bg-card/80 p-3 lg:grid-cols-[minmax(0,1fr)_12rem_minmax(0,1fr)_2.5rem]" key={`${condition.fieldId}-${conditionIndex}`}>
              <Select
                label="Field"
                onChange={(event) => onUpdate(conditionIndex, { fieldId: event.target.value })}
                value={condition.fieldId}
              >
                <option value="">Select field</option>
                {fieldOptions.map((field) => (
                  <option key={field.id} value={field.id}>
                    {field.label}
                  </option>
                ))}
              </Select>
              <Select
                label="Operator"
                onChange={(event) => onUpdate(conditionIndex, { operator: event.target.value as PrintTemplateSectionConditionConfig["operator"] })}
                value={condition.operator}
              >
                <option value="equals">Equals</option>
                <option value="not_equals">Not equals</option>
                <option value="contains">Contains</option>
                <option value="is_empty">Is empty</option>
                <option value="is_not_empty">Is not empty</option>
              </Select>
              {conditionRequiresValue(condition.operator) ? (
                <Input
                  label="Value"
                  onChange={(event) => onUpdate(conditionIndex, { value: event.target.value })}
                  value={condition.value ?? ""}
                />
              ) : (
                <div />
              )}
              <Button aria-label="Remove condition" className="self-end" onClick={() => onRemove(conditionIndex)} size="icon" variant="ghost">
                <Trash2 className="size-4" />
              </Button>
            </div>
          ))}
        </div>
      ) : null}
    </div>
  );
}

function conditionRequiresValue(operator: PrintTemplateSectionConditionConfig["operator"]): boolean {
  return operator === "equals" || operator === "not_equals" || operator === "contains";
}

function Metric({ label, value }: { label: string; value: number }) {
  return (
    <div className="rounded-xl border border-border bg-card/70 p-3">
      <p className="text-xs font-bold uppercase text-muted-foreground">{label}</p>
      <p className="mt-1 text-2xl font-black text-foreground">{value}</p>
    </div>
  );
}

function ValidationErrorsPanel({ errors }: { errors: PrintTemplateValidationError[] }) {
  return (
    <div className="rounded-xl border border-danger/30 bg-danger-soft p-4">
      <div className="flex items-center gap-2 text-sm font-bold text-danger">
        <FileText className="size-4" />
        Validation
      </div>
      <ul className="mt-2 grid gap-1 text-sm text-danger">
        {errors.map((error) => (
          <li key={`${error.path}-${error.code}`}>
            <span className="font-bold">{error.path}</span>: {error.message}
          </li>
        ))}
      </ul>
    </div>
  );
}

function splitLines(value: string): string[] {
  return value
    .split(/\r?\n/)
    .map((line) => line.trim())
    .filter(Boolean);
}

function formatDateTime(value: string): string {
  return new Intl.DateTimeFormat("en", {
    month: "short",
    day: "numeric",
    year: "numeric",
    hour: "numeric",
    minute: "2-digit"
  }).format(new Date(value));
}

function getValidationErrors(error: unknown): PrintTemplateValidationError[] {
  return error instanceof PrintingApiError ? error.errors : [];
}

function getErrorMessage(error: unknown): string {
  return error instanceof Error ? error.message : "Printing request failed.";
}

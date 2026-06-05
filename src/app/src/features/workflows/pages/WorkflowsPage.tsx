import { useEffect, useMemo, useState } from "react";
import { AlertCircle, Braces, Loader2, Plus, Power, PowerOff, RefreshCw, Rocket, Save, Workflow } from "lucide-react";
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
import { listForms } from "../../forms/api";
import type { FormSummary } from "../../forms/drafts";
import { createWorkflow, disableWorkflow, enableWorkflow, getWorkflow, listWorkflows, publishWorkflow, updateWorkflow, WorkflowsApiError } from "../api";
import {
  buildWorkflowRequest,
  createEmptyWorkflowDraft,
  createWorkflowDraftFromDetail,
  formatWorkflowDate,
  formatWorkflowConfigText,
  formatWorkflowStatus,
  parseWorkflowConfig,
  validateWorkflowDraft,
  type WorkflowDraft
} from "../builder";
import { VisualWorkflowBuilder } from "../VisualWorkflowBuilder";
import type { WorkflowDetail, WorkflowSummary, WorkflowValidationError } from "../types";

type WorkflowEditorMode = "visual" | "json";

export function WorkflowsPage() {
  const [forms, setForms] = useState<FormSummary[]>([]);
  const [selectedFormId, setSelectedFormId] = useState("");
  const [workflows, setWorkflows] = useState<WorkflowSummary[]>([]);
  const [selectedWorkflowId, setSelectedWorkflowId] = useState("");
  const [draft, setDraft] = useState<WorkflowDraft>(() => createEmptyWorkflowDraft());
  const [loadingInitial, setLoadingInitial] = useState(true);
  const [loadingWorkspace, setLoadingWorkspace] = useState(false);
  const [loadingWorkflow, setLoadingWorkflow] = useState(false);
  const [saving, setSaving] = useState(false);
  const [publishing, setPublishing] = useState(false);
  const [togglingEnabled, setTogglingEnabled] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [validationErrors, setValidationErrors] = useState<WorkflowValidationError[]>([]);
  const [editorMode, setEditorMode] = useState<WorkflowEditorMode>("visual");

  useEffect(() => {
    void loadInitialData();
  }, []);

  useEffect(() => {
    if (!selectedFormId) {
      setWorkflows([]);
      setSelectedWorkflowId("");
      setDraft(createEmptyWorkflowDraft());
      return;
    }

    void loadWorkflowWorkspace(selectedFormId);
  }, [selectedFormId]);

  const selectedForm = forms.find((form) => form.id === selectedFormId) ?? null;
  const selectedWorkflow = workflows.find((workflow) => workflow.id === selectedWorkflowId) ?? null;
  const selectedStatus = selectedWorkflow ? formatWorkflowStatus(selectedWorkflow) : null;
  const errorMap = useMemo(() => createValidationErrorMap(validationErrors), [validationErrors]);
  const parsedWorkflowConfig = useMemo(() => parseWorkflowConfig(draft.configText), [draft.configText]);
  const canUseSavedActions = Boolean(draft.id && draft.concurrencyStamp);

  useEffect(() => {
    if (!parsedWorkflowConfig && editorMode === "visual") {
      setEditorMode("json");
    }
  }, [editorMode, parsedWorkflowConfig]);

  async function loadInitialData() {
    setLoadingInitial(true);
    setError(null);
    setNotice(null);

    try {
      const formItems = await listForms();
      setForms(formItems);
      setSelectedFormId((current) => current || formItems[0]?.id || "");
    } catch (caught) {
      setError(getErrorMessage(caught));
      setForms([]);
      setSelectedFormId("");
    } finally {
      setLoadingInitial(false);
    }
  }

  async function loadWorkflowWorkspace(formId: string) {
    setLoadingWorkspace(true);
    setError(null);
    setNotice(null);
    setValidationErrors([]);
    setSelectedWorkflowId("");

    try {
      const workflowItems = await listWorkflows(formId);
      setWorkflows(workflowItems);
      setDraft(createEmptyWorkflowDraft(selectedForm?.name ?? "Form"));
    } catch (caught) {
      setError(getErrorMessage(caught));
      setWorkflows([]);
      setDraft(createEmptyWorkflowDraft(selectedForm?.name ?? "Form"));
    } finally {
      setLoadingWorkspace(false);
    }
  }

  async function handleRefresh() {
    await loadInitialData();
    if (selectedFormId) {
      await loadWorkflowWorkspace(selectedFormId);
    }
  }

  async function handleSelectWorkflow(workflowId: string) {
    setSelectedWorkflowId(workflowId);
    setLoadingWorkflow(true);
    setError(null);
    setNotice(null);
    setValidationErrors([]);

    try {
      const detail = await getWorkflow(workflowId);
      setDraft(createWorkflowDraftFromDetail(detail));
    } catch (caught) {
      setError(getErrorMessage(caught));
    } finally {
      setLoadingWorkflow(false);
    }
  }

  function handleNewWorkflow() {
    setSelectedWorkflowId("");
    setDraft(createEmptyWorkflowDraft(selectedForm?.name ?? "Form"));
    setValidationErrors([]);
    setError(null);
    setNotice("New workflow draft started.");
  }

  function updateDraft(patch: Partial<WorkflowDraft>) {
    setDraft((current) => ({ ...current, ...patch }));
    setNotice(null);
  }

  function updateWorkflowConfig(config: NonNullable<typeof parsedWorkflowConfig>) {
    updateDraft({ configText: formatWorkflowConfigText(config) });
  }

  async function handleSave() {
    if (!selectedFormId) return;

    const validation = validateWorkflowDraft(draft);
    setValidationErrors(validation.errors);
    setError(null);
    setNotice(null);

    if (!validation.valid) {
      setError("Fix the highlighted workflow fields before saving.");
      return;
    }

    setSaving(true);

    try {
      const request = buildWorkflowRequest(draft);
      const saved = draft.id && draft.concurrencyStamp
        ? await updateWorkflow(draft.id, { ...request, concurrencyStamp: draft.concurrencyStamp })
        : await createWorkflow(selectedFormId, request);

      await afterWorkflowSaved(saved, "Workflow saved.");
    } catch (caught) {
      setError(getErrorMessage(caught));
      setValidationErrors(getValidationErrors(caught));
    } finally {
      setSaving(false);
    }
  }

  async function handlePublish() {
    if (!draft.id || !draft.concurrencyStamp) return;

    setPublishing(true);
    setError(null);
    setNotice(null);

    try {
      const saved = await publishWorkflow(draft.id, draft.concurrencyStamp);
      await afterWorkflowSaved(saved, "Workflow published.");
    } catch (caught) {
      setError(getErrorMessage(caught));
      setValidationErrors(getValidationErrors(caught));
    } finally {
      setPublishing(false);
    }
  }

  async function handleToggleEnabled() {
    if (!draft.id || !draft.concurrencyStamp) return;

    const isEnabled = selectedWorkflow?.isEnabled ?? draft.isEnabled;
    setTogglingEnabled(true);
    setError(null);
    setNotice(null);

    try {
      const saved = isEnabled
        ? await disableWorkflow(draft.id, draft.concurrencyStamp)
        : await enableWorkflow(draft.id, draft.concurrencyStamp);
      await afterWorkflowSaved(saved, saved.isEnabled ? "Workflow enabled." : "Workflow disabled.");
    } catch (caught) {
      setError(getErrorMessage(caught));
      setValidationErrors(getValidationErrors(caught));
    } finally {
      setTogglingEnabled(false);
    }
  }

  async function afterWorkflowSaved(saved: WorkflowDetail, message: string) {
    setSelectedWorkflowId(saved.id);
    setDraft(createWorkflowDraftFromDetail(saved));
    setWorkflows(await listWorkflows(saved.formId));
    setValidationErrors([]);
    setNotice(message);
  }

  return (
    <div className="grid gap-6">
      <PageHeader
        eyebrow="Workflows V5"
        title="Workflow management"
        description="Manage form-scoped workflow definitions, publish versions, and control availability."
        actions={
          <div className="flex flex-wrap gap-2">
            <Button disabled={loadingInitial || loadingWorkspace} onClick={() => void handleRefresh()} variant="outline">
              <RefreshCw className="size-4" />
              Refresh
            </Button>
            <Button disabled={!selectedFormId || loadingWorkspace} onClick={handleNewWorkflow} variant="outline">
              <Plus className="size-4" />
              New workflow
            </Button>
            <Button disabled={!selectedFormId || saving || loadingWorkspace} onClick={() => void handleSave()}>
              {saving ? <Loader2 className="size-4 animate-spin" /> : <Save className="size-4" />}
              {saving ? "Saving..." : "Save"}
            </Button>
          </div>
        }
      />

      {error ? <Alert title="Workflows">{error}</Alert> : null}
      {notice ? <div className="rounded-xl border border-success/40 bg-success/10 px-4 py-3 text-sm font-semibold text-success">{notice}</div> : null}

      <section className="grid gap-4 xl:grid-cols-[22rem_minmax(0,1fr)]">
        <Card className="self-start">
          <CardHeader>
            <CardTitle>Form workflows</CardTitle>
            <CardDescription>Select a managed form and workflow definition.</CardDescription>
          </CardHeader>
          <CardContent className="grid gap-4">
            <Select disabled={loadingInitial || forms.length === 0} label="Form" onChange={(event) => setSelectedFormId(event.target.value)} value={selectedFormId}>
              {forms.map((form) => (
                <option key={form.id} value={form.id}>
                  {form.name}
                </option>
              ))}
            </Select>

            <div className="grid grid-cols-3 gap-3">
              <Metric label="Total" value={workflows.length} />
              <Metric label="Live" value={workflows.filter((workflow) => workflow.status === "published").length} />
              <Metric label="On" value={workflows.filter((workflow) => workflow.isEnabled).length} />
            </div>

            {loadingWorkspace ? (
              <p className="text-sm font-semibold text-muted-foreground">Loading workflows...</p>
            ) : forms.length === 0 ? (
              <div className="rounded-xl border border-dashed border-border bg-muted/40 p-6 text-sm font-semibold text-muted-foreground">
                No managed forms are available.
              </div>
            ) : workflows.length > 0 ? (
              <div className="grid gap-2">
                {workflows.map((workflow) => {
                  const status = formatWorkflowStatus(workflow);

                  return (
                    <button
                      className={[
                        "rounded-xl border p-3 text-left transition hover:bg-muted/50",
                        selectedWorkflowId === workflow.id ? "border-primary bg-primary-soft" : "border-border bg-card/70"
                      ].join(" ")}
                      key={workflow.id}
                      onClick={() => void handleSelectWorkflow(workflow.id)}
                      type="button"
                    >
                      <div className="flex items-start justify-between gap-3">
                        <div className="min-w-0">
                          <p className="truncate font-bold text-foreground">{workflow.name}</p>
                          <p className="mt-1 text-xs font-semibold text-muted-foreground">
                            {workflow.stateCount} states, {workflow.transitionCount} transitions
                          </p>
                        </div>
                        <Badge variant={status.tone}>{status.label}</Badge>
                      </div>
                      <p className="mt-3 text-xs text-muted-foreground">Updated {formatWorkflowDate(workflow.updatedAt ?? workflow.createdAt)}</p>
                      {workflow.currentVersionNumber ? (
                        <p className="mt-1 text-xs text-muted-foreground">Version {workflow.currentVersionNumber}</p>
                      ) : null}
                    </button>
                  );
                })}
              </div>
            ) : (
              <EmptyState
                title="No workflows"
                description="Create the first workflow definition for this form."
                action={
                  <Button disabled={!selectedFormId} onClick={handleNewWorkflow} variant="outline">
                    <Plus className="size-4" />
                    New workflow
                  </Button>
                }
              />
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <div className="flex flex-wrap items-start justify-between gap-3">
              <div>
                <CardTitle>{selectedWorkflow ? "Edit workflow" : "New workflow"}</CardTitle>
                <CardDescription>
                  {selectedForm ? `${selectedForm.name}.` : "Choose a form to start authoring workflows."}
                </CardDescription>
              </div>
              <div className="flex flex-wrap gap-2">
                {selectedStatus ? <Badge variant={selectedStatus.tone}>{selectedStatus.label}</Badge> : null}
                {selectedWorkflow?.currentVersionNumber ? <Badge>Version {selectedWorkflow.currentVersionNumber}</Badge> : null}
              </div>
            </div>
          </CardHeader>
          <CardContent className="grid gap-5">
            {loadingWorkflow ? (
              <div className="flex items-center gap-2 text-sm font-semibold text-muted-foreground">
                <Loader2 className="size-4 animate-spin" />
                Loading workflow...
              </div>
            ) : null}

            <div className="grid gap-4 lg:grid-cols-[minmax(0,1fr)_11rem]">
              <Input error={errorMap.get("name")} label="Name" onChange={(event) => updateDraft({ name: event.target.value })} value={draft.name} />
              <Checkbox checked={draft.isEnabled} label="Enabled" onChange={(event) => updateDraft({ isEnabled: event.target.checked })} />
            </div>

            <Textarea label="Description" onChange={(event) => updateDraft({ description: event.target.value })} value={draft.description} />

            <div className="grid gap-3">
              <div className="flex flex-wrap items-center justify-between gap-3">
                <div>
                  <h3 className="text-sm font-black text-foreground">Definition</h3>
                  {errorMap.get("config") ? <p className="mt-1 text-xs font-semibold text-danger">{errorMap.get("config")}</p> : null}
                </div>
                <div className="flex rounded-xl border border-border bg-card/80 p-1">
                  <Button
                    disabled={!parsedWorkflowConfig}
                    onClick={() => setEditorMode("visual")}
                    size="sm"
                    variant={editorMode === "visual" ? "primary" : "ghost"}
                  >
                    <Workflow className="size-4" />
                    Visual
                  </Button>
                  <Button onClick={() => setEditorMode("json")} size="sm" variant={editorMode === "json" ? "primary" : "ghost"}>
                    <Braces className="size-4" />
                    JSON
                  </Button>
                </div>
              </div>

              {editorMode === "visual" && parsedWorkflowConfig ? (
                <VisualWorkflowBuilder config={parsedWorkflowConfig} onChange={updateWorkflowConfig} validationErrors={validationErrors} />
              ) : (
                <Textarea
                  className="min-h-[28rem] font-mono text-xs leading-5"
                  error={errorMap.get("config")}
                  label="Definition JSON"
                  onChange={(event) => updateDraft({ configText: event.target.value })}
                  spellCheck={false}
                  value={draft.configText}
                />
              )}
            </div>

            {validationErrors.length > 0 ? <ValidationErrorsPanel errors={validationErrors} /> : null}

            <div className="flex flex-wrap gap-2">
              <Button disabled={!selectedFormId || saving || loadingWorkspace} onClick={() => void handleSave()}>
                {saving ? <Loader2 className="size-4 animate-spin" /> : <Save className="size-4" />}
                {saving ? "Saving..." : "Save"}
              </Button>
              <Button disabled={!canUseSavedActions || publishing} onClick={() => void handlePublish()} variant="outline">
                {publishing ? <Loader2 className="size-4 animate-spin" /> : <Rocket className="size-4" />}
                {publishing ? "Publishing..." : "Publish"}
              </Button>
              <Button disabled={!canUseSavedActions || togglingEnabled} onClick={() => void handleToggleEnabled()} variant="outline">
                {togglingEnabled ? (
                  <Loader2 className="size-4 animate-spin" />
                ) : selectedWorkflow?.isEnabled ?? draft.isEnabled ? (
                  <PowerOff className="size-4" />
                ) : (
                  <Power className="size-4" />
                )}
                {selectedWorkflow?.isEnabled ?? draft.isEnabled ? "Disable" : "Enable"}
              </Button>
            </div>
          </CardContent>
        </Card>
      </section>
    </div>
  );
}

function Metric({ label, value }: { label: string; value: number }) {
  return (
    <div className="rounded-xl border border-border bg-card/70 p-3">
      <p className="text-xs font-bold uppercase text-muted-foreground">{label}</p>
      <p className="mt-1 text-2xl font-black text-foreground">{value}</p>
    </div>
  );
}

function ValidationErrorsPanel({ errors }: { errors: WorkflowValidationError[] }) {
  return (
    <div className="rounded-xl border border-danger/30 bg-danger-soft p-4">
      <div className="flex items-center gap-2 text-sm font-bold text-danger">
        <AlertCircle className="size-4" />
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

function createValidationErrorMap(errors: WorkflowValidationError[]): Map<string, string> {
  return new Map(errors.map((error) => [error.path, error.message]));
}

function getValidationErrors(error: unknown): WorkflowValidationError[] {
  if (error instanceof WorkflowsApiError) {
    return error.errors;
  }

  return [];
}

function getErrorMessage(error: unknown): string {
  return error instanceof Error ? error.message : "Workflow request failed.";
}

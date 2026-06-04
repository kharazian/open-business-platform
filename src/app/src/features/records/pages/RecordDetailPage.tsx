import { type ReactNode, useEffect, useMemo, useState } from "react";
import { ArrowLeft, Edit3, GitBranch, MoveRight, Play, Printer, RefreshCw, Save, Trash2, X } from "lucide-react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { Alert } from "../../../components/ui/Alert";
import { Badge } from "../../../components/ui/Badge";
import { Button } from "../../../components/ui/Button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "../../../components/ui/Card";
import { EmptyState } from "../../../components/ui/EmptyState";
import { PageHeader } from "../../../components/ui/PageHeader";
import { Skeleton } from "../../../components/ui/Skeleton";
import { FormRenderer } from "../../forms/components/FormRenderer";
import { deleteRecord, getRecord, updateRecord, type FormRecordDetail } from "../../forms/api";
import type { FormField, FormRecordValue, FormRecordValues, ValidationError } from "../../forms/types";
import { validateRecordValues } from "../../forms/validation";
import { PrintDocumentFooter, PrintDocumentHeader } from "../../printing/components/PrintDocument";
import { getGeneratedAtPrintMetadata } from "../../printing/printLayout";
import { executeRecordWorkflowTransition, getRecordWorkflow, startRecordWorkflow } from "../../workflows/api";
import type { RecordWorkflowState, RecordWorkflowTransition } from "../../workflows/types";
import { createRecordEditDraft, createUpdateRecordRequest, getRecordListPath } from "../recordEditor";
import { getRecordDetailPrintDescription, requestBrowserPrint } from "../recordPrint";

export function RecordDetailPage() {
  const { recordId } = useParams();
  const navigate = useNavigate();
  const resolvedRecordId = recordId ?? "";
  const [record, setRecord] = useState<FormRecordDetail | null>(null);
  const [draftValues, setDraftValues] = useState<FormRecordValues>({});
  const [validationErrors, setValidationErrors] = useState<ValidationError[]>([]);
  const [editing, setEditing] = useState(false);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [workflowState, setWorkflowState] = useState<RecordWorkflowState | null>(null);
  const [workflowLoading, setWorkflowLoading] = useState(false);
  const [workflowAction, setWorkflowAction] = useState<string | null>(null);
  const [workflowError, setWorkflowError] = useState<string | null>(null);
  const [selectedWorkflowId, setSelectedWorkflowId] = useState("");

  useEffect(() => {
    void refreshRecord();
  }, [resolvedRecordId]);

  const fieldsById = useMemo(() => {
    return new Map(record?.schema.fields.map((field) => [field.id, field]) ?? []);
  }, [record]);

  async function refreshRecord() {
    if (!resolvedRecordId) {
      setError("Record id is required.");
      setLoading(false);
      return;
    }

    setLoading(true);
    setError(null);
    setWorkflowError(null);

    try {
      const [loadedRecord, loadedWorkflowState] = await Promise.all([getRecord(resolvedRecordId), getRecordWorkflow(resolvedRecordId)]);
      setRecord(loadedRecord);
      setDraftValues(createRecordEditDraft(loadedRecord));
      applyWorkflowState(loadedWorkflowState);
      setValidationErrors([]);
      setEditing(false);
    } catch (caught) {
      setError(getErrorMessage(caught));
    } finally {
      setLoading(false);
    }
  }

  function startEditing() {
    if (!record) return;
    setDraftValues(createRecordEditDraft(record));
    setValidationErrors([]);
    setEditing(true);
  }

  function cancelEditing() {
    if (!record) return;
    setDraftValues(createRecordEditDraft(record));
    setValidationErrors([]);
    setEditing(false);
  }

  function handleDraftChange(fieldId: string, value: FormRecordValue) {
    setDraftValues((current) => ({ ...current, [fieldId]: value }));
  }

  async function saveRecord() {
    if (!record) return;

    const validation = validateRecordValues(record.schema, draftValues);
    setValidationErrors(validation.errors);

    if (!validation.valid) {
      return;
    }

    setSaving(true);
    setError(null);

    try {
      const updatedRecord = await updateRecord(record.id, createUpdateRecordRequest(record, draftValues));
      setRecord(updatedRecord);
      setDraftValues(createRecordEditDraft(updatedRecord));
      setValidationErrors([]);
      setEditing(false);
      await refreshWorkflowState(updatedRecord.id);
    } catch (caught) {
      setError(getErrorMessage(caught));
    } finally {
      setSaving(false);
    }
  }

  async function refreshWorkflowState(targetRecordId = resolvedRecordId) {
    if (!targetRecordId) return;

    setWorkflowLoading(true);
    setWorkflowError(null);

    try {
      applyWorkflowState(await getRecordWorkflow(targetRecordId));
    } catch (caught) {
      setWorkflowError(getErrorMessage(caught));
    } finally {
      setWorkflowLoading(false);
    }
  }

  function applyWorkflowState(nextState: RecordWorkflowState) {
    setWorkflowState(nextState);
    setSelectedWorkflowId((current) => {
      const currentStillAvailable = nextState.availableWorkflows.some((workflow) => workflow.workflowDefinitionId === current);
      return currentStillAvailable ? current : nextState.availableWorkflows[0]?.workflowDefinitionId ?? "";
    });
  }

  async function beginWorkflow() {
    if (!record || !workflowState || !selectedWorkflowId) return;

    setWorkflowAction(`start:${selectedWorkflowId}`);
    setWorkflowError(null);

    try {
      const nextState = await startRecordWorkflow(record.id, {
        workflowDefinitionId: selectedWorkflowId,
        concurrencyStamp: workflowState.recordConcurrencyStamp
      });
      applyWorkflowState(nextState);
      const updatedRecord = await getRecord(record.id);
      setRecord(updatedRecord);
      setDraftValues(createRecordEditDraft(updatedRecord));
    } catch (caught) {
      setWorkflowError(getErrorMessage(caught));
    } finally {
      setWorkflowAction(null);
    }
  }

  async function executeWorkflowTransition(transition: RecordWorkflowTransition) {
    if (!record || !workflowState) return;

    setWorkflowAction(`transition:${transition.key}`);
    setWorkflowError(null);

    try {
      const nextState = await executeRecordWorkflowTransition(record.id, transition.key, {
        concurrencyStamp: workflowState.recordConcurrencyStamp
      });
      applyWorkflowState(nextState);
      const updatedRecord = await getRecord(record.id);
      setRecord(updatedRecord);
      setDraftValues(createRecordEditDraft(updatedRecord));
    } catch (caught) {
      setWorkflowError(getErrorMessage(caught));
    } finally {
      setWorkflowAction(null);
    }
  }

  async function removeRecord() {
    if (!record) return;

    if (!window.confirm("Delete this record?")) {
      return;
    }

    setDeleting(true);
    setError(null);

    try {
      await deleteRecord(record.id);
      navigate(getRecordListPath(record));
    } catch (caught) {
      setError(getErrorMessage(caught));
      setDeleting(false);
    }
  }

  return (
    <div className="grid gap-6 print-area">
      {record ? (
        <PrintDocumentHeader
          description={getRecordDetailPrintDescription(formatDateTime(record.createdAt), shortId(record.formVersionId))}
          eyebrow="Record detail"
          metadata={[getGeneratedAtPrintMetadata()]}
          title={`Record ${shortId(record.id)}`}
        />
      ) : null}
      <div data-print-hide="true">
        <PageHeader
          eyebrow="Record detail"
          title={record ? shortId(record.id) : "Record"}
          description={record ? `Submitted ${formatDateTime(record.createdAt)}` : "Submitted record values and form version."}
          actions={
            <div className="flex flex-wrap gap-2">
              {record && editing ? (
                <>
                  <Button disabled={saving || deleting} onClick={cancelEditing} variant="outline">
                    <X className="size-4" />
                    Cancel
                  </Button>
                  <Button disabled={saving || deleting} onClick={() => void saveRecord()}>
                    <Save className="size-4" />
                    Save
                  </Button>
                </>
              ) : record ? (
                <Button disabled={loading || deleting} onClick={startEditing} variant="outline">
                  <Edit3 className="size-4" />
                  Edit
                </Button>
              ) : null}
              {record ? (
                <Button disabled={loading || saving || deleting} onClick={() => void removeRecord()} variant="danger">
                  <Trash2 className="size-4" />
                  Delete
                </Button>
              ) : null}
              {record ? (
                <Button disabled={loading || saving || deleting || editing} onClick={() => requestBrowserPrint()} variant="outline">
                  <Printer className="size-4" />
                  Print
                </Button>
              ) : null}
              <Button disabled={loading || saving || deleting} onClick={() => void refreshRecord()} variant="outline">
                <RefreshCw className="size-4" />
                Refresh
              </Button>
              {record ? (
                <LinkButton to={getRecordListPath(record)}>
                  <ArrowLeft className="size-4" />
                  Records
                </LinkButton>
              ) : (
                <LinkButton to="/forms">
                  <ArrowLeft className="size-4" />
                  Forms
                </LinkButton>
              )}
            </div>
          }
        />
      </div>

      {error ? (
        <div data-print-hide="true">
          <Alert title="Record detail">{error}</Alert>
        </div>
      ) : null}

      {loading ? (
        <LoadingRecord />
      ) : record ? (
        <>
          <section className="grid gap-4 md:grid-cols-3">
            <SummaryTile label="Status" value={<Badge variant={record.status === "active" ? "success" : "default"}>{record.status}</Badge>} />
            <SummaryTile label="Form version" value={shortId(record.formVersionId)} />
            <SummaryTile label="Created" value={formatDateTime(record.createdAt)} />
          </section>

          <WorkflowPanel
            action={workflowAction}
            error={workflowError}
            loading={workflowLoading}
            onRefresh={() => void refreshWorkflowState(record.id)}
            onSelectWorkflow={setSelectedWorkflowId}
            onStart={() => void beginWorkflow()}
            onTransition={(transition) => void executeWorkflowTransition(transition)}
            selectedWorkflowId={selectedWorkflowId}
            state={workflowState}
          />

          <Card className="print-card">
            <CardHeader>
              <CardTitle>{editing ? "Edit values" : "Values"}</CardTitle>
              <CardDescription>
                {editing ? "Update values against the stored form version schema." : "Values captured against the stored form version schema."}
              </CardDescription>
            </CardHeader>
            <CardContent>
              {editing ? (
                <FormRenderer
                  errors={validationErrors}
                  onChange={handleDraftChange}
                  onSubmit={() => void saveRecord()}
                  schema={record.schema}
                  submitLabel={saving ? "Saving" : "Save changes"}
                  values={draftValues}
                />
              ) : (
                <div className="grid gap-3">
                  {record.schema.fields.map((field) => (
                    <ValueRow field={field} key={field.id} value={record.values[field.id]} />
                  ))}
                  {Object.keys(record.values)
                    .filter((fieldId) => !fieldsById.has(fieldId))
                    .map((fieldId) => (
                      <ValueRow key={fieldId} label={fieldId} value={record.values[fieldId]} />
                    ))}
                </div>
              )}
            </CardContent>
          </Card>
          <PrintDocumentFooter />
        </>
      ) : (
        <EmptyState title="Record not found" description="The requested record could not be loaded." action={<LinkButton to="/forms">Forms</LinkButton>} />
      )}
    </div>
  );
}

function ValueRow({ field, label, value }: { field?: FormField; label?: string; value: FormRecordValue | undefined }) {
  return (
    <div className="print-value-row grid gap-2 rounded-xl border border-border bg-card/70 p-4 md:grid-cols-[14rem_minmax(0,1fr)]">
      <div className="min-w-0">
        <p className="truncate font-bold text-foreground">{field?.label ?? label}</p>
        {field ? <p className="mt-1 text-xs font-semibold uppercase tracking-normal text-muted-foreground">{field.type}</p> : null}
      </div>
      <p className="min-w-0 whitespace-pre-wrap break-words text-sm leading-6 text-foreground">{formatRecordValue(value)}</p>
    </div>
  );
}

function SummaryTile({ label, value }: { label: string; value: ReactNode }) {
  return (
    <Card className="p-5">
      <p className="text-sm font-bold text-muted-foreground">{label}</p>
      <div className="mt-3 text-base font-bold text-foreground">{value}</div>
    </Card>
  );
}

function WorkflowPanel({
  action,
  error,
  loading,
  onRefresh,
  onSelectWorkflow,
  onStart,
  onTransition,
  selectedWorkflowId,
  state
}: {
  action: string | null;
  error: string | null;
  loading: boolean;
  onRefresh: () => void;
  onSelectWorkflow: (workflowId: string) => void;
  onStart: () => void;
  onTransition: (transition: RecordWorkflowTransition) => void;
  selectedWorkflowId: string;
  state: RecordWorkflowState | null;
}) {
  const hasActiveWorkflow = Boolean(state?.workflowDefinitionId && state.stateKey);
  const selectedWorkflow = state?.availableWorkflows.find((workflow) => workflow.workflowDefinitionId === selectedWorkflowId);

  return (
    <Card data-print-hide="true">
      <CardHeader className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
        <div>
          <CardTitle className="flex items-center gap-2">
            <GitBranch className="size-5" />
            Workflow
          </CardTitle>
          <CardDescription>
            {hasActiveWorkflow ? `${state?.workflowName ?? "Workflow"} version ${state?.workflowVersionNumber ?? "-"}` : "No active workflow"}
          </CardDescription>
        </div>
        <Button disabled={loading || action !== null} onClick={onRefresh} size="sm" variant="outline">
          <RefreshCw className="size-4" />
          Refresh
        </Button>
      </CardHeader>
      <CardContent className="grid gap-4">
        {error ? <Alert title="Workflow">{error}</Alert> : null}
        {loading && !state ? (
          <Skeleton className="h-28" />
        ) : hasActiveWorkflow && state ? (
          <>
            <div className="flex flex-wrap items-center gap-3">
              <Badge variant="default">{state.stateKey}</Badge>
              <span className="text-sm font-semibold text-muted-foreground">Current state</span>
            </div>
            <div className="flex flex-wrap gap-2">
              {state.availableTransitions.length > 0 ? (
                state.availableTransitions.map((transition) => (
                  <Button
                    disabled={action !== null || loading}
                    key={transition.key}
                    onClick={() => onTransition(transition)}
                    variant="outline"
                  >
                    <MoveRight className="size-4" />
                    {transition.requiresApproval ? `Request ${transition.name}` : transition.name}
                  </Button>
                ))
              ) : (
                <p className="text-sm font-semibold text-muted-foreground">No direct transitions available.</p>
              )}
            </div>
            <WorkflowHistoryList state={state} />
          </>
        ) : state && state.availableWorkflows.length > 0 ? (
          <div className="grid gap-3 md:grid-cols-[minmax(0,1fr)_auto] md:items-end">
            <label className="grid gap-2 text-sm font-bold text-foreground">
              Workflow
              <select
                className="min-h-10 rounded-xl border border-border bg-card px-3 text-sm font-semibold text-foreground outline-none ring-primary/20 focus-visible:ring-4"
                onChange={(event) => onSelectWorkflow(event.target.value)}
                value={selectedWorkflowId}
              >
                {state.availableWorkflows.map((workflow) => (
                  <option key={workflow.workflowDefinitionId} value={workflow.workflowDefinitionId}>
                    {workflow.name} v{workflow.currentVersionNumber} to {workflow.initialStateKey}
                  </option>
                ))}
              </select>
            </label>
            <Button disabled={!selectedWorkflow || action !== null || loading} onClick={onStart}>
              <Play className="size-4" />
              Start
            </Button>
          </div>
        ) : (
          <p className="rounded-xl border border-border bg-card-muted px-4 py-3 text-sm font-semibold text-muted-foreground">
            This record has no active workflow or start option.
          </p>
        )}
      </CardContent>
    </Card>
  );
}

function WorkflowHistoryList({ state }: { state: RecordWorkflowState }) {
  if (state.history.length === 0) {
    return null;
  }

  return (
    <div className="grid gap-2">
      <p className="text-sm font-bold text-muted-foreground">Recent workflow history</p>
      <div className="grid gap-2">
        {state.history.slice(0, 5).map((entry) => (
          <div className="grid gap-1 rounded-xl border border-border bg-card-muted px-3 py-2" key={entry.id}>
            <div className="flex flex-wrap items-center gap-2 text-sm font-bold text-foreground">
              <span>{formatWorkflowAction(entry.action)}</span>
              {entry.transitionKey ? <Badge variant="default">{entry.transitionKey}</Badge> : null}
            </div>
            <p className="text-xs font-semibold text-muted-foreground">
              {entry.fromStateKey ? `${entry.fromStateKey} -> ` : ""}
              {entry.toStateKey} on {formatDateTime(entry.createdAt)}
            </p>
          </div>
        ))}
      </div>
    </div>
  );
}

function LinkButton({ children, to }: { children: ReactNode; to: string }) {
  return (
    <Link
      className="control-transition inline-flex min-h-10 items-center justify-center gap-2 rounded-xl border border-border bg-card/90 px-4 text-sm font-bold text-foreground hover:bg-muted"
      to={to}
    >
      {children}
    </Link>
  );
}

function LoadingRecord() {
  return (
    <div className="grid gap-4">
      <section className="grid gap-4 md:grid-cols-3">
        <Skeleton className="h-24" />
        <Skeleton className="h-24" />
        <Skeleton className="h-24" />
      </section>
      <Skeleton className="h-64" />
    </div>
  );
}

function shortId(value: string): string {
  return value.length > 8 ? value.slice(0, 8) : value;
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

function formatRecordValue(value: FormRecordValue | undefined): string {
  if (value === null || value === undefined || value === "") return "Empty";
  if (typeof value === "boolean") return value ? "Yes" : "No";
  return String(value);
}

function formatWorkflowAction(action: string): string {
  if (action === "workflow_started") return "Started";
  if (action === "workflow_transitioned") return "Transitioned";
  return action.replaceAll("_", " ");
}

function getErrorMessage(error: unknown): string {
  return error instanceof Error ? error.message : "Record request failed.";
}

import { useEffect, useMemo, useState } from "react";
import { Activity, ListChecks, Plus, RefreshCw, RotateCcw, Save, Trash2 } from "lucide-react";
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
import { getForm, getPublishedFormForSubmission, listForms, type FormDetail } from "../../forms/api";
import type { FormSummary } from "../../forms/drafts";
import type { FormSchema } from "../../forms/types";
import { listDepartments, listGroups, listUsers } from "../../users/api";
import type { DepartmentDto, GroupDto, UserDto } from "../../users/types";
import { createTrigger, getTrigger, listTriggerLogs, listTriggers, retryTriggerLog, updateTrigger } from "../api";
import {
  buildTriggerRequest,
  createEmptyTriggerDraft,
  createTriggerActionDraft,
  createTriggerConditionDraft,
  createTriggerDraftFromDetail,
  formatTriggerActionLabel,
  formatTriggerConditionLabel,
  formatTriggerDate,
  formatTriggerEventLabel,
  formatTriggerJson,
  formatTriggerLogStatus,
  getTriggerFieldOptions,
  triggerActionOptions,
  triggerConditionOptions,
  triggerEventOptions,
  validateTriggerDraft,
  type TriggerActionDraft,
  type TriggerConditionDraft,
  type TriggerDraft,
  type TriggerFieldOption
} from "../builder";
import type {
  TriggerActionType,
  TriggerConditionType,
  TriggerDetail,
  TriggerEventName,
  TriggerExecutionLog,
  TriggerSummary,
  TriggerValidationError
} from "../types";

export function TriggersPage() {
  const [forms, setForms] = useState<FormSummary[]>([]);
  const [selectedFormId, setSelectedFormId] = useState("");
  const [formDetail, setFormDetail] = useState<FormDetail | null>(null);
  const [authoringSchema, setAuthoringSchema] = useState<FormSchema | null>(null);
  const [triggers, setTriggers] = useState<TriggerSummary[]>([]);
  const [selectedTriggerId, setSelectedTriggerId] = useState("");
  const [draft, setDraft] = useState<TriggerDraft>(() => createEmptyTriggerDraft());
  const [logs, setLogs] = useState<TriggerExecutionLog[]>([]);
  const [users, setUsers] = useState<UserDto[]>([]);
  const [departments, setDepartments] = useState<DepartmentDto[]>([]);
  const [groups, setGroups] = useState<GroupDto[]>([]);
  const [loadingInitial, setLoadingInitial] = useState(true);
  const [loadingWorkspace, setLoadingWorkspace] = useState(false);
  const [loadingTrigger, setLoadingTrigger] = useState(false);
  const [saving, setSaving] = useState(false);
  const [retryingLogId, setRetryingLogId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [lookupWarning, setLookupWarning] = useState<string | null>(null);
  const [validationErrors, setValidationErrors] = useState<TriggerValidationError[]>([]);

  useEffect(() => {
    void loadInitialData();
  }, []);

  useEffect(() => {
    if (!selectedFormId) {
      setFormDetail(null);
      setAuthoringSchema(null);
      setTriggers([]);
      setSelectedTriggerId("");
      setLogs([]);
      setDraft(createEmptyTriggerDraft());
      return;
    }

    void loadFormWorkspace(selectedFormId);
  }, [selectedFormId]);

  const selectedForm = forms.find((form) => form.id === selectedFormId) ?? null;
  const selectedTrigger = triggers.find((trigger) => trigger.id === selectedTriggerId) ?? null;
  const fieldOptions = useMemo(() => getTriggerFieldOptions(authoringSchema), [authoringSchema]);
  const activeUsers = users.filter((user) => user.isActive);
  const activeDepartments = departments.filter((department) => department.isActive);
  const activeGroups = groups.filter((group) => group.isActive);
  const errorMap = useMemo(() => createValidationErrorMap(validationErrors), [validationErrors]);

  async function loadInitialData() {
    setLoadingInitial(true);
    setError(null);
    setLookupWarning(null);

    try {
      const formItems = await listForms();
      setForms(formItems);
      setSelectedFormId((current) => current || formItems[0]?.id || "");
      await loadLookupData();
    } catch (caught) {
      setError(getErrorMessage(caught));
    } finally {
      setLoadingInitial(false);
    }
  }

  async function loadLookupData() {
    const [userResult, departmentResult, groupResult] = await Promise.allSettled([listUsers(), listDepartments(), listGroups()]);
    const failures = [userResult, departmentResult, groupResult].filter((result) => result.status === "rejected").length;

    if (userResult.status === "fulfilled") setUsers(userResult.value);
    if (departmentResult.status === "fulfilled") setDepartments(departmentResult.value);
    if (groupResult.status === "fulfilled") setGroups(groupResult.value);

    if (failures > 0) {
      setLookupWarning("Some assignment lookup data could not be loaded. Backend validation will still enforce valid targets.");
    }
  }

  async function loadFormWorkspace(formId: string) {
    setLoadingWorkspace(true);
    setError(null);
    setNotice(null);
    setValidationErrors([]);
    setSelectedTriggerId("");
    setLogs([]);

    try {
      const [form, triggerItems] = await Promise.all([getForm(formId), listTriggers(formId)]);
      const schema = await resolveAuthoringSchema(form);
      setFormDetail(form);
      setAuthoringSchema(schema);
      setTriggers(triggerItems);
      setDraft(createEmptyTriggerDraft(form.name));
    } catch (caught) {
      setError(getErrorMessage(caught));
      setFormDetail(null);
      setAuthoringSchema(null);
      setTriggers([]);
      setDraft(createEmptyTriggerDraft(selectedForm?.name));
    } finally {
      setLoadingWorkspace(false);
    }
  }

  async function resolveAuthoringSchema(form: FormDetail): Promise<FormSchema | null> {
    if (form.draftSchema) {
      return form.draftSchema;
    }

    if (!form.currentVersionId) {
      return null;
    }

    try {
      const published = await getPublishedFormForSubmission(form.id);
      return published.schema;
    } catch {
      return null;
    }
  }

  async function handleRefresh() {
    await loadInitialData();
    if (selectedFormId) {
      await loadFormWorkspace(selectedFormId);
    }
  }

  async function handleSelectTrigger(triggerId: string) {
    setSelectedTriggerId(triggerId);
    setLoadingTrigger(true);
    setError(null);
    setNotice(null);
    setValidationErrors([]);

    try {
      const [detail, triggerLogs] = await Promise.all([getTrigger(triggerId), listTriggerLogs(triggerId)]);
      setDraft(createTriggerDraftFromDetail(detail));
      setLogs(triggerLogs);
    } catch (caught) {
      setError(getErrorMessage(caught));
      setLogs([]);
    } finally {
      setLoadingTrigger(false);
    }
  }

  function handleNewTrigger() {
    setSelectedTriggerId("");
    setDraft(createEmptyTriggerDraft(selectedForm?.name ?? "Form"));
    setLogs([]);
    setValidationErrors([]);
    setNotice("New trigger draft started.");
  }

  async function handleSave() {
    if (!selectedFormId) return;

    const validation = validateTriggerDraft(draft);
    setValidationErrors(validation.errors);
    setError(null);
    setNotice(null);

    if (!validation.valid) {
      setError("Fix the highlighted trigger fields before saving.");
      return;
    }

    setSaving(true);

    try {
      const request = buildTriggerRequest(draft);
      const saved = draft.id && draft.concurrencyStamp
        ? await updateTrigger(draft.id, { ...request, concurrencyStamp: draft.concurrencyStamp })
        : await createTrigger(selectedFormId, request);

      await afterTriggerSaved(saved);
    } catch (caught) {
      setError(getErrorMessage(caught));
      setValidationErrors(getValidationErrors(caught));
    } finally {
      setSaving(false);
    }
  }

  async function afterTriggerSaved(saved: TriggerDetail) {
    setSelectedTriggerId(saved.id);
    setDraft(createTriggerDraftFromDetail(saved));
    setTriggers(await listTriggers(saved.formId));
    setLogs(await listTriggerLogs(saved.id));
    setNotice("Trigger saved.");
  }

  async function handleRetryLog(logId: string) {
    if (!selectedTriggerId) return;

    setRetryingLogId(logId);
    setError(null);
    setNotice(null);

    try {
      const retriedLog = await retryTriggerLog(selectedTriggerId, logId);
      setLogs(await listTriggerLogs(selectedTriggerId));
      setNotice(retriedLog.status === "success" ? "Trigger retry succeeded." : "Trigger retry created a failed retry log.");
    } catch (caught) {
      setError(getErrorMessage(caught));
    } finally {
      setRetryingLogId(null);
    }
  }

  function updateDraft(patch: Partial<TriggerDraft>) {
    setDraft((current) => ({ ...current, ...patch }));
    setNotice(null);
  }

  function updateCondition(clientId: string, patch: Partial<TriggerConditionDraft>) {
    setDraft((current) => ({
      ...current,
      conditions: current.conditions.map((condition) => (condition.clientId === clientId ? { ...condition, ...patch } : condition))
    }));
    setNotice(null);
  }

  function updateAction(clientId: string, patch: Partial<TriggerActionDraft>) {
    setDraft((current) => ({
      ...current,
      actions: current.actions.map((action) => (action.clientId === clientId ? { ...action, ...patch } : action))
    }));
    setNotice(null);
  }

  function handleConditionTypeChange(condition: TriggerConditionDraft, type: TriggerConditionType) {
    updateCondition(condition.clientId, {
      ...createTriggerConditionDraft(type),
      clientId: condition.clientId,
      type
    });
  }

  function handleActionTypeChange(action: TriggerActionDraft, type: TriggerActionType) {
    updateAction(action.clientId, {
      ...createTriggerActionDraft(type),
      clientId: action.clientId,
      id: action.id || action.clientId,
      type
    });
  }

  return (
    <div className="grid gap-6">
      <PageHeader
        eyebrow="Triggers V4"
        title="Trigger management"
        description="Create form-scoped automations from record events, supported conditions, and approved actions."
        actions={
          <div className="flex flex-wrap gap-2">
            <Button disabled={loadingInitial || loadingWorkspace} onClick={() => void handleRefresh()} variant="outline">
              <RefreshCw className="size-4" />
              Refresh
            </Button>
            <Button disabled={!selectedFormId || loadingWorkspace} onClick={handleNewTrigger} variant="outline">
              <Plus className="size-4" />
              New trigger
            </Button>
            <Button disabled={!selectedFormId || saving || loadingWorkspace} onClick={() => void handleSave()}>
              <Save className="size-4" />
              {saving ? "Saving..." : "Save"}
            </Button>
          </div>
        }
      />

      {error ? <Alert title="Triggers">{error}</Alert> : null}
      {lookupWarning ? <Alert title="Lookup data">{lookupWarning}</Alert> : null}
      {notice ? <div className="rounded-xl border border-success/40 bg-success/10 px-4 py-3 text-sm font-semibold text-success">{notice}</div> : null}

      <section className="grid gap-4 xl:grid-cols-[22rem_minmax(0,1fr)]">
        <Card className="self-start">
          <CardHeader>
            <CardTitle>Form triggers</CardTitle>
            <CardDescription>Select a managed form and automation.</CardDescription>
          </CardHeader>
          <CardContent className="grid gap-4">
            <Select disabled={loadingInitial || forms.length === 0} label="Form" onChange={(event) => setSelectedFormId(event.target.value)} value={selectedFormId}>
              {forms.map((form) => (
                <option key={form.id} value={form.id}>
                  {form.name}
                </option>
              ))}
            </Select>

            <div className="grid grid-cols-2 gap-3">
              <Metric label="Triggers" value={triggers.length} />
              <Metric label="Enabled" value={triggers.filter((trigger) => trigger.isEnabled).length} />
            </div>

            {loadingWorkspace ? (
              <p className="text-sm font-semibold text-muted-foreground">Loading triggers...</p>
            ) : triggers.length > 0 ? (
              <div className="grid gap-2">
                {triggers.map((trigger) => (
                  <button
                    className={[
                      "rounded-xl border p-3 text-left transition hover:bg-muted/50",
                      selectedTriggerId === trigger.id ? "border-primary bg-primary-soft" : "border-border bg-card/70"
                    ].join(" ")}
                    key={trigger.id}
                    onClick={() => void handleSelectTrigger(trigger.id)}
                    type="button"
                  >
                    <div className="flex items-start justify-between gap-3">
                      <div>
                        <p className="font-bold text-foreground">{trigger.name}</p>
                        <p className="mt-1 text-xs font-semibold text-muted-foreground">{formatTriggerEventLabel(trigger.eventName)}</p>
                      </div>
                      <Badge variant={trigger.isEnabled ? "success" : "default"}>{trigger.isEnabled ? "Enabled" : "Disabled"}</Badge>
                    </div>
                    <p className="mt-3 text-xs text-muted-foreground">
                      {trigger.conditionCount} conditions, {trigger.actionCount} actions
                    </p>
                    <p className="mt-1 text-xs text-muted-foreground">Updated {formatTriggerDate(trigger.updatedAt ?? trigger.createdAt)}</p>
                  </button>
                ))}
              </div>
            ) : (
              <EmptyState
                title="No triggers"
                description="Create the first automation for this form using the V4 event and action foundation."
                action={
                  <Button disabled={!selectedFormId} onClick={handleNewTrigger} variant="outline">
                    <Plus className="size-4" />
                    New trigger
                  </Button>
                }
              />
            )}
          </CardContent>
        </Card>

        <div className="grid gap-4">
          <Card>
            <CardHeader>
              <CardTitle>{selectedTrigger ? "Edit trigger" : "New trigger"}</CardTitle>
              <CardDescription>
                {formDetail ? `Automation for ${formDetail.name}.` : "Choose a form to start authoring triggers."}
              </CardDescription>
            </CardHeader>
            <CardContent className="grid gap-5">
              <div className="grid gap-4 lg:grid-cols-[minmax(0,1fr)_minmax(0,1fr)_11rem]">
                <Input error={errorMap.get("name")} label="Name" onChange={(event) => updateDraft({ name: event.target.value })} value={draft.name} />
                <Select label="Event" onChange={(event) => updateDraft({ eventName: event.target.value as TriggerEventName })} options={triggerEventOptions} value={draft.eventName} />
                <Checkbox
                  checked={draft.isEnabled}
                  label="Enabled"
                  onChange={(event) => updateDraft({ isEnabled: event.target.checked })}
                />
              </div>
              <Textarea label="Description" onChange={(event) => updateDraft({ description: event.target.value })} value={draft.description} />

              <div className="grid gap-3">
                <div className="flex flex-wrap items-center justify-between gap-3">
                  <div>
                    <h3 className="text-base font-bold text-foreground">Conditions</h3>
                    <p className="text-sm text-muted-foreground">All conditions must match. Leave empty to run for every matching event.</p>
                  </div>
                  <Button onClick={() => updateDraft({ conditions: [...draft.conditions, createTriggerConditionDraft("field_equals", Date.now())] })} size="sm" variant="outline">
                    <Plus className="size-4" />
                    Add condition
                  </Button>
                </div>

                {draft.conditions.length === 0 ? (
                  <div className="rounded-xl border border-dashed border-border bg-muted/30 p-4 text-sm text-muted-foreground">
                    No conditions. This trigger will run for every {formatTriggerEventLabel(draft.eventName).toLowerCase()} event.
                  </div>
                ) : (
                  <div className="grid gap-3">
                    {draft.conditions.map((condition, index) => (
                      <ConditionEditor
                        condition={condition}
                        departments={activeDepartments}
                        errors={errorMap}
                        fields={fieldOptions}
                        groups={activeGroups}
                        index={index}
                        key={condition.clientId}
                        onRemove={() => updateDraft({ conditions: draft.conditions.filter((item) => item.clientId !== condition.clientId) })}
                        onTypeChange={(type) => handleConditionTypeChange(condition, type)}
                        onUpdate={(patch) => updateCondition(condition.clientId, patch)}
                        users={activeUsers}
                      />
                    ))}
                  </div>
                )}
              </div>

              <div className="grid gap-3">
                <div className="flex flex-wrap items-center justify-between gap-3">
                  <div>
                    <h3 className="text-base font-bold text-foreground">Actions</h3>
                    <p className="text-sm text-muted-foreground">Actions run in saved order without recursive trigger dispatch.</p>
                  </div>
                  <Button onClick={() => updateDraft({ actions: [...draft.actions, createTriggerActionDraft("write_audit_entry", Date.now())] })} size="sm" variant="outline">
                    <Plus className="size-4" />
                    Add action
                  </Button>
                </div>
                {errorMap.get("actions") ? <p className="text-sm font-semibold text-danger">{errorMap.get("actions")}</p> : null}

                <div className="grid gap-3">
                  {draft.actions.map((action, index) => (
                    <ActionEditor
                      action={action}
                      errors={errorMap}
                      fields={fieldOptions}
                      groups={activeGroups}
                      index={index}
                      key={action.clientId}
                      onRemove={() => updateDraft({ actions: draft.actions.filter((item) => item.clientId !== action.clientId) })}
                      onTypeChange={(type) => handleActionTypeChange(action, type)}
                      onUpdate={(patch) => updateAction(action.clientId, patch)}
                      users={activeUsers}
                    />
                  ))}
                </div>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Execution logs</CardTitle>
              <CardDescription>Matching trigger executions, newest first.</CardDescription>
            </CardHeader>
            <CardContent>
              {loadingTrigger ? (
                <p className="text-sm font-semibold text-muted-foreground">Loading trigger detail...</p>
              ) : selectedTriggerId && logs.length > 0 ? (
                <div className="grid gap-3">
                  {logs.map((log) => (
                    <TriggerLogPanel
                      key={log.id}
                      log={log}
                      onRetry={(logId) => void handleRetryLog(logId)}
                      retrying={retryingLogId === log.id}
                    />
                  ))}
                </div>
              ) : selectedTriggerId ? (
                <EmptyState title="No logs yet" description="This trigger has no matching executions yet." action={null} />
              ) : (
                <EmptyState title="Select a trigger" description="Choose a saved trigger to review execution logs." action={null} />
              )}
            </CardContent>
          </Card>
        </div>
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

function ConditionEditor({
  condition,
  departments,
  errors,
  fields,
  groups,
  index,
  onRemove,
  onTypeChange,
  onUpdate,
  users
}: {
  condition: TriggerConditionDraft;
  departments: DepartmentDto[];
  errors: Map<string, string>;
  fields: TriggerFieldOption[];
  groups: GroupDto[];
  index: number;
  onRemove: () => void;
  onTypeChange: (type: TriggerConditionType) => void;
  onUpdate: (patch: Partial<TriggerConditionDraft>) => void;
  users: UserDto[];
}) {
  const path = `conditions[${index}]`;

  return (
    <div className="rounded-xl border border-border bg-card/70 p-4">
      <div className="grid gap-3 lg:grid-cols-[16rem_minmax(0,1fr)_2.5rem]">
        <Select label={`Condition ${index + 1}`} onChange={(event) => onTypeChange(event.target.value as TriggerConditionType)} options={triggerConditionOptions} value={condition.type} />
        <ConditionValueEditor condition={condition} departments={departments} errors={errors} fields={fields} groups={groups} onUpdate={onUpdate} path={path} users={users} />
        <Button aria-label="Remove condition" className="self-end" onClick={onRemove} size="icon" variant="ghost">
          <Trash2 className="size-4" />
        </Button>
      </div>
    </div>
  );
}

function ConditionValueEditor({
  condition,
  departments,
  errors,
  fields,
  groups,
  onUpdate,
  path,
  users
}: {
  condition: TriggerConditionDraft;
  departments: DepartmentDto[];
  errors: Map<string, string>;
  fields: TriggerFieldOption[];
  groups: GroupDto[];
  onUpdate: (patch: Partial<TriggerConditionDraft>) => void;
  path: string;
  users: UserDto[];
}) {
  if (condition.type === "field_equals") {
    const selectedField = fields.find((field) => field.id === condition.fieldId);

    return (
      <div className="grid gap-3 md:grid-cols-2">
        <FieldSelect error={errors.get(`${path}.fieldId`)} fields={fields} value={condition.fieldId ?? ""} onChange={(fieldId) => onUpdate({ fieldId })} />
        {selectedField?.options.length ? (
          <Select error={errors.get(`${path}.value`)} label="Value" onChange={(event) => onUpdate({ value: event.target.value })} value={String(condition.value ?? "")}>
            <option value="">Choose value</option>
            {selectedField.options.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </Select>
        ) : (
          <Input error={errors.get(`${path}.value`)} label="Value" onChange={(event) => onUpdate({ value: event.target.value })} value={String(condition.value ?? "")} />
        )}
      </div>
    );
  }

  if (condition.type === "field_changed") {
    return <FieldSelect error={errors.get(`${path}.fieldId`)} fields={fields} value={condition.fieldId ?? ""} onChange={(fieldId) => onUpdate({ fieldId })} />;
  }

  if (condition.type === "status_changed_to") {
    return <Input error={errors.get(`${path}.status`)} label="Status" onChange={(event) => onUpdate({ status: event.target.value })} placeholder="submitted" value={condition.status ?? ""} />;
  }

  if (condition.type === "department_equals") {
    return (
      <Select error={errors.get(`${path}.departmentId`)} label="Department" onChange={(event) => onUpdate({ departmentId: event.target.value })} value={condition.departmentId ?? ""}>
        <option value="">Choose department</option>
        {departments.map((department) => (
          <option key={department.id} value={department.id}>
            {department.name}
          </option>
        ))}
      </Select>
    );
  }

  if (condition.type === "assigned_to_user") {
    return (
      <Select error={errors.get(`${path}.userId`)} label="User" onChange={(event) => onUpdate({ userId: event.target.value })} value={condition.userId ?? ""}>
        <option value="">Choose user</option>
        {users.map((user) => (
          <option key={user.id} value={user.id}>
            {user.name}
          </option>
        ))}
      </Select>
    );
  }

  return (
    <Select error={errors.get(`${path}.groupId`)} label="Group" onChange={(event) => onUpdate({ groupId: event.target.value })} value={condition.groupId ?? ""}>
      <option value="">Choose group</option>
      {groups.map((group) => (
        <option key={group.id} value={group.id}>
          {group.name}
        </option>
      ))}
    </Select>
  );
}

function ActionEditor({
  action,
  errors,
  fields,
  groups,
  index,
  onRemove,
  onTypeChange,
  onUpdate,
  users
}: {
  action: TriggerActionDraft;
  errors: Map<string, string>;
  fields: TriggerFieldOption[];
  groups: GroupDto[];
  index: number;
  onRemove: () => void;
  onTypeChange: (type: TriggerActionType) => void;
  onUpdate: (patch: Partial<TriggerActionDraft>) => void;
  users: UserDto[];
}) {
  const path = `actions[${index}]`;

  return (
    <div className="rounded-xl border border-border bg-card/70 p-4">
      <div className="grid gap-3 lg:grid-cols-[16rem_minmax(0,1fr)_2.5rem]">
        <div className="grid gap-3">
          <Select label={`Action ${index + 1}`} onChange={(event) => onTypeChange(event.target.value as TriggerActionType)} options={triggerActionOptions} value={action.type} />
          <Input error={errors.get(`${path}.id`)} label="Action id" onChange={(event) => onUpdate({ id: event.target.value })} value={action.id} />
        </div>
        <ActionValueEditor action={action} errors={errors} fields={fields} groups={groups} onUpdate={onUpdate} path={path} users={users} />
        <Button aria-label="Remove action" className="self-end" onClick={onRemove} size="icon" variant="ghost">
          <Trash2 className="size-4" />
        </Button>
      </div>
    </div>
  );
}

function ActionValueEditor({
  action,
  errors,
  fields,
  groups,
  onUpdate,
  path,
  users
}: {
  action: TriggerActionDraft;
  errors: Map<string, string>;
  fields: TriggerFieldOption[];
  groups: GroupDto[];
  onUpdate: (patch: Partial<TriggerActionDraft>) => void;
  path: string;
  users: UserDto[];
}) {
  if (action.type === "write_audit_entry") {
    return <Textarea error={errors.get(`${path}.message`)} label="Audit message" onChange={(event) => onUpdate({ message: event.target.value })} value={action.message ?? ""} />;
  }

  if (action.type === "send_email") {
    return (
      <div className="grid gap-3">
        <Input error={errors.get(`${path}.to`)} label="Recipients" onChange={(event) => onUpdate({ toText: event.target.value })} placeholder="admin@example.com, ops@example.com" value={action.toText ?? ""} />
        <Input error={errors.get(`${path}.subject`)} label="Subject" onChange={(event) => onUpdate({ subject: event.target.value })} value={action.subject ?? ""} />
        <Textarea label="Body" onChange={(event) => onUpdate({ body: event.target.value })} value={action.body ?? ""} />
      </div>
    );
  }

  if (action.type === "change_status") {
    return <Input error={errors.get(`${path}.status`)} label="New status" onChange={(event) => onUpdate({ status: event.target.value })} placeholder="in_review" value={action.status ?? ""} />;
  }

  if (action.type === "update_field") {
    const selectedField = fields.find((field) => field.id === action.fieldId);

    return (
      <div className="grid gap-3 md:grid-cols-2">
        <FieldSelect error={errors.get(`${path}.fieldId`)} fields={fields} value={action.fieldId ?? ""} onChange={(fieldId) => onUpdate({ fieldId })} />
        {selectedField?.options.length ? (
          <Select error={errors.get(`${path}.value`)} label="New value" onChange={(event) => onUpdate({ value: event.target.value })} value={String(action.value ?? "")}>
            <option value="">Choose value</option>
            {selectedField.options.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </Select>
        ) : (
          <Input error={errors.get(`${path}.value`)} label="New value" onChange={(event) => onUpdate({ value: event.target.value })} value={String(action.value ?? "")} />
        )}
      </div>
    );
  }

  return (
    <div className="grid gap-3 md:grid-cols-2">
      <Select error={errors.get(`${path}.assignment`)} label="Assign user" onChange={(event) => onUpdate({ assignedToUserId: event.target.value, assignedGroupId: "" })} value={action.assignedToUserId ?? ""}>
        <option value="">No user</option>
        {users.map((user) => (
          <option key={user.id} value={user.id}>
            {user.name}
          </option>
        ))}
      </Select>
      <Select error={errors.get(`${path}.assignment`)} label="Assign group" onChange={(event) => onUpdate({ assignedGroupId: event.target.value, assignedToUserId: "" })} value={action.assignedGroupId ?? ""}>
        <option value="">No group</option>
        {groups.map((group) => (
          <option key={group.id} value={group.id}>
            {group.name}
          </option>
        ))}
      </Select>
    </div>
  );
}

function FieldSelect({ error, fields, onChange, value }: { error?: string; fields: TriggerFieldOption[]; onChange: (fieldId: string) => void; value: string }) {
  return (
    <Select error={error} label="Field" onChange={(event) => onChange(event.target.value)} value={value}>
      <option value="">Choose field</option>
      {fields.map((field) => (
        <option key={field.id} value={field.id}>
          {field.label}
        </option>
      ))}
    </Select>
  );
}

function TriggerLogPanel({ log, onRetry, retrying }: { log: TriggerExecutionLog; onRetry: (logId: string) => void; retrying: boolean }) {
  const status = formatTriggerLogStatus(log.status);

  return (
    <div className="rounded-xl border border-border bg-card/70 p-4">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="flex items-start gap-3">
          <span className="grid size-10 shrink-0 place-items-center rounded-xl bg-muted text-muted-foreground">
            {log.status === "success" ? <Activity className="size-5" /> : <ListChecks className="size-5" />}
          </span>
          <div>
            <div className="flex flex-wrap items-center gap-2">
              <p className="font-bold text-foreground">{formatTriggerEventLabel(log.eventName)}</p>
              <Badge variant={status.variant}>{status.label}</Badge>
            </div>
            <p className="mt-1 text-sm text-muted-foreground">
              {log.entityType} {log.entityId}
            </p>
          </div>
        </div>
        <div className="grid justify-items-end gap-2 text-right text-xs text-muted-foreground">
          <div>
            <p>Started {formatTriggerDate(log.startedAt)}</p>
            <p>Completed {formatTriggerDate(log.completedAt)}</p>
            {log.retryOfLogId ? <p>Retry of {log.retryOfLogId}</p> : null}
          </div>
          {log.status === "failed" ? (
            <Button disabled={retrying} onClick={() => onRetry(log.id)} size="sm" variant="outline">
              <RotateCcw className="size-4" />
              {retrying ? "Retrying..." : "Retry"}
            </Button>
          ) : null}
        </div>
      </div>
      {log.errorMessage ? <p className="mt-3 rounded-lg bg-danger-soft px-3 py-2 text-sm font-semibold text-danger">{log.errorMessage}</p> : null}
      <div className="mt-4 grid gap-3 lg:grid-cols-2">
        <JsonBlock label="Input" value={log.input} />
        <JsonBlock label="Result" value={log.result} />
      </div>
    </div>
  );
}

function JsonBlock({ label, value }: { label: string; value: unknown }) {
  return (
    <div>
      <p className="mb-2 text-xs font-bold uppercase text-muted-foreground">{label}</p>
      <pre className="max-h-64 overflow-auto rounded-xl border border-border bg-muted/40 p-3 text-xs leading-5 text-foreground">
        {formatTriggerJson(value)}
      </pre>
    </div>
  );
}

function createValidationErrorMap(errors: TriggerValidationError[]): Map<string, string> {
  return new Map(errors.map((error) => [error.path, error.message]));
}

function getValidationErrors(error: unknown): TriggerValidationError[] {
  return typeof error === "object" && error !== null && "errors" in error && Array.isArray(error.errors)
    ? error.errors.filter(isValidationError)
    : [];
}

function isValidationError(value: unknown): value is TriggerValidationError {
  return (
    typeof value === "object"
    && value !== null
    && "path" in value
    && typeof value.path === "string"
    && "code" in value
    && typeof value.code === "string"
    && "message" in value
    && typeof value.message === "string"
  );
}

function getErrorMessage(error: unknown): string {
  return error instanceof Error ? error.message : "Trigger request failed.";
}

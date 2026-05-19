import { type FormEvent, useEffect, useMemo, useState } from "react";
import { ArrowLeft, Plus, Save, Settings2, Trash2 } from "lucide-react";
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
import { Textarea } from "../../../components/ui/Textarea";
import { cn } from "../../../lib/cn";
import { listForms } from "../api";
import {
  addFieldToSchema,
  createEmptyFormBuilderSchema,
  deleteFieldFromSchema,
  fieldTypeDescriptions,
  fieldTypeLabels,
  getDefaultFieldValue,
  isChoiceFieldType,
  loadFormBuilderDraft,
  saveFormBuilderDraft,
  updateFieldInSchema
} from "../builder";
import { formFieldTypes, type FormField, type FormFieldOption, type FormFieldType, type FormSchema } from "../types";

const fieldTypeOptions = formFieldTypes.map((type) => ({ label: fieldTypeLabels[type], value: type }));

export function FormBuilderPage() {
  const { formId } = useParams<{ formId: string }>();
  const navigate = useNavigate();
  const resolvedFormId = formId ?? "unknown";
  const [schema, setSchema] = useState<FormSchema>(() =>
    formId ? loadFormBuilderDraft(formId) : createEmptyFormBuilderSchema()
  );
  const [selectedFieldId, setSelectedFieldId] = useState<string | null>(() => schema.fields[0]?.id ?? null);
  const [formName, setFormName] = useState("Form draft");
  const [loadingForm, setLoadingForm] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  useEffect(() => {
    if (!formId) return;

    setSchema(loadFormBuilderDraft(formId));
  }, [formId]);

  useEffect(() => {
    setSelectedFieldId((current) => (current && schema.fields.some((field) => field.id === current) ? current : schema.fields[0]?.id ?? null));
  }, [schema]);

  useEffect(() => {
    let active = true;
    setLoadingForm(true);
    setError(null);

    listForms()
      .then((forms) => {
        if (!active) return;
        const form = forms.find((candidate) => candidate.id === resolvedFormId);
        setFormName(form?.name ?? `Form ${resolvedFormId}`);
      })
      .catch((caught: unknown) => {
        if (!active) return;
        setFormName(`Form ${resolvedFormId}`);
        setError(getErrorMessage(caught));
      })
      .finally(() => {
        if (active) setLoadingForm(false);
      });

    return () => {
      active = false;
    };
  }, [resolvedFormId]);

  const selectedField = useMemo(
    () => schema.fields.find((field) => field.id === selectedFieldId) ?? null,
    [schema.fields, selectedFieldId]
  );

  function handleAddField(type: FormFieldType) {
    const result = addFieldToSchema(schema, type);
    setSchema(result.schema);
    setSelectedFieldId(result.field.id);
    setNotice(null);
  }

  function handleUpdateField(field: FormField) {
    setSchema((currentSchema) => updateFieldInSchema(currentSchema, field));
    setNotice(null);
  }

  function handleDeleteField() {
    if (!selectedField) return;

    setSchema((currentSchema) => deleteFieldFromSchema(currentSchema, selectedField.id));
    setNotice(null);
  }

  function handleSaveDraft() {
    saveFormBuilderDraft(resolvedFormId, schema);
    setNotice("Draft saved locally.");
  }

  return (
    <div className="grid gap-6">
      <PageHeader
        eyebrow="Form builder"
        title={loadingForm ? "Loading form..." : formName}
        description="Edit draft fields before layout, publishing, and record workflows are added."
        actions={
          <div className="flex flex-wrap gap-2">
            <Button onClick={() => navigate("/forms")} variant="outline">
              <ArrowLeft className="size-4" />
              Forms
            </Button>
            <Button onClick={handleSaveDraft}>
              <Save className="size-4" />
              Save draft
            </Button>
          </div>
        }
      />

      {error ? <Alert title="Form builder">{error}</Alert> : null}
      {notice ? (
        <div className="rounded-xl border border-success/40 bg-success/10 px-4 py-3 text-sm font-semibold text-success">
          {notice}
        </div>
      ) : null}

      <div className="grid gap-4 xl:grid-cols-[17rem_minmax(0,1fr)_24rem]">
        <FieldPalette onAddField={handleAddField} />
        <BuilderCanvas fields={schema.fields} selectedFieldId={selectedFieldId} onSelectField={setSelectedFieldId} />
        <FieldSettings field={selectedField} onChange={handleUpdateField} onDelete={handleDeleteField} />
      </div>
    </div>
  );
}

function FieldPalette({ onAddField }: { onAddField: (type: FormFieldType) => void }) {
  return (
    <Card className="self-start">
      <CardHeader>
        <CardTitle>Fields</CardTitle>
        <CardDescription>V1 field palette.</CardDescription>
      </CardHeader>
      <CardContent className="grid gap-2">
        {fieldTypeOptions.map((fieldType) => (
          <button
            className="flex min-h-12 items-center justify-between gap-3 rounded-xl border border-border bg-card/80 px-3 py-2 text-left transition hover:border-primary/50 hover:bg-muted"
            key={fieldType.value}
            type="button"
            onClick={() => onAddField(fieldType.value)}
          >
            <span>
              <span className="block text-sm font-bold text-foreground">{fieldType.label}</span>
              <span className="mt-0.5 block text-xs text-muted-foreground">{fieldTypeDescriptions[fieldType.value]}</span>
            </span>
            <Plus className="size-4 shrink-0 text-muted-foreground" />
          </button>
        ))}
      </CardContent>
    </Card>
  );
}

function BuilderCanvas({
  fields,
  selectedFieldId,
  onSelectField
}: {
  fields: FormField[];
  selectedFieldId: string | null;
  onSelectField: (fieldId: string) => void;
}) {
  return (
    <Card className="min-h-[36rem]">
      <CardHeader>
        <div className="flex items-start justify-between gap-3">
          <div>
            <CardTitle>Canvas</CardTitle>
            <CardDescription>Draft field order.</CardDescription>
          </div>
          <Badge>{fields.length} fields</Badge>
        </div>
      </CardHeader>
      <CardContent>
        {fields.length > 0 ? (
          <div className="grid gap-3">
            {fields.map((field) => (
              <button
                className={cn(
                  "w-full rounded-xl border bg-card/90 p-4 text-left transition",
                  selectedFieldId === field.id
                    ? "border-primary shadow-lifted ring-4 ring-primary/10"
                    : "border-border hover:border-primary/40 hover:bg-muted/50"
                )}
                key={field.id}
                type="button"
                onClick={() => onSelectField(field.id)}
              >
                <div className="mb-3 flex items-start justify-between gap-3">
                  <div>
                    <div className="flex flex-wrap items-center gap-2">
                      <p className="font-bold text-foreground">{field.label}</p>
                      {field.required ? <Badge variant="warning">Required</Badge> : null}
                    </div>
                    {field.helpText ? <p className="mt-1 text-sm text-muted-foreground">{field.helpText}</p> : null}
                  </div>
                  <Badge variant="default">{fieldTypeLabels[field.type]}</Badge>
                </div>
                <FieldPreview field={field} />
              </button>
            ))}
          </div>
        ) : (
          <EmptyState title="No fields" description="Add a field from the palette to start this draft." />
        )}
      </CardContent>
    </Card>
  );
}

function FieldPreview({ field }: { field: FormField }) {
  const controlClass = "min-h-10 w-full rounded-xl border border-border bg-muted/50 px-3 text-sm text-muted-foreground";

  if (field.type === "textarea") {
    return <textarea className={cn(controlClass, "min-h-24 py-3")} disabled placeholder={field.placeholder} />;
  }

  if (field.type === "select") {
    return (
      <select className={controlClass} disabled>
        {(field.options ?? []).map((option) => (
          <option key={option.id}>{option.label}</option>
        ))}
      </select>
    );
  }

  if (field.type === "radio") {
    return (
      <div className="grid gap-2">
        {(field.options ?? []).map((option) => (
          <label className="flex items-center gap-2 text-sm font-semibold text-muted-foreground" key={option.id}>
            <input disabled name={field.id} type="radio" />
            {option.label}
          </label>
        ))}
      </div>
    );
  }

  if (field.type === "checkbox") {
    return (
      <label className="flex items-center gap-2 text-sm font-semibold text-muted-foreground">
        <input checked={Boolean(getDefaultFieldValue(field))} disabled readOnly type="checkbox" />
        {field.placeholder || field.label}
      </label>
    );
  }

  return <input className={controlClass} disabled placeholder={field.placeholder} type={getInputType(field.type)} />;
}

function FieldSettings({
  field,
  onChange,
  onDelete
}: {
  field: FormField | null;
  onChange: (field: FormField) => void;
  onDelete: () => void;
}) {
  if (!field) {
    return (
      <Card className="self-start">
        <CardHeader>
          <CardTitle>Settings</CardTitle>
          <CardDescription>Select a field to edit its settings.</CardDescription>
        </CardHeader>
        <CardContent>
          <EmptyState title="No field selected" description="Add or select a field on the canvas." />
        </CardContent>
      </Card>
    );
  }

  function patchField(patch: Partial<FormField>) {
    if (!field) return;
    onChange({ ...field, ...patch });
  }

  return (
    <Card className="self-start">
      <CardHeader>
        <div className="flex items-start justify-between gap-3">
          <div>
            <CardTitle>Settings</CardTitle>
            <CardDescription>{fieldTypeLabels[field.type]} field</CardDescription>
          </div>
          <Settings2 className="size-5 text-muted-foreground" />
        </div>
      </CardHeader>
      <CardContent>
        <form className="grid gap-4" onSubmit={preventSubmit}>
          <Select
            label="Field type"
            onChange={(event) => patchField({ type: event.target.value as FormFieldType })}
            options={fieldTypeOptions}
            value={field.type}
          />
          <Input label="Label" onChange={(event) => patchField({ label: event.target.value })} value={field.label} />
          {field.type !== "checkbox" ? (
            <Input
              label="Placeholder"
              onChange={(event) => patchField({ placeholder: event.target.value })}
              value={field.placeholder ?? ""}
            />
          ) : null}
          <Textarea label="Help text" onChange={(event) => patchField({ helpText: event.target.value })} value={field.helpText ?? ""} />
          <Checkbox
            checked={Boolean(field.required)}
            label="Required"
            onChange={(event) => patchField({ required: event.target.checked })}
          />
          <DefaultValueSetting field={field} onChange={(defaultValue) => patchField({ defaultValue })} />
          {isChoiceFieldType(field.type) ? <OptionsEditor field={field} onChange={(options) => patchField({ options })} /> : null}
          <Button onClick={onDelete} variant="danger">
            <Trash2 className="size-4" />
            Delete field
          </Button>
        </form>
      </CardContent>
    </Card>
  );
}

function DefaultValueSetting({
  field,
  onChange
}: {
  field: FormField;
  onChange: (value: FormField["defaultValue"]) => void;
}) {
  if (field.type === "checkbox") {
    return (
      <Checkbox
        checked={Boolean(field.defaultValue)}
        label="Checked by default"
        onChange={(event) => onChange(event.target.checked)}
      />
    );
  }

  if (isChoiceFieldType(field.type)) {
    return (
      <Select
        label="Default value"
        onChange={(event) => onChange(event.target.value || undefined)}
        value={typeof field.defaultValue === "string" ? field.defaultValue : ""}
      >
        <option value="">No default</option>
        {(field.options ?? []).map((option) => (
          <option key={option.id} value={option.value}>
            {option.label}
          </option>
        ))}
      </Select>
    );
  }

  return (
    <Input
      label="Default value"
      onChange={(event) => onChange(event.target.value)}
      type={field.type === "number" ? "number" : getInputType(field.type)}
      value={field.defaultValue === undefined || typeof field.defaultValue === "boolean" ? "" : String(field.defaultValue)}
    />
  );
}

function OptionsEditor({ field, onChange }: { field: FormField; onChange: (options: FormFieldOption[]) => void }) {
  const options = field.options ?? [];

  function updateOption(index: number, patch: Partial<FormFieldOption>) {
    onChange(options.map((option, optionIndex) => (optionIndex === index ? { ...option, ...patch } : option)));
  }

  function addOption() {
    const nextIndex = options.length + 1;
    onChange([...options, { id: "", label: `Option ${nextIndex}`, value: `option_${nextIndex}` }]);
  }

  function removeOption(index: number) {
    onChange(options.filter((_, optionIndex) => optionIndex !== index));
  }

  return (
    <div className="grid gap-3 rounded-xl border border-border bg-muted/30 p-3">
      <div className="flex items-center justify-between gap-3">
        <p className="text-sm font-bold text-foreground">Options</p>
        <Button onClick={addOption} size="sm" variant="outline">
          <Plus className="size-4" />
          Add option
        </Button>
      </div>
      {options.map((option, index) => (
        <div className="grid gap-2 rounded-lg border border-border bg-card/80 p-3" key={`${option.id}-${index}`}>
          <Input label="Label" onChange={(event) => updateOption(index, { label: event.target.value })} value={option.label} />
          <Input label="Value" onChange={(event) => updateOption(index, { value: event.target.value })} value={option.value} />
          <Button disabled={options.length <= 1} onClick={() => removeOption(index)} size="sm" variant="ghost">
            <Trash2 className="size-4" />
            Remove
          </Button>
        </div>
      ))}
    </div>
  );
}

function getInputType(type: FormFieldType): string {
  if (type === "email") return "email";
  if (type === "number") return "number";
  if (type === "date") return "date";
  if (type === "phone") return "tel";
  return "text";
}

function preventSubmit(event: FormEvent<HTMLFormElement>) {
  event.preventDefault();
}

function getErrorMessage(error: unknown): string {
  return error instanceof Error ? error.message : "Form builder request failed.";
}

import { type FormEvent, useEffect, useMemo, useState } from "react";
import { ArrowLeft, Eye, Monitor, Plus, Save, Settings2, Smartphone, Tablet, Trash2 } from "lucide-react";
import { useNavigate, useParams } from "react-router-dom";
import { Alert } from "../../../components/ui/Alert";
import { Badge } from "../../../components/ui/Badge";
import { Button } from "../../../components/ui/Button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "../../../components/ui/Card";
import { Checkbox } from "../../../components/ui/Checkbox";
import { EmptyState } from "../../../components/ui/EmptyState";
import { Input } from "../../../components/ui/Input";
import { Modal } from "../../../components/ui/Modal";
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
  getFieldLayoutWidth,
  getDefaultFieldValue,
  isChoiceFieldType,
  layoutWidthOptions,
  loadFormBuilderDraft,
  saveFormBuilderDraft,
  updateFieldLayoutWidth,
  updateFieldInSchema
} from "../builder";
import type { LayoutWidthValue } from "../builder";
import { FormRenderer } from "../components/FormRenderer";
import { createInitialRecordValues, type FormPreviewSize } from "../renderer";
import {
  formFieldTypes,
  type FormField,
  type FormFieldOption,
  type FormFieldType,
  type FormLayoutColumn,
  type FormRecordValue,
  type FormRecordValues,
  type ValidationError,
  type FormSchema
} from "../types";
import { validateRecordValues } from "../validation";

const fieldTypeOptions = formFieldTypes.map((type) => ({ label: fieldTypeLabels[type], value: type }));
const layoutWidthSelectOptions = layoutWidthOptions.map(({ label, value }) => ({ label, value }));
const tabletSpanClasses: Record<number, string> = {
  1: "md:col-span-1",
  2: "md:col-span-2",
  3: "md:col-span-3",
  4: "md:col-span-4",
  5: "md:col-span-5",
  6: "md:col-span-6",
  7: "md:col-span-7",
  8: "md:col-span-8",
  9: "md:col-span-9",
  10: "md:col-span-10",
  11: "md:col-span-11",
  12: "md:col-span-12"
};
const desktopSpanClasses: Record<number, string> = {
  1: "xl:col-span-1",
  2: "xl:col-span-2",
  3: "xl:col-span-3",
  4: "xl:col-span-4",
  5: "xl:col-span-5",
  6: "xl:col-span-6",
  7: "xl:col-span-7",
  8: "xl:col-span-8",
  9: "xl:col-span-9",
  10: "xl:col-span-10",
  11: "xl:col-span-11",
  12: "xl:col-span-12"
};

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
  const [previewOpen, setPreviewOpen] = useState(false);
  const [previewSize, setPreviewSize] = useState<FormPreviewSize>("desktop");
  const [previewValues, setPreviewValues] = useState<FormRecordValues>(() => createInitialRecordValues(schema));
  const [previewErrors, setPreviewErrors] = useState<ValidationError[]>([]);
  const [previewNotice, setPreviewNotice] = useState<string | null>(null);

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
  const selectedFieldLayoutWidth = selectedField ? getFieldLayoutWidth(schema, selectedField.id) : null;

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

  function handleUpdateFieldLayoutWidth(fieldId: string, width: LayoutWidthValue) {
    setSchema((currentSchema) => updateFieldLayoutWidth(currentSchema, fieldId, width));
    setNotice(null);
  }

  function handleSaveDraft() {
    saveFormBuilderDraft(resolvedFormId, schema);
    setNotice("Draft saved locally.");
  }

  function handleOpenPreview() {
    setPreviewValues(createInitialRecordValues(schema));
    setPreviewErrors([]);
    setPreviewNotice(null);
    setPreviewOpen(true);
  }

  function handlePreviewValueChange(fieldId: string, value: FormRecordValue) {
    setPreviewValues((currentValues) => ({ ...currentValues, [fieldId]: value }));
    setPreviewErrors((currentErrors) => currentErrors.filter((validationError) => validationError.path !== `values.${fieldId}`));
    setPreviewNotice(null);
  }

  function handleValidatePreview() {
    const result = validateRecordValues(schema, previewValues);
    setPreviewErrors(result.errors);
    setPreviewNotice(result.valid ? "Preview values pass validation." : null);
  }

  return (
    <div className="grid gap-6">
      <PageHeader
        eyebrow="Form builder"
        title={loadingForm ? "Loading form..." : formName}
        description="Edit draft fields and responsive layout before preview, publishing, and records are added."
        actions={
          <div className="flex flex-wrap gap-2">
            <Button onClick={() => navigate("/forms")} variant="outline">
              <ArrowLeft className="size-4" />
              Forms
            </Button>
            <Button onClick={handleOpenPreview} variant="outline">
              <Eye className="size-4" />
              Preview
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
        <BuilderCanvas schema={schema} selectedFieldId={selectedFieldId} onSelectField={setSelectedFieldId} />
        <FieldSettings
          field={selectedField}
          layoutWidth={selectedFieldLayoutWidth}
          onChange={handleUpdateField}
          onChangeLayoutWidth={handleUpdateFieldLayoutWidth}
          onDelete={handleDeleteField}
        />
      </div>

      <Modal
        description="Render the current local draft with the shared V1 form renderer."
        onClose={() => setPreviewOpen(false)}
        open={previewOpen}
        panelClassName="max-h-[90vh] max-w-6xl overflow-hidden"
        title={`${formName} preview`}
      >
        <div className="grid max-h-[70vh] gap-4 overflow-y-auto pr-1">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <FormPreviewSizeSelector onChange={setPreviewSize} value={previewSize} />
            <Badge>{schema.fields.length} fields</Badge>
          </div>
          {previewNotice ? (
            <div className="rounded-xl border border-success/40 bg-success/10 px-4 py-3 text-sm font-semibold text-success">
              {previewNotice}
            </div>
          ) : previewErrors.length > 0 ? (
            <Alert title="Preview validation">Fix the highlighted fields before this form can be submitted.</Alert>
          ) : null}
          <div
            className={cn(
              "mx-auto w-full rounded-xl border border-border bg-background p-4 transition-all",
              previewSize === "mobile" ? "max-w-sm" : previewSize === "tablet" ? "max-w-3xl" : "max-w-none"
            )}
          >
            <FormRenderer
              errors={previewErrors}
              onChange={handlePreviewValueChange}
              onSubmit={handleValidatePreview}
              previewSize={previewSize}
              schema={schema}
              submitLabel="Validate preview"
              values={previewValues}
            />
          </div>
        </div>
      </Modal>
    </div>
  );
}

function FormPreviewSizeSelector({
  onChange,
  value
}: {
  onChange: (value: FormPreviewSize) => void;
  value: FormPreviewSize;
}) {
  const options: Array<{ icon: typeof Smartphone; label: string; value: FormPreviewSize }> = [
    { icon: Smartphone, label: "Mobile", value: "mobile" },
    { icon: Tablet, label: "Tablet", value: "tablet" },
    { icon: Monitor, label: "Desktop", value: "desktop" }
  ];

  return (
    <div className="flex flex-wrap gap-2" aria-label="Preview size">
      {options.map((option) => {
        const Icon = option.icon;

        return (
          <Button
            aria-pressed={value === option.value}
            key={option.value}
            onClick={() => onChange(option.value)}
            size="sm"
            variant={value === option.value ? "primary" : "outline"}
          >
            <Icon className="size-4" />
            {option.label}
          </Button>
        );
      })}
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
  schema,
  selectedFieldId,
  onSelectField
}: {
  schema: FormSchema;
  selectedFieldId: string | null;
  onSelectField: (fieldId: string) => void;
}) {
  const fieldsById = new Map(schema.fields.map((field) => [field.id, field]));

  return (
    <Card className="min-h-[36rem]">
      <CardHeader>
        <div className="flex items-start justify-between gap-3">
          <div>
            <CardTitle>Canvas</CardTitle>
            <CardDescription>Responsive 12-column layout.</CardDescription>
          </div>
          <Badge>{schema.fields.length} fields</Badge>
        </div>
      </CardHeader>
      <CardContent>
        {schema.fields.length > 0 ? (
          <div className="space-y-5">
            {schema.layout.pages.map((page) => (
              <div className="space-y-5" key={page.id}>
                {page.sections.map((section) => (
                  <section className="rounded-xl border border-border bg-muted/20 p-4" key={section.id}>
                    <div className="mb-4">
                      <h2 className="text-sm font-black uppercase tracking-normal text-foreground">{section.title ?? "Section"}</h2>
                      {section.description ? <p className="mt-1 text-sm text-muted-foreground">{section.description}</p> : null}
                    </div>
                    <div className="space-y-3">
                      {section.rows.map((row) => (
                        <div className="grid gap-3 md:grid-cols-12" key={row.id}>
                          {row.columns.map((column) => {
                            const columnFields = column.fields
                              .map((fieldId) => fieldsById.get(fieldId))
                              .filter((field): field is FormField => Boolean(field));

                            return (
                              <div className={cn("min-w-0", getColumnSpanClass(column))} key={column.id}>
                                <div className="grid gap-3">
                                  {columnFields.map((field) => (
                                    <FieldCanvasCard
                                      column={column}
                                      field={field}
                                      key={field.id}
                                      onSelectField={onSelectField}
                                      selected={selectedFieldId === field.id}
                                    />
                                  ))}
                                </div>
                              </div>
                            );
                          })}
                        </div>
                      ))}
                    </div>
                  </section>
                ))}
              </div>
            ))}
          </div>
        ) : (
          <EmptyState title="No fields" description="Add a field from the palette to start this draft." />
        )}
      </CardContent>
    </Card>
  );
}

function FieldCanvasCard({
  column,
  field,
  onSelectField,
  selected
}: {
  column: FormLayoutColumn;
  field: FormField;
  onSelectField: (fieldId: string) => void;
  selected: boolean;
}) {
  return (
    <button
      className={cn(
        "w-full rounded-xl border bg-card/90 p-4 text-left transition",
        selected ? "border-primary shadow-lifted ring-4 ring-primary/10" : "border-border hover:border-primary/40 hover:bg-muted/50"
      )}
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
        <div className="flex shrink-0 flex-wrap justify-end gap-2">
          <Badge variant="default">{fieldTypeLabels[field.type]}</Badge>
          <Badge>{getLayoutWidthLabel(column)}</Badge>
        </div>
      </div>
      <FieldPreview field={field} />
    </button>
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
  layoutWidth,
  onChange,
  onChangeLayoutWidth,
  onDelete
}: {
  field: FormField | null;
  layoutWidth: LayoutWidthValue | null;
  onChange: (field: FormField) => void;
  onChangeLayoutWidth: (fieldId: string, width: LayoutWidthValue) => void;
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
          <Select
            help="Mobile stays full width. Tablet and desktop use this width."
            label="Width"
            onChange={(event) => onChangeLayoutWidth(field.id, event.target.value as LayoutWidthValue)}
            options={layoutWidthSelectOptions}
            value={layoutWidth ?? "full"}
          />
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

function getColumnSpanClass(column: FormLayoutColumn): string {
  return cn(
    tabletSpanClasses[column.span.tablet] ?? tabletSpanClasses[12],
    desktopSpanClasses[column.span.desktop] ?? desktopSpanClasses[12]
  );
}

function getLayoutWidthLabel(column: FormLayoutColumn): string {
  return layoutWidthOptions.find((option) => option.span.desktop === column.span.desktop)?.label ?? "Custom width";
}

function preventSubmit(event: FormEvent<HTMLFormElement>) {
  event.preventDefault();
}

function getErrorMessage(error: unknown): string {
  return error instanceof Error ? error.message : "Form builder request failed.";
}

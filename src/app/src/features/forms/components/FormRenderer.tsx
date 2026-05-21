import type { FormEvent, ReactNode } from "react";
import { Badge } from "../../../components/ui/Badge";
import { Button } from "../../../components/ui/Button";
import { Checkbox } from "../../../components/ui/Checkbox";
import { EmptyState } from "../../../components/ui/EmptyState";
import { Input } from "../../../components/ui/Input";
import { Select } from "../../../components/ui/Select";
import { Textarea } from "../../../components/ui/Textarea";
import { cn } from "../../../lib/cn";
import {
  coerceFieldInputValue,
  getColumnSpanClass,
  getFieldErrorsById,
  getLayoutFields,
  type FormPreviewSize
} from "../renderer";
import type { FormField, FormRecordValue, FormRecordValues, FormSchema, ValidationError } from "../types";

type FormRendererMode = "entry" | "readonly";

export type FormRendererProps = {
  schema: FormSchema;
  values: FormRecordValues;
  errors?: ValidationError[];
  mode?: FormRendererMode;
  previewSize?: FormPreviewSize;
  submitLabel?: string;
  onChange?: (fieldId: string, value: FormRecordValue) => void;
  onSubmit?: () => void;
};

export function FormRenderer({
  schema,
  values,
  errors = [],
  mode = "entry",
  previewSize = "responsive",
  submitLabel = "Submit",
  onChange,
  onSubmit
}: FormRendererProps) {
  const readonly = mode === "readonly";
  const fieldsById = new Map(schema.fields.map((field) => [field.id, field]));
  const errorsById = getFieldErrorsById(errors);

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    onSubmit?.();
  }

  function handleFieldChange(field: FormField, value: FormRecordValue | string | boolean) {
    if (readonly) {
      return;
    }

    onChange?.(field.id, coerceFieldInputValue(field, value));
  }

  if (schema.fields.length === 0) {
    return (
      <EmptyState
        action={<span className="sr-only">No form fields to preview.</span>}
        title="No fields to preview"
        description="Add fields to this draft before previewing the form."
      />
    );
  }

  return (
    <form className="grid gap-6" noValidate onSubmit={handleSubmit}>
      {schema.layout.pages.map((page) => (
        <div className="grid gap-5" key={page.id}>
          {page.title || page.description ? (
            <header>
              {page.title ? <h2 className="text-xl font-black text-foreground">{page.title}</h2> : null}
              {page.description ? <p className="mt-1 text-sm leading-6 text-muted-foreground">{page.description}</p> : null}
            </header>
          ) : null}

          {page.sections.map((section) => (
            <section className="grid gap-4 rounded-xl border border-border bg-card/80 p-4" key={section.id}>
              {section.title || section.description ? (
                <div>
                  {section.title ? <h3 className="text-sm font-black uppercase tracking-normal text-foreground">{section.title}</h3> : null}
                  {section.description ? <p className="mt-1 text-sm leading-6 text-muted-foreground">{section.description}</p> : null}
                </div>
              ) : null}

              <div className="grid gap-4">
                {section.rows.map((row) => (
                  <div className="grid grid-cols-12 gap-4" key={row.id}>
                    {row.columns.map((column) => (
                      <div className={cn("min-w-0", getColumnSpanClass(column, previewSize))} key={column.id}>
                        <div className="grid gap-4">
                          {getLayoutFields(column, fieldsById).map((field) => (
                            <RenderedField
                              disabled={readonly}
                              errors={errorsById[field.id] ?? []}
                              field={field}
                              key={field.id}
                              onChange={(value) => handleFieldChange(field, value)}
                              value={values[field.id]}
                            />
                          ))}
                        </div>
                      </div>
                    ))}
                  </div>
                ))}
              </div>
            </section>
          ))}
        </div>
      ))}

      {onSubmit && !readonly ? (
        <div className="flex justify-end">
          <Button type="submit">{submitLabel}</Button>
        </div>
      ) : null}
    </form>
  );
}

function RenderedField({
  disabled,
  errors,
  field,
  onChange,
  value
}: {
  disabled: boolean;
  errors: string[];
  field: FormField;
  onChange: (value: FormRecordValue | string | boolean) => void;
  value: FormRecordValue | undefined;
}) {
  const label = (
    <span className="flex flex-wrap items-center gap-2">
      <span>{field.label}</span>
      {field.required ? <Badge variant="warning">Required</Badge> : null}
    </span>
  );
  const error = errors[0];

  if (field.type === "textarea") {
    return (
      <Textarea
        disabled={disabled}
        error={error}
        help={field.helpText}
        label={getFieldLabel(field)}
        onChange={(event) => onChange(event.target.value)}
        placeholder={field.placeholder}
        required={field.required}
        value={getStringValue(value)}
      />
    );
  }

  if (field.type === "select") {
    return (
      <Select
        disabled={disabled}
        error={error}
        help={field.helpText}
        label={getFieldLabel(field)}
        onChange={(event) => onChange(event.target.value)}
        required={field.required}
        value={getStringValue(value)}
      >
        <option value="">Select an option</option>
        {(field.options ?? []).map((option) => (
          <option key={option.id} value={option.value}>
            {option.label}
          </option>
        ))}
      </Select>
    );
  }

  if (field.type === "checkbox") {
    return (
      <FieldShell errors={errors} helpText={field.helpText}>
        <Checkbox
          checked={Boolean(value)}
          className={error ? "border-danger" : undefined}
          description={field.placeholder}
          disabled={disabled}
          label={getFieldLabel(field)}
          onChange={(event) => onChange(event.target.checked)}
          required={field.required}
        />
      </FieldShell>
    );
  }

  if (field.type === "radio") {
    return (
      <fieldset className="grid gap-2">
      <legend className="text-sm font-bold text-foreground">{label}</legend>
        {field.helpText ? <p className="text-xs text-muted-foreground">{field.helpText}</p> : null}
        <div className="grid gap-2">
          {(field.options ?? []).map((option) => (
            <label
              className={cn(
                "flex cursor-pointer items-start gap-3 rounded-xl border bg-card/70 p-3 transition hover:bg-muted/50",
                error ? "border-danger" : "border-border"
              )}
              key={option.id}
            >
              <input
                checked={value === option.value}
                className="mt-1 size-4 border-border text-primary"
                disabled={disabled}
                name={field.id}
                onChange={() => onChange(option.value)}
                required={field.required}
                type="radio"
                value={option.value}
              />
              <span className="block text-sm font-bold text-foreground">{option.label}</span>
            </label>
          ))}
        </div>
        {error ? <p className="text-xs font-semibold text-danger">{error}</p> : null}
      </fieldset>
    );
  }

  return (
    <Input
      disabled={disabled}
      error={error}
      help={field.helpText}
      label={getFieldLabel(field)}
      onChange={(event) => onChange(event.target.value)}
      placeholder={field.placeholder}
      required={field.required}
      type={getInputType(field.type)}
      value={getStringValue(value)}
    />
  );
}

function FieldShell({ children, errors, helpText }: { children: ReactNode; errors: string[]; helpText?: string }) {
  return (
    <div className="grid gap-1.5">
      {children}
      {errors[0] ? <p className="text-xs font-semibold text-danger">{errors[0]}</p> : null}
      {!errors[0] && helpText ? <p className="text-xs text-muted-foreground">{helpText}</p> : null}
    </div>
  );
}

function getStringValue(value: FormRecordValue | undefined): string {
  if (value === undefined || value === null || typeof value === "boolean") {
    return "";
  }

  return String(value);
}

function getFieldLabel(field: FormField): string {
  return field.required ? `${field.label} *` : field.label;
}

function getInputType(type: FormField["type"]): string {
  if (type === "email") return "email";
  if (type === "number") return "number";
  if (type === "date") return "date";
  if (type === "phone") return "tel";
  return "text";
}

import { type FormEvent, type ReactNode, useEffect, useMemo, useState } from "react";
import { Search, X } from "lucide-react";
import { Badge } from "../../../components/ui/Badge";
import { Button } from "../../../components/ui/Button";
import { Checkbox } from "../../../components/ui/Checkbox";
import { EmptyState } from "../../../components/ui/EmptyState";
import { Input } from "../../../components/ui/Input";
import { Select } from "../../../components/ui/Select";
import { Textarea } from "../../../components/ui/Textarea";
import { cn } from "../../../lib/cn";
import { listDirectoryDepartments, listDirectoryUsers, type DirectoryOption } from "../directoryApi";
import {
  coerceFieldInputValue,
  getColumnSpanClass,
  getFieldErrorsById,
  getLayoutFields,
  getRenderableRows,
  type FormPreviewSize
} from "../renderer";
import { listLookupOptions, listSubTableRows, type RecordLookupOption, type SubTableRowsResult } from "../api";
import type { FormField, FormRecordValue, FormRecordValues, FormSchema, ValidationError } from "../types";

type FormRendererMode = "entry" | "readonly";

export type FormRendererProps = {
  formId?: string;
  recordId?: string;
  schema: FormSchema;
  values: FormRecordValues;
  errors?: ValidationError[];
  lookupDisplayValues?: Record<string, string>;
  mode?: FormRendererMode;
  previewSize?: FormPreviewSize;
  submitLabel?: string;
  onChange?: (fieldId: string, value: FormRecordValue) => void;
  onSubmit?: () => void;
};

export function FormRenderer({
  formId,
  recordId,
  schema,
  values,
  errors = [],
  lookupDisplayValues,
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
                {getRenderableRows(section).map((row) => (
                  <div className="grid grid-cols-12 gap-4" key={row.id}>
                    {row.columns.map((column) => (
                      <div className={cn("min-w-0", getColumnSpanClass(column, previewSize))} key={column.id}>
                        <div className="grid gap-4">
                          {getLayoutFields(column, fieldsById).map((field) => (
                            <RenderedField
                              disabled={readonly}
                              errors={errorsById[field.id] ?? []}
                              field={field}
                              formId={formId}
                              recordId={recordId}
                              dependencies={values}
                              displayValue={lookupDisplayValues?.[field.id]}
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
  dependencies,
  displayValue,
  errors,
  field,
  formId,
  recordId,
  onChange,
  value
}: {
  disabled: boolean;
  dependencies: FormRecordValues;
  displayValue?: string;
  errors: string[];
  field: FormField;
  formId?: string;
  recordId?: string;
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

  if (field.type === "subTable") {
    return <SubTablePreviewField errors={errors} field={field} recordId={recordId} />;
  }

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

  if (field.type === "recordLookup") {
    return (
      <RecordLookupField
        disabled={disabled}
        error={error}
        field={field}
        formId={formId}
        dependencies={dependencies}
        displayValue={displayValue}
        onChange={onChange}
        value={getStringValue(value)}
      />
    );
  }

  if (field.type === "userPicker" || field.type === "departmentPicker") {
    return (
      <DirectoryPickerField
        disabled={disabled}
        error={error}
        field={field}
        onChange={onChange}
        value={getStringValue(value)}
      />
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

export function SubTablePreviewField({ errors, field, recordId }: { errors: string[]; field: FormField; recordId?: string }) {
  const displayColumnFieldIds = field.subTable?.displayColumnFieldIds ?? [];
  const [rows, setRows] = useState<SubTableRowsResult | null>(null);
  const [loading, setLoading] = useState(false);
  const [subTableError, setSubTableError] = useState<string | null>(null);
  const subTableReady = Boolean(recordId && field.subTable?.childFormId && field.subTable?.parentLookupFieldId);
  const columns = rows?.columns.length
    ? rows.columns
    : displayColumnFieldIds.map((fieldId) => ({ fieldId, label: fieldId, type: "text" }));

  useEffect(() => {
    if (!recordId || !subTableReady) {
      setRows(null);
      setLoading(false);
      setSubTableError(null);
      return;
    }

    let cancelled = false;
    setLoading(true);
    setSubTableError(null);

    listSubTableRows(recordId, field.id)
      .then((result) => {
        if (!cancelled) {
          setRows(result);
        }
      })
      .catch((caught) => {
        if (!cancelled) {
          setRows(null);
          setSubTableError(caught instanceof Error ? caught.message : "Child records could not be loaded.");
        }
      })
      .finally(() => {
        if (!cancelled) {
          setLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [field.id, recordId, subTableReady]);

  return (
    <FieldShell errors={errors} helpText={field.helpText}>
      <div className="overflow-hidden rounded-xl border border-border bg-card/80">
        <div className="flex flex-wrap items-center justify-between gap-3 border-b border-border px-4 py-3">
          <div>
            <p className="text-sm font-bold text-foreground">{field.label}</p>
            <p className="mt-1 text-xs leading-5 text-muted-foreground">Related child records are shown from the configured child form.</p>
          </div>
          <Badge variant="default">{rows ? formatRowCount(rows.totalCount) : "Read-only"}</Badge>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full min-w-96 text-left text-sm">
            <thead className="bg-muted/60 text-xs font-bold uppercase tracking-normal text-muted-foreground">
              <tr>
                {(columns.length > 0 ? columns : [{ fieldId: "empty", label: "Column", type: "text" }]).map((column) => (
                  <th className="px-4 py-3" key={column.fieldId}>
                    {column.label}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {!subTableReady ? (
                <tr>
                  <td className="px-4 py-4 text-sm font-semibold text-muted-foreground" colSpan={Math.max(columns.length, 1)}>
                    {recordId ? "Configure the child form, parent lookup field, and display columns before showing rows." : "Child record rows will appear here after the parent record is opened."}
                  </td>
                </tr>
              ) : loading ? (
                <tr>
                  <td className="px-4 py-4 text-sm font-semibold text-muted-foreground" colSpan={Math.max(columns.length, 1)}>
                    Loading child records...
                  </td>
                </tr>
              ) : subTableError ? (
                <tr>
                  <td className="px-4 py-4 text-sm font-semibold text-danger" colSpan={Math.max(columns.length, 1)}>
                    {subTableError}
                  </td>
                </tr>
              ) : rows && rows.items.length > 0 && columns.length > 0 ? (
                rows.items.map((row) => (
                  <tr className="border-t border-border" key={row.recordId}>
                    {columns.map((column) => (
                      <td className="px-4 py-3 text-sm text-foreground" key={column.fieldId}>
                        {formatTableValue(row.displayValues?.[column.fieldId] ?? row.values[column.fieldId])}
                      </td>
                    ))}
                  </tr>
                ))
              ) : rows && columns.length === 0 ? (
                <tr>
                  <td className="px-4 py-4 text-sm font-semibold text-muted-foreground" colSpan={1}>
                    No visible columns configured.
                  </td>
                </tr>
              ) : (
                <tr>
                  <td className="px-4 py-4 text-sm font-semibold text-muted-foreground" colSpan={Math.max(columns.length, 1)}>
                    No child records found.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
    </FieldShell>
  );
}

function DirectoryPickerField({
  disabled,
  error,
  field,
  onChange,
  value
}: {
  disabled: boolean;
  error?: string;
  field: FormField;
  onChange: (value: string) => void;
  value: string;
}) {
  const [options, setOptions] = useState<DirectoryOption[]>([]);
  const [loading, setLoading] = useState(false);
  const [directoryError, setDirectoryError] = useState<string | null>(null);
  const isUserPicker = field.type === "userPicker";
  const placeholder = isUserPicker ? "Select a user" : "Select a department";

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setDirectoryError(null);

    const request = isUserPicker ? listDirectoryUsers() : listDirectoryDepartments();
    request
      .then((items) => {
        if (!cancelled) {
          setOptions(items);
        }
      })
      .catch((caught) => {
        if (!cancelled) {
          setOptions([]);
          setDirectoryError(caught instanceof Error ? caught.message : "Directory options could not be loaded.");
        }
      })
      .finally(() => {
        if (!cancelled) {
          setLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [isUserPicker]);

  const selectedFallback = value && !options.some((option) => option.id === value)
    ? [{ id: value, label: value, description: null }]
    : [];

  return (
    <Select
      disabled={disabled || loading}
      error={error}
      help={directoryError ?? (loading ? "Loading directory..." : field.helpText)}
      label={getFieldLabel(field)}
      onChange={(event) => onChange(event.target.value)}
      required={field.required}
      value={value}
    >
      <option value="">{placeholder}</option>
      {[...selectedFallback, ...options].map((option) => (
        <option key={option.id} value={option.id}>
          {option.description ? `${option.label} (${option.description})` : option.label}
        </option>
      ))}
    </Select>
  );
}

function RecordLookupField({
  disabled,
  dependencies,
  displayValue,
  error,
  field,
  formId,
  onChange,
  value
}: {
  disabled: boolean;
  dependencies: FormRecordValues;
  displayValue?: string;
  error?: string;
  field: FormField;
  formId?: string;
  onChange: (value: string) => void;
  value: string;
}) {
  const [search, setSearch] = useState(displayValue ?? value);
  const [options, setOptions] = useState<RecordLookupOption[]>([]);
  const [loading, setLoading] = useState(false);
  const [lookupError, setLookupError] = useState<string | null>(null);
  const [selectedLabel, setSelectedLabel] = useState<string | null>(displayValue ?? null);
  const sourceFormId = field.lookup?.sourceFormId ?? "";
  const lookupReady = Boolean(
    formId &&
      sourceFormId &&
      field.lookup?.labelFieldIds.length &&
      field.lookup?.searchFieldIds.length
  );
  const selectedOption = useMemo(
    () => options.find((option) => option.recordId === value),
    [options, value]
  );

  useEffect(() => {
    if (selectedOption) {
      setSelectedLabel(selectedOption.label);
    }
  }, [selectedOption]);

  useEffect(() => {
    if (displayValue) {
      setSelectedLabel(displayValue);
      setSearch(displayValue);
    }
  }, [displayValue]);

  useEffect(() => {
    if (!value) {
      setSelectedLabel(null);
      return;
    }

    if (!search && !selectedLabel) {
      setSearch(value);
    }
  }, [search, selectedLabel, value]);

  useEffect(() => {
    if (disabled || !lookupReady || !formId) {
      setOptions([]);
      setLoading(false);
      setLookupError(null);
      return;
    }

    let cancelled = false;
    setLoading(true);
    setLookupError(null);

    listLookupOptions(formId, field.id, { search, dependencies })
      .then((result) => {
        if (!cancelled) {
          setOptions(result.items);
        }
      })
      .catch((caught) => {
        if (!cancelled) {
          setOptions([]);
          setLookupError(caught instanceof Error ? caught.message : "Lookup options could not be loaded.");
        }
      })
      .finally(() => {
        if (!cancelled) {
          setLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [dependencies, disabled, field.id, formId, lookupReady, search, sourceFormId]);

  function handleSearchChange(nextSearch: string) {
    setSearch(nextSearch);

    if (value && nextSearch !== (selectedLabel ?? value)) {
      onChange("");
      setSelectedLabel(null);
    }
  }

  function selectOption(option: RecordLookupOption) {
    onChange(option.recordId);
    setSearch(option.label);
    setSelectedLabel(option.label);
  }

  function clearSelection() {
    onChange("");
    setSearch("");
    setSelectedLabel(null);
  }

  return (
    <div className="grid gap-2">
      <label className="block">
        <span className="mb-2 block text-sm font-bold text-foreground">{getFieldLabel(field)}</span>
        <span className="relative block">
          <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
          <input
            aria-label="Search lookup records"
            className={cn(
              "h-10 w-full rounded-xl border bg-card/90 px-3 pl-10 pr-11 text-sm text-foreground outline-none transition placeholder:text-muted-foreground/70 focus:ring-4 focus:ring-primary/20 disabled:cursor-not-allowed disabled:opacity-60",
              error ? "border-danger" : "border-border"
            )}
            disabled={disabled || !lookupReady}
            onChange={(event) => handleSearchChange(event.target.value)}
            onKeyDown={(event) => {
              if (event.key === "Enter") {
                event.preventDefault();
              }
            }}
            placeholder={field.placeholder ?? "Search records"}
            required={field.required}
            type="search"
            value={search}
          />
          {value && !disabled ? (
            <Button
              aria-label={`Clear ${field.label}`}
              className="absolute right-1 top-1/2 size-8 -translate-y-1/2"
              onClick={clearSelection}
              size="icon"
              variant="ghost"
            >
              <X className="size-4" />
            </Button>
          ) : null}
        </span>
      </label>
      {error ? <p className="text-xs font-semibold text-danger">{error}</p> : null}
      {!error && field.helpText ? <p className="text-xs text-muted-foreground">{field.helpText}</p> : null}
      {!lookupReady ? (
        <p className="text-xs font-semibold text-muted-foreground">Configure the lookup source before selecting records.</p>
      ) : null}
      {selectedLabel ? (
        <p className="text-xs font-semibold text-muted-foreground">Selected: {selectedLabel}</p>
      ) : value ? (
        <p className="text-xs font-semibold text-muted-foreground">Selected record: {value}</p>
      ) : null}
      {lookupReady && !disabled ? (
        <div className="grid gap-2 rounded-xl border border-border bg-card/70 p-2" role="listbox">
          {loading ? <p className="px-2 py-1 text-xs font-semibold text-muted-foreground">Loading records...</p> : null}
          {lookupError ? <p className="px-2 py-1 text-xs font-semibold text-danger">{lookupError}</p> : null}
          {!loading && !lookupError && options.length === 0 ? (
            <p className="px-2 py-1 text-xs font-semibold text-muted-foreground">No matching records.</p>
          ) : null}
          {options.map((option) => (
            <button
              className={cn(
                "rounded-lg border px-3 py-2 text-left transition hover:bg-muted/60",
                option.recordId === value ? "border-primary bg-primary/10" : "border-border bg-background"
              )}
              key={option.recordId}
              aria-selected={option.recordId === value}
              onClick={() => selectOption(option)}
              role="option"
              type="button"
            >
              <span className="flex flex-wrap items-center gap-2 text-sm font-bold text-foreground">
                {option.label}
                {option.recordId === value ? <Badge>Selected</Badge> : null}
              </span>
              {option.description ? <span className="mt-1 block text-xs text-muted-foreground">{option.description}</span> : null}
            </button>
          ))}
        </div>
      ) : null}
    </div>
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

function formatTableValue(value: FormRecordValue | string | undefined): string {
  if (value === undefined || value === null || value === "") {
    return "Empty";
  }

  if (typeof value === "boolean") {
    return value ? "Yes" : "No";
  }

  return String(value);
}

function formatRowCount(count: number): string {
  return `${count} ${count === 1 ? "row" : "rows"}`;
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

import type { FormField, FormLayoutColumn, FormRecordValue, FormRecordValues, FormSchema, ValidationError } from "./types";

export type FormPreviewSize = "responsive" | "mobile" | "tablet" | "desktop";

const columnSpanClasses: Record<number, string> = {
  1: "col-span-1",
  2: "col-span-2",
  3: "col-span-3",
  4: "col-span-4",
  5: "col-span-5",
  6: "col-span-6",
  7: "col-span-7",
  8: "col-span-8",
  9: "col-span-9",
  10: "col-span-10",
  11: "col-span-11",
  12: "col-span-12"
};

const tabletColumnSpanClasses: Record<number, string> = {
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

const desktopColumnSpanClasses: Record<number, string> = {
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

export function createInitialRecordValues(schema: FormSchema): FormRecordValues {
  return Object.fromEntries(schema.fields.map((field) => [field.id, normalizeInitialFieldValue(field)]));
}

export function coerceFieldInputValue(field: FormField, value: FormRecordValue | string | boolean): FormRecordValue {
  if (field.type === "checkbox") {
    return Boolean(value);
  }

  if (value === "") {
    return null;
  }

  if (field.type === "number") {
    const numericValue = typeof value === "number" ? value : Number(value);
    return Number.isFinite(numericValue) ? numericValue : null;
  }

  if (typeof value === "boolean") {
    return value;
  }

  return String(value);
}

export function getFieldErrorsById(errors: ValidationError[] = []): Record<string, string[]> {
  return errors.reduce<Record<string, string[]>>((result, validationError) => {
    const match = /^values\.([^.[\]]+)$/.exec(validationError.path);

    if (!match) {
      return result;
    }

    result[match[1]] = [...(result[match[1]] ?? []), validationError.message];
    return result;
  }, {});
}

export function getLayoutFields(column: FormLayoutColumn, fieldsById: Map<string, FormField>): FormField[] {
  return column.fields.map((fieldId) => fieldsById.get(fieldId)).filter((field): field is FormField => Boolean(field));
}

export function getColumnSpanClass(column: FormLayoutColumn, previewSize: FormPreviewSize = "responsive"): string {
  if (previewSize === "mobile") {
    return columnSpanClasses[normalizeSpan(column.span.mobile)];
  }

  if (previewSize === "tablet") {
    return columnSpanClasses[normalizeSpan(column.span.tablet)];
  }

  if (previewSize === "desktop") {
    return columnSpanClasses[normalizeSpan(column.span.desktop)];
  }

  return `${columnSpanClasses[normalizeSpan(column.span.mobile)]} ${tabletColumnSpanClasses[normalizeSpan(column.span.tablet)]} ${
    desktopColumnSpanClasses[normalizeSpan(column.span.desktop)]
  }`;
}

function normalizeInitialFieldValue(field: FormField): FormRecordValue {
  if (field.defaultValue !== undefined) {
    return coerceFieldInputValue(field, field.defaultValue);
  }

  if (field.type === "checkbox") {
    return false;
  }

  return "";
}

function normalizeSpan(span: number): number {
  return Number.isInteger(span) && span >= 1 && span <= 12 ? span : 12;
}

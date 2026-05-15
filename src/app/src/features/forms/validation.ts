import {
  type FormField,
  type FormLayout,
  type FormRecordValue,
  type FormRecordValues,
  type FormSchema,
  type ResponsiveSpan,
  type ValidationError,
  type ValidationResult,
  isFormFieldType
} from "./types";

const choiceFieldTypes = new Set(["select", "radio"]);
const textFieldTypes = new Set(["text", "textarea", "phone"]);
const breakpoints = ["mobile", "tablet", "desktop"] as const;

export function validateFormSchema(schema: FormSchema): ValidationResult {
  const errors: ValidationError[] = [];
  const candidate = schema as Partial<FormSchema> | undefined;
  const fields = Array.isArray(candidate?.fields) ? candidate.fields : [];
  const fieldIds = new Set<string>();

  if (!candidate) {
    errors.push(error("", "schema.required", "Form schema is required."));
    return result(errors);
  }

  if (candidate.schemaVersion !== 1) {
    errors.push(error("schemaVersion", "schema.version", "Schema version must be 1."));
  }

  if (!Array.isArray(candidate.fields) || candidate.fields.length === 0) {
    errors.push(error("fields", "fields.required", "At least one field is required."));
  }

  fields.forEach((field, index) => {
    validateField(field, index, fieldIds, errors);
  });

  validateLayout(candidate.layout, fieldIds, errors);

  return result(errors);
}

export function validateRecordValues(schema: FormSchema, values: FormRecordValues): ValidationResult {
  const errors: ValidationError[] = [];
  const fieldsById = new Map(schema.fields.map((field) => [field.id, field]));
  const valueKeys = new Set(Object.keys(values));

  for (const valueKey of valueKeys) {
    if (!fieldsById.has(valueKey)) {
      errors.push(error(`values.${valueKey}`, "record.field_unknown", `Record contains unknown field '${valueKey}'.`));
    }
  }

  for (const field of schema.fields) {
    const hasValue = Object.prototype.hasOwnProperty.call(values, field.id);
    const value = values[field.id];

    if (field.required && (!hasValue || isEmptyValue(value))) {
      errors.push(error(`values.${field.id}`, "record.required", `'${field.label}' is required.`));
      continue;
    }

    if (!hasValue || isEmptyValue(value)) {
      continue;
    }

    validateRecordFieldValue(field, value, errors);
  }

  return result(errors);
}

function validateField(field: FormField, index: number, fieldIds: Set<string>, errors: ValidationError[]) {
  const path = `fields[${index}]`;

  if (!isNonEmptyString(field.id)) {
    errors.push(error(`${path}.id`, "field.id_required", "Field id is required."));
  } else if (fieldIds.has(field.id)) {
    errors.push(error(`${path}.id`, "field.duplicate_id", `Field id '${field.id}' is duplicated.`));
  } else {
    fieldIds.add(field.id);
  }

  if (!isNonEmptyString(field.label)) {
    errors.push(error(`${path}.label`, "field.label_required", "Field label is required."));
  }

  if (!isNonEmptyString(field.type) || !isFormFieldType(field.type)) {
    errors.push(error(`${path}.type`, "field.type_unknown", "Field type is not supported in V1."));
  }

  if (choiceFieldTypes.has(field.type)) {
    validateOptions(field, path, errors);
  }
}

function validateOptions(field: FormField, path: string, errors: ValidationError[]) {
  const options = Array.isArray(field.options) ? field.options : [];
  const optionValues = new Set<string>();

  if (options.length === 0) {
    errors.push(error(`${path}.options`, "field.options_required", `'${field.label}' requires at least one option.`));
  }

  options.forEach((option, optionIndex) => {
    const optionPath = `${path}.options[${optionIndex}]`;

    if (!isNonEmptyString(option.id)) {
      errors.push(error(`${optionPath}.id`, "field.option_id_required", "Option id is required."));
    }

    if (!isNonEmptyString(option.label)) {
      errors.push(error(`${optionPath}.label`, "field.option_label_required", "Option label is required."));
    }

    if (!isNonEmptyString(option.value)) {
      errors.push(error(`${optionPath}.value`, "field.option_value_required", "Option value is required."));
    } else if (optionValues.has(option.value)) {
      errors.push(error(`${optionPath}.value`, "field.option_value_duplicate", `Option value '${option.value}' is duplicated.`));
    } else {
      optionValues.add(option.value);
    }
  });
}

function validateLayout(layout: FormLayout | undefined, fieldIds: Set<string>, errors: ValidationError[]) {
  const referencedFields = new Set<string>();

  if (!layout || !Array.isArray(layout.pages) || layout.pages.length === 0) {
    errors.push(error("layout.pages", "layout.pages_required", "At least one layout page is required."));
    return;
  }

  layout.pages.forEach((page, pageIndex) => {
    const pagePath = `layout.pages[${pageIndex}]`;

    if (!isNonEmptyString(page.id)) {
      errors.push(error(`${pagePath}.id`, "layout.page_id_required", "Page id is required."));
    }

    if (!Array.isArray(page.sections) || page.sections.length === 0) {
      errors.push(error(`${pagePath}.sections`, "layout.sections_required", "Each page requires at least one section."));
      return;
    }

    page.sections.forEach((section, sectionIndex) => {
      const sectionPath = `${pagePath}.sections[${sectionIndex}]`;

      if (!isNonEmptyString(section.id)) {
        errors.push(error(`${sectionPath}.id`, "layout.section_id_required", "Section id is required."));
      }

      if (!Array.isArray(section.rows) || section.rows.length === 0) {
        errors.push(error(`${sectionPath}.rows`, "layout.rows_required", "Each section requires at least one row."));
        return;
      }

      section.rows.forEach((row, rowIndex) => {
        const rowPath = `${sectionPath}.rows[${rowIndex}]`;

        if (!isNonEmptyString(row.id)) {
          errors.push(error(`${rowPath}.id`, "layout.row_id_required", "Row id is required."));
        }

        if (!Array.isArray(row.columns) || row.columns.length === 0) {
          errors.push(error(`${rowPath}.columns`, "layout.columns_required", "Each row requires at least one column."));
          return;
        }

        row.columns.forEach((column, columnIndex) => {
          const columnPath = `${rowPath}.columns[${columnIndex}]`;

          if (!isNonEmptyString(column.id)) {
            errors.push(error(`${columnPath}.id`, "layout.column_id_required", "Column id is required."));
          }

          validateSpan(column.span, columnPath, errors);
          validateLayoutFields(column.fields, columnPath, fieldIds, referencedFields, errors);
        });
      });
    });
  });

  for (const fieldId of fieldIds) {
    if (!referencedFields.has(fieldId)) {
      errors.push(error("layout", "layout.field_missing", `Field '${fieldId}' is not placed in the layout.`));
    }
  }
}

function validateSpan(span: ResponsiveSpan, path: string, errors: ValidationError[]) {
  for (const breakpoint of breakpoints) {
    const value = span?.[breakpoint];

    if (!Number.isInteger(value) || value < 1 || value > 12) {
      errors.push(error(`${path}.span.${breakpoint}`, "layout.span_invalid", `${breakpoint} span must be an integer from 1 to 12.`));
      continue;
    }
  }
}

function validateLayoutFields(
  fields: string[],
  path: string,
  fieldIds: Set<string>,
  referencedFields: Set<string>,
  errors: ValidationError[]
) {
  if (!Array.isArray(fields)) {
    errors.push(error(`${path}.fields`, "layout.fields_required", "Column fields must be an array."));
    return;
  }

  fields.forEach((fieldId, fieldIndex) => {
    const fieldPath = `${path}.fields[${fieldIndex}]`;

    if (!isNonEmptyString(fieldId)) {
      errors.push(error(fieldPath, "layout.field_id_required", "Layout field id is required."));
      return;
    }

    if (!fieldIds.has(fieldId)) {
      errors.push(error(fieldPath, "layout.field_unknown", `Layout references unknown field '${fieldId}'.`));
      return;
    }

    if (referencedFields.has(fieldId)) {
      errors.push(error(fieldPath, "layout.field_duplicate", `Field '${fieldId}' is placed more than once.`));
      return;
    }

    referencedFields.add(fieldId);
  });
}

function validateRecordFieldValue(field: FormField, value: FormRecordValue, errors: ValidationError[]) {
  const path = `values.${field.id}`;

  if (textFieldTypes.has(field.type) && typeof value !== "string") {
    errors.push(error(path, "record.type", `'${field.label}' must be text.`));
    return;
  }

  if (field.type === "email") {
    if (typeof value !== "string") {
      errors.push(error(path, "record.type", `'${field.label}' must be an email string.`));
      return;
    }

    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value)) {
      errors.push(error(path, "record.email", `'${field.label}' must be a valid email address.`));
    }

    return;
  }

  if (field.type === "number") {
    if (typeof value !== "number" || !Number.isFinite(value)) {
      errors.push(error(path, "record.type", `'${field.label}' must be a finite number.`));
    }

    return;
  }

  if (field.type === "date") {
    if (typeof value !== "string" || !/^\d{4}-\d{2}-\d{2}$/.test(value)) {
      errors.push(error(path, "record.date", `'${field.label}' must use YYYY-MM-DD format.`));
    }

    return;
  }

  if (field.type === "checkbox") {
    if (typeof value !== "boolean") {
      errors.push(error(path, "record.type", `'${field.label}' must be true or false.`));
    }

    return;
  }

  if (choiceFieldTypes.has(field.type)) {
    if (typeof value !== "string") {
      errors.push(error(path, "record.type", `'${field.label}' must be an option value.`));
      return;
    }

    const allowedValues = new Set((field.options ?? []).map((option) => option.value));

    if (!allowedValues.has(value)) {
      errors.push(error(path, "record.option_unknown", `'${field.label}' has an unknown option value.`));
    }
  }
}

function isEmptyValue(value: FormRecordValue | undefined): boolean {
  return value === undefined || value === null || value === "";
}

function isNonEmptyString(value: unknown): value is string {
  return typeof value === "string" && value.trim().length > 0;
}

function error(path: string, code: string, message: string): ValidationError {
  return { path, code, message };
}

function result(errors: ValidationError[]): ValidationResult {
  return { valid: errors.length === 0, errors };
}

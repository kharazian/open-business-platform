import {
  type FormField,
  addressSubfields,
  type FormAddressValue,
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
const textFieldTypes = new Set(["text", "textarea", "phone", "fileUpload"]);
const numericFieldTypes = new Set(["number", "currency"]);
const lookupFieldTypes = new Set(["recordLookup"]);
const subTableFieldTypes = new Set(["subTable"]);
const addressSubfieldSet = new Set<string>(addressSubfields);
const breakpoints = ["mobile", "tablet", "desktop"] as const;
const guidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
const timePattern = /^([01]\d|2[0-3]):[0-5]\d$/;
const datetimePattern = /^\d{4}-\d{2}-\d{2}T([01]\d|2[0-3]):[0-5]\d(:[0-5]\d)?$/;

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

  if (lookupFieldTypes.has(field.type)) {
    validateLookupConfig(field, path, errors);
  }

  if (subTableFieldTypes.has(field.type)) {
    validateSubTableConfig(field, path, errors);
  }

  if (field.type === "address") {
    validateAddressConfig(field, path, errors);
  }
}

function validateAddressConfig(field: FormField, path: string, errors: ValidationError[]) {
  if (!field.address) {
    errors.push(error(`${path}.address`, "field.address_required", `'${field.label}' requires address configuration.`));
    return;
  }
  const seen = new Set<string>();
  (field.address.requiredSubfields ?? []).forEach((subfield, index) => {
    if (!addressSubfieldSet.has(subfield)) {
      errors.push(error(`${path}.address.requiredSubfields[${index}]`, "field.address_subfield_unknown", `Address subfield '${subfield}' is not supported.`));
    } else if (seen.has(subfield)) {
      errors.push(error(`${path}.address.requiredSubfields[${index}]`, "field.address_subfield_duplicate", `Address subfield '${subfield}' is duplicated.`));
    } else {
      seen.add(subfield);
    }
  });
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

function validateLookupConfig(field: FormField, path: string, errors: ValidationError[]) {
  const lookup = field.lookup;

  if (!lookup) {
    errors.push(error(`${path}.lookup`, "field.lookup_required", `'${field.label}' requires lookup configuration.`));
    return;
  }

  if (lookup.sourceType !== "form_records") {
    errors.push(error(`${path}.lookup.sourceType`, "field.lookup_source_type", `'${field.label}' lookup source is not supported.`));
  }

  if (!isNonEmptyString(lookup.sourceFormId)) {
    errors.push(error(`${path}.lookup.sourceFormId`, "field.lookup_source_form_required", `'${field.label}' requires a source form.`));
  }

  if (!Array.isArray(lookup.labelFieldIds) || lookup.labelFieldIds.length === 0 || lookup.labelFieldIds.some((fieldId) => !isNonEmptyString(fieldId))) {
    errors.push(error(`${path}.lookup.labelFieldIds`, "field.lookup_label_fields_required", `'${field.label}' requires at least one label field.`));
  }

  if (!Array.isArray(lookup.searchFieldIds) || lookup.searchFieldIds.length === 0 || lookup.searchFieldIds.some((fieldId) => !isNonEmptyString(fieldId))) {
    errors.push(error(`${path}.lookup.searchFieldIds`, "field.lookup_search_fields_required", `'${field.label}' requires at least one search field.`));
  }

  const filters = Array.isArray(lookup.filters) ? lookup.filters : [];
  filters.forEach((filter, filterIndex) => {
    if (!isNonEmptyString(filter.sourceFieldId) || !isNonEmptyString(filter.valueFromFieldId)) {
      errors.push(error(`${path}.lookup.filters[${filterIndex}]`, "field.lookup_filter_required", `'${field.label}' lookup filters require source and parent fields.`));
    }
  });
}

function validateSubTableConfig(field: FormField, path: string, errors: ValidationError[]) {
  const subTable = field.subTable;

  if (!subTable) {
    errors.push(error(`${path}.subTable`, "field.sub_table_required", `'${field.label}' requires sub-table configuration.`));
    return;
  }

  if (subTable.sourceType !== "child_form_records") {
    errors.push(error(`${path}.subTable.sourceType`, "field.sub_table_source_type", `'${field.label}' sub-table source is not supported.`));
  }

  if (!isNonEmptyString(subTable.childFormId)) {
    errors.push(error(`${path}.subTable.childFormId`, "field.sub_table_child_form_required", `'${field.label}' requires a child form.`));
  }

  if (!isNonEmptyString(subTable.parentLookupFieldId)) {
    errors.push(error(`${path}.subTable.parentLookupFieldId`, "field.sub_table_parent_lookup_required", `'${field.label}' requires a parent lookup field.`));
  }

  if (
    !Array.isArray(subTable.displayColumnFieldIds) ||
    subTable.displayColumnFieldIds.length === 0 ||
    subTable.displayColumnFieldIds.some((fieldId) => !isNonEmptyString(fieldId))
  ) {
    errors.push(
      error(
        `${path}.subTable.displayColumnFieldIds`,
        "field.sub_table_display_fields_required",
        `'${field.label}' requires at least one display column.`
      )
    );
  }

  const minRows = subTable.minRows;
  const maxRows = subTable.maxRows;

  if (minRows !== undefined && (!Number.isInteger(minRows) || minRows < 0)) {
    errors.push(error(`${path}.subTable.minRows`, "field.sub_table_min_rows", `'${field.label}' minimum rows must be 0 or greater.`));
  }

  if (maxRows !== undefined && (!Number.isInteger(maxRows) || maxRows < 1)) {
    errors.push(error(`${path}.subTable.maxRows`, "field.sub_table_max_rows", `'${field.label}' maximum rows must be 1 or greater.`));
  }

  if (minRows !== undefined && maxRows !== undefined && Number.isInteger(minRows) && Number.isInteger(maxRows) && minRows > maxRows) {
    errors.push(error(`${path}.subTable.maxRows`, "field.sub_table_row_range", `'${field.label}' minimum rows cannot exceed maximum rows.`));
  }
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

  if (field.type === "subTable") {
    errors.push(error(path, "record.sub_table_readonly", `'${field.label}' is stored through related child records.`));
    return;
  }

  if (field.type === "address") {
    validateAddressValue(field, value, path, errors);
    return;
  }

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

  if (numericFieldTypes.has(field.type)) {
    if (typeof value !== "number" || !Number.isFinite(value)) {
      errors.push(error(path, "record.type", `'${field.label}' must be a finite number.`));
    }

    return;
  }

  if (field.type === "percent") {
    if (typeof value !== "number" || !Number.isFinite(value)) {
      errors.push(error(path, "record.type", `'${field.label}' must be a finite number.`));
      return;
    }

    if (value < 0 || value > 100) {
      errors.push(error(path, "record.percent", `'${field.label}' must be between 0 and 100.`));
    }

    return;
  }

  if (field.type === "rating") {
    if (typeof value !== "number" || !Number.isInteger(value)) {
      errors.push(error(path, "record.type", `'${field.label}' must be a whole number.`));
      return;
    }

    if (value < 1 || value > 5) {
      errors.push(error(path, "record.rating", `'${field.label}' must be a rating from 1 to 5.`));
    }

    return;
  }

  if (field.type === "date") {
    if (typeof value !== "string" || !/^\d{4}-\d{2}-\d{2}$/.test(value)) {
      errors.push(error(path, "record.date", `'${field.label}' must use YYYY-MM-DD format.`));
    }

    return;
  }

  if (field.type === "time") {
    if (typeof value !== "string" || !timePattern.test(value)) {
      errors.push(error(path, "record.time", `'${field.label}' must use HH:mm format.`));
    }

    return;
  }

  if (field.type === "datetime") {
    if (typeof value !== "string" || !datetimePattern.test(value)) {
      errors.push(error(path, "record.datetime", `'${field.label}' must use date and time format.`));
    }

    return;
  }

  if (field.type === "url") {
    if (typeof value !== "string" || !isHttpUrl(value)) {
      errors.push(error(path, "record.url", `'${field.label}' must be a valid URL.`));
    }

    return;
  }

  if (field.type === "checkbox") {
    if (typeof value !== "boolean") {
      errors.push(error(path, "record.type", `'${field.label}' must be true or false.`));
    }

    return;
  }

  if (field.type === "recordLookup") {
    if (typeof value !== "string" || !guidPattern.test(value)) {
      errors.push(error(path, "record.lookup_type", `'${field.label}' must be a selected record id.`));
    }

    return;
  }

  if (field.type === "userPicker") {
    if (typeof value !== "string" || !guidPattern.test(value)) {
      errors.push(error(path, "record.user_picker_type", `'${field.label}' must be a selected user id.`));
    }

    return;
  }

  if (field.type === "departmentPicker") {
    if (typeof value !== "string" || !guidPattern.test(value)) {
      errors.push(error(path, "record.department_picker_type", `'${field.label}' must be a selected department id.`));
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

function validateAddressValue(field: FormField, value: FormRecordValue, path: string, errors: ValidationError[]) {
  if (!isAddressValue(value)) {
    errors.push(error(path, "record.address_type", `'${field.label}' must be a structured address.`));
    return;
  }
  for (const [member, memberValue] of Object.entries(value)) {
    const memberPath = `${path}.${member}`;
    if (!addressSubfieldSet.has(member)) {
      errors.push(error(memberPath, "record.address_member_unknown", `'${field.label}' contains an unsupported address member.`));
      continue;
    }
    if (memberValue === null || memberValue === undefined || memberValue === "") continue;
    if (member === "latitude" || member === "longitude") {
      const min = member === "latitude" ? -90 : -180;
      const max = member === "latitude" ? 90 : 180;
      if (typeof memberValue !== "number" || !Number.isFinite(memberValue)) {
        errors.push(error(memberPath, "record.address_coordinate_type", `'${field.label}' coordinate must be a finite number.`));
      } else if (memberValue < min || memberValue > max) {
        errors.push(error(memberPath, "record.address_coordinate_range", `'${field.label}' ${member} must be between ${min} and ${max}.`));
      }
      continue;
    }
    const maxLength = member === "country" ? 100 : 200;
    if (typeof memberValue !== "string") {
      errors.push(error(memberPath, "record.address_member_type", `'${field.label}' address text must be a string.`));
    } else if (memberValue.trim().length > maxLength) {
      errors.push(error(memberPath, "record.address_member_length", `'${field.label}' ${member} must be at most ${maxLength} characters.`));
    }
  }
  for (const required of field.address?.requiredSubfields ?? []) {
    const memberValue = value[required];
    if (memberValue === undefined || memberValue === null || memberValue === "") {
      errors.push(error(`${path}.${required}`, "record.address_member_required", `'${field.label}' ${required} is required.`));
    }
  }
}

function isAddressValue(value: FormRecordValue): value is FormAddressValue {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function isEmptyValue(value: FormRecordValue | undefined): boolean {
  return value === undefined || value === null || value === "" || (isAddressValue(value) && Object.values(value).every((member) => member === undefined || member === null || member === ""));
}

function isNonEmptyString(value: unknown): value is string {
  return typeof value === "string" && value.trim().length > 0;
}

function isHttpUrl(value: string): boolean {
  try {
    const parsed = new URL(value);
    return parsed.protocol === "http:" || parsed.protocol === "https:";
  } catch {
    return false;
  }
}

function error(path: string, code: string, message: string): ValidationError {
  return { path, code, message };
}

function result(errors: ValidationError[]): ValidationResult {
  return { valid: errors.length === 0, errors };
}

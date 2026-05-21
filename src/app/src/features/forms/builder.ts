import {
  formFieldTypes,
  type FormField,
  type FormFieldOption,
  type FormFieldType,
  type FormLayout,
  type FormLayoutColumn,
  type FormLayoutSection,
  type FormRecordValue,
  type ResponsiveSpan,
  type FormSchema
} from "./types";

export const fieldTypeLabels: Record<FormFieldType, string> = {
  text: "Text",
  textarea: "Textarea",
  number: "Number",
  email: "Email",
  phone: "Phone",
  date: "Date",
  select: "Select",
  checkbox: "Checkbox",
  radio: "Radio"
};

export const fieldTypeDescriptions: Record<FormFieldType, string> = {
  text: "Single-line text input",
  textarea: "Multi-line text area",
  number: "Numeric value",
  email: "Email address",
  phone: "Phone number",
  date: "Calendar date",
  select: "Dropdown choice",
  checkbox: "True or false checkbox",
  radio: "Single visible choice"
};

export const choiceFieldTypes = ["select", "radio"] as const;

export type LayoutWidthValue = "full" | "half" | "third" | "twoThirds";

export const layoutWidthOptions: Array<{ label: string; value: LayoutWidthValue; span: ResponsiveSpan }> = [
  { label: "Full width", value: "full", span: { mobile: 12, tablet: 12, desktop: 12 } },
  { label: "Half width", value: "half", span: { mobile: 12, tablet: 6, desktop: 6 } },
  { label: "One third", value: "third", span: { mobile: 12, tablet: 4, desktop: 4 } },
  { label: "Two thirds", value: "twoThirds", span: { mobile: 12, tablet: 8, desktop: 8 } }
];

type BuilderStorage = Pick<Storage, "getItem" | "setItem">;

export type AddFieldResult = {
  schema: FormSchema;
  field: FormField;
};

export function createEmptyFormBuilderSchema(): FormSchema {
  return {
    schemaVersion: 1,
    fields: [],
    layout: {
      pages: [
        {
          id: "page_1",
          title: "Page 1",
          sections: [
            {
              id: "section_1",
              title: "Main",
              rows: []
            }
          ]
        }
      ]
    }
  };
}

export function addFieldToSchema(schema: FormSchema, type: FormFieldType): AddFieldResult {
  const field = createField(type, schema.fields);

  return {
    field,
    schema: {
      ...schema,
      fields: [...schema.fields, field],
      layout: addFieldToLayout(schema.layout, field.id)
    }
  };
}

export function updateFieldInSchema(schema: FormSchema, field: FormField): FormSchema {
  const normalizedField = normalizeField(field);

  return {
    ...schema,
    fields: schema.fields.map((candidate) => (candidate.id === field.id ? normalizedField : candidate))
  };
}

export function deleteFieldFromSchema(schema: FormSchema, fieldId: string): FormSchema {
  return {
    ...schema,
    fields: schema.fields.filter((field) => field.id !== fieldId),
    layout: removeFieldFromLayout(schema.layout, fieldId)
  };
}

export function getFieldLayoutWidth(schema: FormSchema, fieldId: string): LayoutWidthValue {
  const column = findColumnForField(schema.layout, fieldId);
  const desktopSpan = column?.span.desktop ?? 12;
  return layoutWidthOptions.find((option) => option.span.desktop === desktopSpan)?.value ?? "full";
}

export function updateFieldLayoutWidth(schema: FormSchema, fieldId: string, width: LayoutWidthValue): FormSchema {
  if (!schema.fields.some((field) => field.id === fieldId)) {
    return schema;
  }

  const option = layoutWidthOptions.find((candidate) => candidate.value === width) ?? layoutWidthOptions[0];

  return {
    ...schema,
    layout: {
      pages: schema.layout.pages.map((page) => ({
        ...page,
        sections: page.sections.map((section) =>
          sectionContainsField(section, fieldId) ? repackSectionRows(section, fieldId, option.span) : section
        )
      }))
    }
  };
}

export function createFormBuilderDraftStorageKey(formId: string): string {
  return `obp.formBuilderDraft.${formId}`;
}

export function loadFormBuilderDraft(formId: string, storage: BuilderStorage = window.localStorage): FormSchema {
  try {
    const rawValue = storage.getItem(createFormBuilderDraftStorageKey(formId));

    if (!rawValue) {
      return createEmptyFormBuilderSchema();
    }

    const parsed = JSON.parse(rawValue) as Partial<FormSchema>;

    if (!isUsableSchema(parsed)) {
      return createEmptyFormBuilderSchema();
    }

    return parsed;
  } catch {
    return createEmptyFormBuilderSchema();
  }
}

export function saveFormBuilderDraft(formId: string, schema: FormSchema, storage: BuilderStorage = window.localStorage): void {
  storage.setItem(createFormBuilderDraftStorageKey(formId), JSON.stringify(schema));
}

export function isChoiceFieldType(type: FormFieldType): type is (typeof choiceFieldTypes)[number] {
  return (choiceFieldTypes as readonly string[]).includes(type);
}

export function getDefaultFieldValue(field: FormField): FormRecordValue | undefined {
  if (field.defaultValue !== undefined) {
    return field.defaultValue;
  }

  return field.type === "checkbox" ? false : "";
}

function createField(type: FormFieldType, existingFields: FormField[]): FormField {
  const label = fieldTypeLabels[type];
  const field: FormField = {
    id: createUniqueFieldId(label, existingFields),
    type,
    label,
    required: false
  };

  if (isChoiceFieldType(type)) {
    field.options = createDefaultOptions(field.id);
  }

  return field;
}

function createUniqueFieldId(label: string, existingFields: FormField[]): string {
  const baseId = slugify(label) || "field";
  const existingIds = new Set(existingFields.map((field) => field.id));

  if (!existingIds.has(baseId)) {
    return baseId;
  }

  let counter = 2;
  let candidate = `${baseId}_${counter}`;

  while (existingIds.has(candidate)) {
    counter += 1;
    candidate = `${baseId}_${counter}`;
  }

  return candidate;
}

function createDefaultOptions(fieldId: string): FormFieldOption[] {
  return [
    { id: `${fieldId}_option_1`, label: "Option 1", value: "option_1" },
    { id: `${fieldId}_option_2`, label: "Option 2", value: "option_2" }
  ];
}

function normalizeField(field: FormField): FormField {
  const label = normalizeText(field.label) ?? fieldTypeLabels[field.type];
  const normalized: FormField = {
    ...field,
    label,
    required: Boolean(field.required),
    placeholder: normalizeText(field.placeholder),
    helpText: normalizeText(field.helpText),
    defaultValue: normalizeDefaultValue(field)
  };

  if (isChoiceFieldType(field.type)) {
    normalized.options = normalizeOptions(field.id, field.options);
  } else {
    delete normalized.options;
  }

  removeUndefinedOptionalProperties(normalized);

  return normalized;
}

function normalizeDefaultValue(field: FormField): FormRecordValue | undefined {
  if (field.defaultValue === undefined || field.defaultValue === null || field.defaultValue === "") {
    return undefined;
  }

  if (field.type === "checkbox") {
    return Boolean(field.defaultValue);
  }

  if (field.type === "number") {
    const numericValue = typeof field.defaultValue === "number" ? field.defaultValue : Number(field.defaultValue);
    return Number.isFinite(numericValue) ? numericValue : undefined;
  }

  return String(field.defaultValue).trim();
}

function normalizeOptions(fieldId: string, options: FormFieldOption[] | undefined): FormFieldOption[] {
  const normalizedOptions = (options ?? [])
    .map((option, index) => {
      const label = normalizeText(option.label) ?? `Option ${index + 1}`;
      const value = normalizeText(option.value) ?? slugify(label) ?? `option_${index + 1}`;

      return {
        id: normalizeText(option.id) ?? `${fieldId}_option_${index + 1}`,
        label,
        value
      };
    })
    .filter((option) => option.label.length > 0 && option.value.length > 0);

  return normalizedOptions.length > 0 ? normalizedOptions : createDefaultOptions(fieldId);
}

function addFieldToLayout(layout: FormLayout, fieldId: string): FormLayout {
  const page = layout.pages[0] ?? createEmptyFormBuilderSchema().layout.pages[0];
  const section = page.sections[0] ?? createEmptyFormBuilderSchema().layout.pages[0].sections[0];

  return {
    pages: [
      {
        ...page,
        sections: [
          {
            ...section,
            rows: [
              ...section.rows,
              {
                id: `row_${fieldId}`,
                columns: [
                  {
                    id: `col_${fieldId}`,
                    span: { mobile: 12, tablet: 12, desktop: 12 },
                    fields: [fieldId]
                  }
                ]
              }
            ]
          },
          ...page.sections.slice(1)
        ]
      },
      ...layout.pages.slice(1)
    ]
  };
}

function removeFieldFromLayout(layout: FormLayout, fieldId: string): FormLayout {
  return {
    pages: layout.pages.map((page) => ({
      ...page,
      sections: page.sections.map((section) => ({
        ...section,
        rows: section.rows
          .map((row) => ({
            ...row,
            columns: row.columns
              .map((column) => ({
                ...column,
                fields: column.fields.filter((candidate) => candidate !== fieldId)
              }))
              .filter((column) => column.fields.length > 0)
          }))
          .filter((row) => row.columns.length > 0)
      }))
    }))
  };
}

function findColumnForField(layout: FormLayout, fieldId: string): FormLayoutColumn | undefined {
  for (const page of layout.pages) {
    for (const section of page.sections) {
      for (const row of section.rows) {
        const column = row.columns.find((candidate) => candidate.fields.includes(fieldId));
        if (column) return column;
      }
    }
  }

  return undefined;
}

function sectionContainsField(section: FormLayoutSection, fieldId: string): boolean {
  return section.rows.some((row) => row.columns.some((column) => column.fields.includes(fieldId)));
}

function repackSectionRows(section: FormLayoutSection, targetFieldId: string, targetSpan: ResponsiveSpan): FormLayoutSection {
  const items = section.rows.flatMap((row) =>
    row.columns.flatMap((column) =>
      column.fields.map((fieldId) => ({
        fieldId,
        columnId: column.fields.length === 1 ? column.id : `col_${fieldId}`,
        span: fieldId === targetFieldId ? targetSpan : normalizeLayoutSpan(column.span)
      }))
    )
  );

  const rows: FormLayoutSection["rows"] = [];
  let currentColumns: FormLayoutColumn[] = [];
  let currentDesktopSpan = 0;

  for (const item of items) {
    const desktopSpan = clampSpan(item.span.desktop);

    if (currentColumns.length > 0 && currentDesktopSpan + desktopSpan > 12) {
      rows.push(createPackedRow(currentColumns));
      currentColumns = [];
      currentDesktopSpan = 0;
    }

    currentColumns.push({
      id: item.columnId,
      span: normalizeLayoutSpan(item.span),
      fields: [item.fieldId]
    });
    currentDesktopSpan += desktopSpan;
  }

  if (currentColumns.length > 0) {
    rows.push(createPackedRow(currentColumns));
  }

  return {
    ...section,
    rows
  };
}

function createPackedRow(columns: FormLayoutColumn[]): FormLayoutSection["rows"][number] {
  return {
    id: `row_${columns[0]?.fields[0] ?? "empty"}`,
    columns
  };
}

function normalizeLayoutSpan(span: ResponsiveSpan): ResponsiveSpan {
  return {
    mobile: 12,
    tablet: clampSpan(span.tablet),
    desktop: clampSpan(span.desktop)
  };
}

function clampSpan(value: number): number {
  if (!Number.isInteger(value)) return 12;
  return Math.min(12, Math.max(1, value));
}

function isUsableSchema(value: Partial<FormSchema>): value is FormSchema {
  return (
    value.schemaVersion === 1 &&
    Array.isArray(value.fields) &&
    value.fields.every((field) => formFieldTypes.includes(field.type)) &&
    Boolean(value.layout) &&
    Array.isArray(value.layout?.pages)
  );
}

function normalizeText(value: unknown): string | undefined {
  const normalized = typeof value === "string" ? value.trim() : "";
  return normalized.length > 0 ? normalized : undefined;
}

function removeUndefinedOptionalProperties(field: FormField): void {
  if (field.placeholder === undefined) delete field.placeholder;
  if (field.helpText === undefined) delete field.helpText;
  if (field.defaultValue === undefined) delete field.defaultValue;
  if (field.validation === undefined) delete field.validation;
}

function slugify(value: string): string {
  return value
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "_")
    .replace(/^_+|_+$/g, "");
}

import { addFieldToSchema } from "./builder";
import type {
  FormField,
  FormFieldType,
  FormLayout,
  FormLayoutColumn,
  FormLayoutRow,
  FormLayoutSection,
  FormSchema,
  ResponsiveSpan
} from "./types";

export type LayoutBlockDrop = {
  kind: "section" | "row";
  sectionId: string;
  position: "end" | "before" | "after";
  rowId?: string;
  spans?: number[];
};

export type DesignerDropTarget =
  | { type: "section"; sectionId: string }
  | { type: "row"; rowId: string; index: number }
  | { type: "column"; columnId: string; index: number };

export type InsertFieldResult = {
  schema: FormSchema;
  field: FormField;
};

export type AddColumnResult = {
  schema: FormSchema;
  column: FormLayoutColumn | null;
};

export type ColumnActionState = {
  canBalance: boolean;
  canDelete: boolean;
  canMoveLeft: boolean;
  canMoveRight: boolean;
};

const defaultSectionTitle = "New section";

export function addLayoutBlockToSchema(schema: FormSchema, block: LayoutBlockDrop): FormSchema {
  const layoutIds = collectLayoutIds(schema.layout);

  if (block.kind === "section") {
    return {
      ...schema,
      layout: {
        pages: schema.layout.pages.map((page, pageIndex) => {
          if (pageIndex !== 0) return page;

          const targetIndex = page.sections.findIndex((section) => section.id === block.sectionId);
          const insertIndex = block.position === "before" ? Math.max(targetIndex, 0) : targetIndex >= 0 ? targetIndex + 1 : page.sections.length;
          const nextSections = [...page.sections];
          nextSections.splice(insertIndex, 0, createSection(layoutIds.sectionIds));
          return { ...page, sections: nextSections };
        })
      }
    };
  }

  const spans = block.spans?.length ? block.spans : [12];

  return {
    ...schema,
    layout: mapSections(schema.layout, (section) => {
      if (section.id !== block.sectionId) return section;

      const row = createRow(spans, layoutIds);
      const rowIndex = block.rowId ? section.rows.findIndex((candidate) => candidate.id === block.rowId) : -1;
      const insertIndex =
        rowIndex >= 0 && block.position === "before" ? rowIndex : rowIndex >= 0 && block.position === "after" ? rowIndex + 1 : section.rows.length;
      const rows = [...section.rows];
      rows.splice(insertIndex, 0, row);
      return { ...section, rows };
    })
  };
}

export function insertNewFieldAtTarget(schema: FormSchema, type: FormFieldType, target: DesignerDropTarget): InsertFieldResult {
  const added = addFieldToSchema(schema, type);
  const schemaWithoutAutoPlacement = removeFieldReferences(added.schema, added.field.id);

  return {
    field: added.field,
    schema: moveFieldToTarget(schemaWithoutAutoPlacement, added.field.id, target)
  };
}

export function moveFieldToTarget(schema: FormSchema, fieldId: string, target: DesignerDropTarget): FormSchema {
  if (!schema.fields.some((field) => field.id === fieldId) || !targetExists(schema.layout, target)) {
    return schema;
  }

  const sourceLocation = findFieldLocation(schema.layout, fieldId);
  const withoutField = removeFieldReferences(schema, fieldId);
  const layoutIds = collectLayoutIds(withoutField.layout);

  if (target.type === "section") {
    return {
      ...withoutField,
      layout: mapSections(withoutField.layout, (section) =>
        section.id === target.sectionId ? { ...section, rows: [...section.rows, createRow([12], layoutIds, [[fieldId]])] } : section
      )
    };
  }

  if (target.type === "row") {
    return {
      ...withoutField,
      layout: mapRows(withoutField.layout, (row) => {
        if (row.id !== target.rowId) return row;
        const columns = [...row.columns];
        columns.splice(clampIndex(target.index, columns.length), 0, createColumn(12, layoutIds, [fieldId]));
        return { ...row, columns };
      })
    };
  }

  return {
    ...withoutField,
    layout: mapColumns(withoutField.layout, (column) => {
      if (column.id !== target.columnId) return column;
      const fields = [...column.fields];
      fields.splice(clampIndex(getAdjustedColumnTargetIndex(sourceLocation, target), fields.length), 0, fieldId);
      return { ...column, fields };
    })
  };
}

export function updateColumnSpan(schema: FormSchema, columnId: string, span: ResponsiveSpan): FormSchema {
  return {
    ...schema,
    layout: mapColumns(schema.layout, (column) =>
      column.id === columnId
        ? { ...column, span: { mobile: 12, tablet: clampSpan(span.tablet), desktop: clampSpan(span.desktop) } }
        : column
    )
  };
}

export function resizeColumnSpan(schema: FormSchema, columnId: string, direction: "grow" | "shrink"): FormSchema {
  const delta = direction === "grow" ? 1 : -1;

  return {
    ...schema,
    layout: mapColumns(schema.layout, (column) =>
      column.id === columnId
        ? {
            ...column,
            span: {
              mobile: 12,
              tablet: clampSpan(column.span.tablet + delta),
              desktop: clampSpan(column.span.desktop + delta)
            }
          }
        : column
    )
  };
}

export function addColumnNearColumn(schema: FormSchema, columnId: string, position: "before" | "after"): AddColumnResult {
  const layoutIds = collectLayoutIds(schema.layout);
  let addedColumn: FormLayoutColumn | null = null;

  const layout = mapRows(schema.layout, (row) => {
    const targetIndex = row.columns.findIndex((column) => column.id === columnId);
    if (targetIndex < 0) return row;

    const targetColumn = row.columns[targetIndex];
    const splitSpan = splitResponsiveSpan(targetColumn.span);
    const nextColumns = [...row.columns];
    const insertIndex = position === "before" ? targetIndex : targetIndex + 1;

    addedColumn = createColumnFromSpan(splitSpan.addedSpan, layoutIds, []);
    nextColumns[targetIndex] = { ...targetColumn, span: splitSpan.sourceSpan };
    nextColumns.splice(insertIndex, 0, addedColumn);

    return { ...row, columns: nextColumns };
  });

  return {
    column: addedColumn,
    schema: addedColumn ? { ...schema, layout } : schema
  };
}

export function moveColumn(schema: FormSchema, columnId: string, direction: "left" | "right"): FormSchema {
  return {
    ...schema,
    layout: mapRows(schema.layout, (row) => {
      const sourceIndex = row.columns.findIndex((column) => column.id === columnId);
      if (sourceIndex < 0) return row;

      const targetIndex = direction === "left" ? sourceIndex - 1 : sourceIndex + 1;
      if (targetIndex < 0 || targetIndex >= row.columns.length) return row;

      const columns = [...row.columns];
      const [column] = columns.splice(sourceIndex, 1);
      columns.splice(targetIndex, 0, column);
      return { ...row, columns };
    })
  };
}

export function getColumnActionState(row: FormLayoutRow, columnId: string): ColumnActionState {
  const columnIndex = row.columns.findIndex((column) => column.id === columnId);
  const column = columnIndex >= 0 ? row.columns[columnIndex] : null;

  return {
    canBalance: row.columns.length >= 1 && row.columns.length <= 4,
    canDelete: Boolean(column && row.columns.length > 1 && column.fields.length === 0),
    canMoveLeft: columnIndex > 0,
    canMoveRight: columnIndex >= 0 && columnIndex < row.columns.length - 1
  };
}

export function deleteColumnIfEmpty(schema: FormSchema, columnId: string): FormSchema {
  return {
    ...schema,
    layout: mapRows(schema.layout, (row) => {
      const targetColumn = row.columns.find((column) => column.id === columnId);
      if (!targetColumn || targetColumn.fields.length > 0 || row.columns.length <= 1) return row;

      return { ...row, columns: row.columns.filter((column) => column.id !== columnId) };
    })
  };
}

export function balanceRowColumns(schema: FormSchema, rowId: string): FormSchema {
  return {
    ...schema,
    layout: mapRows(schema.layout, (row) => {
      if (row.id !== rowId) return row;

      const balancedSpan = getBalancedColumnSpan(row.columns.length);
      if (!balancedSpan) return row;

      return {
        ...row,
        columns: row.columns.map((column) => ({
          ...column,
          span: { mobile: 12, tablet: balancedSpan, desktop: balancedSpan }
        }))
      };
    })
  };
}

export function updateSectionDetails(
  schema: FormSchema,
  sectionId: string,
  patch: Pick<FormLayoutSection, "title" | "description">
): FormSchema {
  return {
    ...schema,
    layout: mapSections(schema.layout, (section) =>
      section.id === sectionId
        ? {
            ...section,
            title: normalizeOptionalText(patch.title, section.title),
            description: normalizeOptionalText(patch.description, section.description)
          }
        : section
    )
  };
}

export function deleteLayoutRowIfEmpty(schema: FormSchema, rowId: string): FormSchema {
  return {
    ...schema,
    layout: mapSections(schema.layout, (section) => ({
      ...section,
      rows: section.rows.filter((row) => row.id !== rowId || !isLayoutRowEmpty(row))
    }))
  };
}

export function deleteLayoutSectionIfEmpty(schema: FormSchema, sectionId: string): FormSchema {
  return {
    ...schema,
    layout: {
      pages: schema.layout.pages.map((page) => {
        if (page.sections.length <= 1) return page;

        return {
          ...page,
          sections: page.sections.filter((section) => section.id !== sectionId || !isLayoutSectionEmpty(section))
        };
      })
    }
  };
}

export function deleteSectionFromSchema(schema: FormSchema, sectionId: string): FormSchema {
  for (const page of schema.layout.pages) {
    const section = page.sections.find((candidate) => candidate.id === sectionId);
    if (!section || page.sections.length <= 1) continue;

    const fieldIdsToDelete = new Set(getSectionFieldIds(section));

    return {
      ...schema,
      fields: schema.fields.filter((field) => !fieldIdsToDelete.has(field.id)),
      layout: {
        pages: schema.layout.pages.map((candidatePage) =>
          candidatePage.id === page.id
            ? { ...candidatePage, sections: candidatePage.sections.filter((candidate) => candidate.id !== sectionId) }
            : candidatePage
        )
      }
    };
  }

  return schema;
}

export function getSectionFieldIds(section: FormLayoutSection): string[] {
  return section.rows.flatMap((row) => row.columns.flatMap((column) => column.fields));
}

export function removeEmptyLayoutContainers(schema: FormSchema): FormSchema {
  return {
    ...schema,
    layout: {
      pages: schema.layout.pages.map((page) => ({
        ...page,
        sections: page.sections.map((section) => ({
          ...section,
          rows: section.rows
            .map((row) => ({
              ...row,
              columns: row.columns.filter((column) => column.fields.length > 0)
            }))
            .filter((row) => row.columns.length > 0)
        }))
      }))
    }
  };
}

export function createDesignerWarningMessages(schema: FormSchema): string[] {
  const warnings: string[] = [];
  const placedFieldIds = new Set(
    schema.layout.pages.flatMap((page) => page.sections.flatMap((section) => section.rows.flatMap((row) => row.columns.flatMap((column) => column.fields))))
  );
  const fieldIds = new Set(schema.fields.map((field) => field.id));

  for (const page of schema.layout.pages) {
    for (const section of page.sections) {
      for (const row of section.rows) {
        if (row.columns.length === 0 || row.columns.every((column) => column.fields.length === 0)) {
          warnings.push(`Section '${section.title ?? section.id}' has an empty row.`);
        }

        for (const column of row.columns) {
          for (const fieldId of column.fields) {
            if (!fieldIds.has(fieldId)) {
              warnings.push(`Layout references missing field '${fieldId}'.`);
            }
          }
        }
      }
    }
  }

  for (const field of schema.fields) {
    if (!placedFieldIds.has(field.id)) {
      warnings.push(`Field '${field.label}' is not placed in the layout.`);
    }
  }

  return warnings;
}

export function getFieldDropTargets(schema: FormSchema): DesignerDropTarget[] {
  return schema.layout.pages.flatMap((page) =>
    page.sections.flatMap((section) => [
      { type: "section" as const, sectionId: section.id },
      ...section.rows.flatMap((row) => [
        { type: "row" as const, rowId: row.id, index: row.columns.length },
        ...row.columns.map((column) => ({ type: "column" as const, columnId: column.id, index: column.fields.length }))
      ])
    ])
  );
}

function createSection(existingSectionIds: Set<string>): FormLayoutSection {
  return {
    id: createUniqueId("section", existingSectionIds),
    title: defaultSectionTitle,
    rows: []
  };
}

function createRow(spans: number[], layoutIds: LayoutIds, fieldGroups: string[][] = []): FormLayoutRow {
  return {
    id: createUniqueId("row", layoutIds.rowIds),
    columns: spans.map((span, index) => createColumn(span, layoutIds, fieldGroups[index] ?? []))
  };
}

function createColumn(span: number, layoutIds: LayoutIds, fields: string[] = []): FormLayoutColumn {
  return {
    id: createUniqueId("col", layoutIds.columnIds),
    span: { mobile: 12, tablet: clampSpan(span), desktop: clampSpan(span) },
    fields
  };
}

function createColumnFromSpan(span: ResponsiveSpan, layoutIds: LayoutIds, fields: string[] = []): FormLayoutColumn {
  return {
    id: createUniqueId("col", layoutIds.columnIds),
    span: { mobile: 12, tablet: clampSpan(span.tablet), desktop: clampSpan(span.desktop) },
    fields
  };
}

function removeFieldReferences(schema: FormSchema, fieldId: string): FormSchema {
  return {
    ...schema,
    layout: mapColumns(schema.layout, (column) => ({ ...column, fields: column.fields.filter((candidate) => candidate !== fieldId) }))
  };
}

function targetExists(layout: FormLayout, target: DesignerDropTarget): boolean {
  return layout.pages.some((page) =>
    page.sections.some((section) => {
      if (target.type === "section") return section.id === target.sectionId;

      return section.rows.some((row) => {
        if (target.type === "row") return row.id === target.rowId;
        return row.columns.some((column) => column.id === target.columnId);
      });
    })
  );
}

type FieldLocation = {
  columnId: string;
  index: number;
};

function findFieldLocation(layout: FormLayout, fieldId: string): FieldLocation | null {
  for (const page of layout.pages) {
    for (const section of page.sections) {
      for (const row of section.rows) {
        for (const column of row.columns) {
          const index = column.fields.indexOf(fieldId);

          if (index >= 0) {
            return { columnId: column.id, index };
          }
        }
      }
    }
  }

  return null;
}

export function isLayoutSectionEmpty(section: FormLayoutSection): boolean {
  return section.rows.every(isLayoutRowEmpty);
}

export function isLayoutRowEmpty(row: FormLayoutRow): boolean {
  return row.columns.every((column) => column.fields.length === 0);
}

function normalizeOptionalText(nextValue: string | undefined, currentValue: string | undefined): string | undefined {
  if (nextValue === undefined) return currentValue;

  const trimmedValue = nextValue.trim();
  return trimmedValue.length > 0 ? trimmedValue : undefined;
}

function splitResponsiveSpan(span: ResponsiveSpan): { sourceSpan: ResponsiveSpan; addedSpan: ResponsiveSpan } {
  const tabletSplit = splitSpan(span.tablet);
  const desktopSplit = splitSpan(span.desktop);

  return {
    sourceSpan: { mobile: 12, tablet: tabletSplit.source, desktop: desktopSplit.source },
    addedSpan: { mobile: 12, tablet: tabletSplit.added, desktop: desktopSplit.added }
  };
}

function splitSpan(value: number): { source: number; added: number } {
  const span = clampSpan(value);

  if (span <= 1) {
    return { source: 1, added: 12 };
  }

  const source = Math.floor(span / 2);
  return { source, added: span - source };
}

function getBalancedColumnSpan(columnCount: number): number | null {
  if (columnCount === 1) return 12;
  if (columnCount === 2) return 6;
  if (columnCount === 3) return 4;
  if (columnCount === 4) return 3;
  return null;
}

function getAdjustedColumnTargetIndex(sourceLocation: FieldLocation | null, target: Extract<DesignerDropTarget, { type: "column" }>): number {
  if (sourceLocation?.columnId === target.columnId && sourceLocation.index < target.index) {
    return target.index - 1;
  }

  return target.index;
}

function mapSections(layout: FormLayout, mapper: (section: FormLayoutSection) => FormLayoutSection): FormLayout {
  return {
    pages: layout.pages.map((page) => ({ ...page, sections: page.sections.map(mapper) }))
  };
}

function mapRows(layout: FormLayout, mapper: (row: FormLayoutRow) => FormLayoutRow): FormLayout {
  return mapSections(layout, (section) => ({ ...section, rows: section.rows.map(mapper) }));
}

function mapColumns(layout: FormLayout, mapper: (column: FormLayoutColumn) => FormLayoutColumn): FormLayout {
  return mapRows(layout, (row) => ({ ...row, columns: row.columns.map(mapper) }));
}

type LayoutIds = {
  sectionIds: Set<string>;
  rowIds: Set<string>;
  columnIds: Set<string>;
};

function collectLayoutIds(layout: FormLayout): LayoutIds {
  const sectionIds = new Set<string>();
  const rowIds = new Set<string>();
  const columnIds = new Set<string>();

  for (const page of layout.pages) {
    for (const section of page.sections) {
      sectionIds.add(section.id);

      for (const row of section.rows) {
        rowIds.add(row.id);

        for (const column of row.columns) {
          columnIds.add(column.id);
        }
      }
    }
  }

  return { sectionIds, rowIds, columnIds };
}

function clampSpan(value: number): number {
  return Number.isInteger(value) ? Math.min(12, Math.max(1, value)) : 12;
}

function clampIndex(index: number, length: number): number {
  return Math.min(Math.max(index, 0), length);
}

function createUniqueId(prefix: string, existingIds: Set<string>): string {
  let counter = 1;
  let candidate = `${prefix}_${counter}`;

  while (existingIds.has(candidate)) {
    counter += 1;
    candidate = `${prefix}_${counter}`;
  }

  existingIds.add(candidate);
  return candidate;
}

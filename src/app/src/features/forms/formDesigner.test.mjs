import assert from "node:assert/strict";
import { test } from "vitest";
import {
  addColumnNearColumn,
  addLayoutBlockToSchema,
  balanceRowColumns,
  createDesignerWarningMessages,
  deleteColumnIfEmpty,
  deleteLayoutRowIfEmpty,
  deleteLayoutSectionIfEmpty,
  deleteSectionFromSchema,
  getColumnActionState,
  getFieldDropTargets,
  getSectionFieldIds,
  insertNewFieldAtTarget,
  moveColumn,
  moveFieldWithinColumn,
  moveFieldToTarget,
  removeEmptyLayoutContainers,
  resizeColumnSpan,
  updateSectionDetails,
  updateColumnSpan
} from "./designer.ts";
import { addFieldToSchema, createEmptyFormBuilderSchema } from "./builder.ts";

test("designer helpers add layout blocks without changing existing fields", () => {
  const schema = createEmptyFormBuilderSchema();

  const withSection = addLayoutBlockToSchema(schema, { kind: "section", sectionId: "section_1", position: "after" });
  assert.equal(withSection.layout.pages[0].sections.length, 2);
  assert.equal(withSection.layout.pages[0].sections[1].id, "section_2");
  assert.equal(withSection.layout.pages[0].sections[1].title, "New section");

  const withTwoColumns = addLayoutBlockToSchema(withSection, {
    kind: "row",
    sectionId: "section_1",
    position: "end",
    spans: [6, 6]
  });

  const row = withTwoColumns.layout.pages[0].sections[0].rows[0];
  assert.equal(row.id, "row_1");
  assert.deepEqual(row.columns.map((column) => column.id), ["col_1", "col_2"]);
  assert.deepEqual(row.columns.map((column) => column.span.desktop), [6, 6]);
  assert.deepEqual(row.columns.map((column) => column.span.tablet), [6, 6]);
  assert.deepEqual(row.columns.map((column) => column.span.mobile), [12, 12]);
  assert.deepEqual(row.columns.map((column) => column.fields), [[], []]);
});

test("designer helpers insert new fields into explicit columns", () => {
  const schema = addLayoutBlockToSchema(createEmptyFormBuilderSchema(), {
    kind: "row",
    sectionId: "section_1",
    position: "end",
    spans: [6, 6]
  });
  const targetColumnId = schema.layout.pages[0].sections[0].rows[0].columns[1].id;

  const result = insertNewFieldAtTarget(schema, "email", { type: "column", columnId: targetColumnId, index: 0 });

  assert.equal(result.field.type, "email");
  assert.equal(result.schema.fields.length, 1);
  assert.deepEqual(result.schema.layout.pages[0].sections[0].rows[0].columns[0].fields, []);
  assert.deepEqual(result.schema.layout.pages[0].sections[0].rows[0].columns[1].fields, [result.field.id]);
});

test("designer helpers move existing fields without duplicates", () => {
  const first = addFieldToSchema(createEmptyFormBuilderSchema(), "text").schema;
  const second = addFieldToSchema(first, "email").schema;
  const targetColumnId = second.layout.pages[0].sections[0].rows[0].columns[0].id;

  const moved = moveFieldToTarget(second, "email", { type: "column", columnId: targetColumnId, index: 0 });
  const placedFieldIds = moved.layout.pages[0].sections[0].rows.flatMap((row) => row.columns.flatMap((column) => column.fields));

  assert.equal(moved.fields.length, 2);
  assert.deepEqual(moved.layout.pages[0].sections[0].rows[0].columns[0].fields, ["email", "text"]);
  assert.equal(placedFieldIds.filter((fieldId) => fieldId === "email").length, 1);
});

test("designer helpers preserve target slot order when reordering fields in the same column", () => {
  const first = addFieldToSchema(createEmptyFormBuilderSchema(), "text").schema;
  const second = addFieldToSchema(first, "email").schema;
  const third = addFieldToSchema(second, "number").schema;
  const targetColumnId = third.layout.pages[0].sections[0].rows[0].columns[0].id;
  const withEmailInTargetColumn = moveFieldToTarget(third, "email", { type: "column", columnId: targetColumnId, index: 1 });
  const withAllFieldsInTargetColumn = moveFieldToTarget(withEmailInTargetColumn, "number", {
    type: "column",
    columnId: targetColumnId,
    index: 2
  });

  const moved = moveFieldToTarget(withAllFieldsInTargetColumn, "text", { type: "column", columnId: targetColumnId, index: 2 });
  const targetColumn = moved.layout.pages[0].sections[0].rows
    .flatMap((row) => row.columns)
    .find((column) => column.id === targetColumnId);

  assert.deepEqual(targetColumn?.fields, ["email", "text", "number"]);
});

test("designer helpers move fields up and down inside the current column", () => {
  const first = addFieldToSchema(createEmptyFormBuilderSchema(), "text").schema;
  const second = addFieldToSchema(first, "email").schema;
  const third = addFieldToSchema(second, "number").schema;
  const targetColumnId = third.layout.pages[0].sections[0].rows[0].columns[0].id;
  const withEmailInTargetColumn = moveFieldToTarget(third, "email", { type: "column", columnId: targetColumnId, index: 1 });
  const withAllFieldsInTargetColumn = moveFieldToTarget(withEmailInTargetColumn, "number", {
    type: "column",
    columnId: targetColumnId,
    index: 2
  });

  const movedUp = moveFieldWithinColumn(withAllFieldsInTargetColumn, "number", "up");
  const movedDown = moveFieldWithinColumn(movedUp, "number", "down");

  assert.deepEqual(movedUp.layout.pages[0].sections[0].rows[0].columns[0].fields, ["text", "number", "email"]);
  assert.deepEqual(movedDown.layout.pages[0].sections[0].rows[0].columns[0].fields, ["text", "email", "number"]);
  assert.deepEqual(moveFieldWithinColumn(movedDown, "text", "up"), movedDown);
  assert.deepEqual(moveFieldWithinColumn(movedDown, "number", "down"), movedDown);
});

test("designer helpers update custom column spans", () => {
  const schema = addFieldToSchema(createEmptyFormBuilderSchema(), "text").schema;
  const columnId = schema.layout.pages[0].sections[0].rows[0].columns[0].id;
  const updated = updateColumnSpan(schema, columnId, { mobile: 3, tablet: 5, desktop: 13 });

  assert.deepEqual(updated.layout.pages[0].sections[0].rows[0].columns[0].span, {
    mobile: 12,
    tablet: 5,
    desktop: 12
  });
});

test("designer helpers resize columns one step while keeping mobile full width", () => {
  const schema = addFieldToSchema(createEmptyFormBuilderSchema(), "text").schema;
  const columnId = schema.layout.pages[0].sections[0].rows[0].columns[0].id;
  const narrowed = resizeColumnSpan(schema, columnId, "shrink");
  const widened = resizeColumnSpan(narrowed, columnId, "grow");

  assert.deepEqual(narrowed.layout.pages[0].sections[0].rows[0].columns[0].span, {
    mobile: 12,
    tablet: 11,
    desktop: 11
  });
  assert.deepEqual(widened.layout.pages[0].sections[0].rows[0].columns[0].span, {
    mobile: 12,
    tablet: 12,
    desktop: 12
  });
});

test("designer helpers add columns beside selected columns without dropping fields", () => {
  const schema = addFieldToSchema(createEmptyFormBuilderSchema(), "text").schema;
  const sourceColumnId = schema.layout.pages[0].sections[0].rows[0].columns[0].id;

  const result = addColumnNearColumn(schema, sourceColumnId, "after");
  const row = result.schema.layout.pages[0].sections[0].rows[0];

  assert.equal(result.column?.id, "col_1");
  assert.deepEqual(row.columns.map((column) => column.id), [sourceColumnId, "col_1"]);
  assert.deepEqual(row.columns.map((column) => column.span.desktop), [6, 6]);
  assert.deepEqual(row.columns.map((column) => column.fields), [["text"], []]);
});

test("designer helpers move columns left and right while preserving fields", () => {
  const schema = addFieldToSchema(createEmptyFormBuilderSchema(), "text").schema;
  const sourceColumnId = schema.layout.pages[0].sections[0].rows[0].columns[0].id;
  const withColumn = addColumnNearColumn(schema, sourceColumnId, "after").schema;

  const movedRight = moveColumn(withColumn, sourceColumnId, "right");
  const movedLeft = moveColumn(movedRight, sourceColumnId, "left");

  assert.deepEqual(movedRight.layout.pages[0].sections[0].rows[0].columns.map((column) => column.fields), [[], ["text"]]);
  assert.deepEqual(movedLeft.layout.pages[0].sections[0].rows[0].columns.map((column) => column.fields), [["text"], []]);
  assert.deepEqual(moveColumn(movedLeft, sourceColumnId, "left"), movedLeft);
});

test("designer helpers report available column actions", () => {
  const schema = addFieldToSchema(createEmptyFormBuilderSchema(), "text").schema;
  const sourceColumnId = schema.layout.pages[0].sections[0].rows[0].columns[0].id;
  const withColumn = addColumnNearColumn(schema, sourceColumnId, "after").schema;
  const row = withColumn.layout.pages[0].sections[0].rows[0];
  const emptyColumnId = row.columns[1].id;

  assert.deepEqual(getColumnActionState(row, sourceColumnId), {
    canBalance: true,
    canDelete: false,
    canMoveLeft: false,
    canMoveRight: true
  });
  assert.deepEqual(getColumnActionState(row, emptyColumnId), {
    canBalance: true,
    canDelete: true,
    canMoveLeft: true,
    canMoveRight: false
  });
});

test("designer helpers delete only empty columns and keep at least one column", () => {
  const schema = addFieldToSchema(createEmptyFormBuilderSchema(), "text").schema;
  const sourceColumnId = schema.layout.pages[0].sections[0].rows[0].columns[0].id;
  const withColumn = addColumnNearColumn(schema, sourceColumnId, "after").schema;
  const emptyColumnId = withColumn.layout.pages[0].sections[0].rows[0].columns[1].id;
  const emptyRowSchema = addLayoutBlockToSchema(createEmptyFormBuilderSchema(), {
    kind: "row",
    sectionId: "section_1",
    position: "end",
    spans: [12]
  });
  const onlyColumnId = emptyRowSchema.layout.pages[0].sections[0].rows[0].columns[0].id;

  const afterPopulatedDelete = deleteColumnIfEmpty(withColumn, sourceColumnId);
  const afterEmptyDelete = deleteColumnIfEmpty(withColumn, emptyColumnId);
  const afterOnlyColumnDelete = deleteColumnIfEmpty(emptyRowSchema, onlyColumnId);

  assert.deepEqual(afterPopulatedDelete, withColumn);
  assert.deepEqual(afterEmptyDelete.layout.pages[0].sections[0].rows[0].columns.map((column) => column.id), [sourceColumnId]);
  assert.deepEqual(afterOnlyColumnDelete, emptyRowSchema);
});

test("designer helpers balance row columns up to four columns", () => {
  const schema = addLayoutBlockToSchema(createEmptyFormBuilderSchema(), {
    kind: "row",
    sectionId: "section_1",
    position: "end",
    spans: [12, 12, 12, 12]
  });
  const rowId = schema.layout.pages[0].sections[0].rows[0].id;

  const balanced = balanceRowColumns(schema, rowId);

  assert.deepEqual(balanced.layout.pages[0].sections[0].rows[0].columns.map((column) => column.span.mobile), [12, 12, 12, 12]);
  assert.deepEqual(balanced.layout.pages[0].sections[0].rows[0].columns.map((column) => column.span.tablet), [3, 3, 3, 3]);
  assert.deepEqual(balanced.layout.pages[0].sections[0].rows[0].columns.map((column) => column.span.desktop), [3, 3, 3, 3]);
});

test("designer helpers update section details without changing layout contents", () => {
  const schema = addFieldToSchema(createEmptyFormBuilderSchema(), "text").schema;

  const updated = updateSectionDetails(schema, "section_1", {
    title: "  Employee intake  ",
    description: "  Used by HR before onboarding.  "
  });
  const clearedDescription = updateSectionDetails(updated, "section_1", { description: "   " });

  assert.equal(updated.layout.pages[0].sections[0].title, "Employee intake");
  assert.equal(updated.layout.pages[0].sections[0].description, "Used by HR before onboarding.");
  assert.deepEqual(updated.layout.pages[0].sections[0].rows, schema.layout.pages[0].sections[0].rows);
  assert.equal(clearedDescription.layout.pages[0].sections[0].description, undefined);
});

test("designer helpers only delete empty layout rows and sections", () => {
  const withField = addFieldToSchema(createEmptyFormBuilderSchema(), "text").schema;
  const populatedRowId = withField.layout.pages[0].sections[0].rows[0].id;
  const withEmptyRow = addLayoutBlockToSchema(withField, { kind: "row", sectionId: "section_1", position: "end", spans: [6, 6] });
  const emptyRowId = withEmptyRow.layout.pages[0].sections[0].rows[1].id;
  const withSecondSection = addLayoutBlockToSchema(withEmptyRow, { kind: "section", sectionId: "section_1", position: "after" });

  const afterPopulatedRowDelete = deleteLayoutRowIfEmpty(withSecondSection, populatedRowId);
  const afterEmptyRowDelete = deleteLayoutRowIfEmpty(withSecondSection, emptyRowId);
  const afterEmptySectionDelete = deleteLayoutSectionIfEmpty(withSecondSection, "section_2");
  const afterLastSectionDelete = deleteLayoutSectionIfEmpty(createEmptyFormBuilderSchema(), "section_1");

  assert.deepEqual(afterPopulatedRowDelete, withSecondSection);
  assert.equal(afterEmptyRowDelete.layout.pages[0].sections[0].rows.some((row) => row.id === emptyRowId), false);
  assert.equal(afterEmptySectionDelete.layout.pages[0].sections.some((section) => section.id === "section_2"), false);
  assert.deepEqual(afterLastSectionDelete, createEmptyFormBuilderSchema());
});

test("designer helpers delete populated sections with their fields while preserving the last section", () => {
  const withText = addFieldToSchema(createEmptyFormBuilderSchema(), "text").schema;
  const withSecondSection = addLayoutBlockToSchema(withText, { kind: "section", sectionId: "section_1", position: "after" });
  const withEmail = insertNewFieldAtTarget(withSecondSection, "email", { type: "section", sectionId: "section_2" }).schema;

  const firstSection = withEmail.layout.pages[0].sections[0];
  const afterDelete = deleteSectionFromSchema(withEmail, "section_1");

  assert.deepEqual(getSectionFieldIds(firstSection), ["text"]);
  assert.deepEqual(afterDelete.fields.map((field) => field.id), ["email"]);
  assert.deepEqual(afterDelete.layout.pages[0].sections.map((section) => section.id), ["section_2"]);
  assert.equal(
    afterDelete.layout.pages[0].sections[0].rows.some((row) => row.columns.some((column) => column.fields.includes("text"))),
    false
  );
  assert.deepEqual(deleteSectionFromSchema(withText, "section_1"), withText);
});

test("designer helpers report layout warnings and remove empty containers", () => {
  const schema = addLayoutBlockToSchema(createEmptyFormBuilderSchema(), {
    kind: "row",
    sectionId: "section_1",
    position: "end",
    spans: [4, 4, 4]
  });

  assert.equal(createDesignerWarningMessages(schema).some((message) => message.includes("empty row")), true);

  const cleaned = removeEmptyLayoutContainers(schema);
  assert.equal(cleaned.layout.pages[0].sections[0].rows.length, 0);
  assert.deepEqual(getFieldDropTargets(cleaned), [{ type: "section", sectionId: "section_1" }]);
});

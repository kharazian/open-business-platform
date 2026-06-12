import assert from "node:assert/strict";
import { test } from "vitest";
import {
  addLayoutBlockToSchema,
  createDesignerWarningMessages,
  deleteLayoutRowIfEmpty,
  deleteLayoutSectionIfEmpty,
  getFieldDropTargets,
  insertNewFieldAtTarget,
  moveFieldToTarget,
  removeEmptyLayoutContainers,
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

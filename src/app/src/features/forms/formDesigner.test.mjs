import assert from "node:assert/strict";
import { test } from "vitest";
import {
  addLayoutBlockToSchema,
  createDesignerWarningMessages,
  getFieldDropTargets,
  insertNewFieldAtTarget,
  moveFieldToTarget,
  removeEmptyLayoutContainers,
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

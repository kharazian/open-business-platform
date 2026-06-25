import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { test } from "vitest";
import {
  addFieldToSchema,
  createEmptyFormBuilderSchema,
  createFormBuilderDraftStorageKey,
  deleteFieldFromSchema,
  fieldTypeDescriptions,
  fieldTypeLabels,
  getFieldLayoutWidth,
  layoutWidthOptions,
  loadFormBuilderDraft,
  saveFormBuilderDraft,
  updateFieldInSchema,
  updateFieldLayoutWidth
} from "./builder.ts";
import { formPreviewContentClassName, formPreviewPanelClassName } from "./builderPreview.ts";
import {
  createFieldParentSelectionItems,
  draftDetailsModalPanelClassName,
  formBuilderCanvasScrollClassName,
  formBuilderSoftDangerButtonClassName,
  formBuilderSoftDangerIconButtonClassName,
  formBuilderSidebarClassName,
  formBuilderWorkspaceClassName
} from "./builderWorkspace.ts";

test("form builder helpers manage field lifecycle, layout widths, and local drafts", () => {
  const emptySchema = createEmptyFormBuilderSchema();

  assert.equal(emptySchema.schemaVersion, 1);
  assert.deepEqual(emptySchema.fields, []);
  assert.equal(emptySchema.layout.pages[0].sections[0].rows.length, 0);
  assert.equal(fieldTypeLabels.text, "Text");
  assert.equal(fieldTypeLabels.radio, "Radio");

  const textResult = addFieldToSchema(emptySchema, "text");
  assert.equal(textResult.field.id, "text");
  assert.equal(textResult.field.label, "Text");
  assert.equal(textResult.schema.fields.length, 1);
  assert.equal(textResult.schema.layout.pages[0].sections[0].rows[0].columns[0].span.desktop, 12);
  assert.deepEqual(textResult.schema.layout.pages[0].sections[0].rows[0].columns[0].fields, ["text"]);
  assert.equal(getFieldLayoutWidth(textResult.schema, "text"), "full");
  assert.deepEqual(layoutWidthOptions.map((option) => option.value), ["full", "half", "third", "twoThirds"]);

  const secondTextResult = addFieldToSchema(textResult.schema, "text");
  assert.equal(secondTextResult.field.id, "text_2");

  const emailResult = addFieldToSchema(textResult.schema, "email");
  const halfTextSchema = updateFieldLayoutWidth(emailResult.schema, "text", "half");
  assert.deepEqual(halfTextSchema.layout.pages[0].sections[0].rows[0].columns[0].span, {
    mobile: 12,
    tablet: 6,
    desktop: 6
  });
  assert.equal(getFieldLayoutWidth(halfTextSchema, "text"), "half");
  assert.equal(halfTextSchema.layout.pages[0].sections[0].rows.length, 2);

  const twoColumnSchema = updateFieldLayoutWidth(halfTextSchema, "email", "half");
  assert.equal(twoColumnSchema.layout.pages[0].sections[0].rows.length, 1);
  assert.deepEqual(
    twoColumnSchema.layout.pages[0].sections[0].rows[0].columns.map((column) => column.fields),
    [["text"], ["email"]]
  );
  assert.equal(getFieldLayoutWidth(twoColumnSchema, "email"), "half");

  const twoThirdsSchema = updateFieldLayoutWidth(twoColumnSchema, "text", "twoThirds");
  const thirdSchema = updateFieldLayoutWidth(twoThirdsSchema, "email", "third");
  assert.deepEqual(
    thirdSchema.layout.pages[0].sections[0].rows[0].columns.map((column) => column.span.desktop),
    [8, 4]
  );
  assert.equal(getFieldLayoutWidth(thirdSchema, "text"), "twoThirds");
  assert.equal(getFieldLayoutWidth(thirdSchema, "email"), "third");

  assert.deepEqual(updateFieldLayoutWidth(thirdSchema, "missing", "half"), thirdSchema);

  const selectResult = addFieldToSchema(emptySchema, "select");
  assert.equal(selectResult.field.options.length, 2);
  assert.equal(selectResult.field.options[0].label, "Option 1");
  assert.equal(selectResult.field.options[0].value, "option_1");

  const updatedTextSchema = updateFieldInSchema(textResult.schema, {
    ...textResult.field,
    label: "  Employee name  ",
    placeholder: "  Jane Cooper  ",
    helpText: "  Legal name  ",
    required: true
  });
  assert.equal(updatedTextSchema.fields[0].label, "Employee name");
  assert.equal(updatedTextSchema.fields[0].placeholder, "Jane Cooper");
  assert.equal(updatedTextSchema.fields[0].helpText, "Legal name");
  assert.equal(updatedTextSchema.fields[0].required, true);

  const updatedSelectSchema = updateFieldInSchema(selectResult.schema, {
    ...selectResult.field,
    options: [{ id: "", label: " High priority ", value: "" }]
  });
  assert.equal(updatedSelectSchema.fields[0].options[0].id, "select_option_1");
  assert.equal(updatedSelectSchema.fields[0].options[0].label, "High priority");
  assert.equal(updatedSelectSchema.fields[0].options[0].value, "high_priority");

  const deletedTextSchema = deleteFieldFromSchema(updatedTextSchema, "text");
  assert.equal(deletedTextSchema.fields.length, 0);
  assert.equal(deletedTextSchema.layout.pages[0].sections[0].rows.length, 0);

  const storageValues = new Map();
  const storage = {
    getItem: (key) => storageValues.get(key) ?? null,
    setItem: (key, value) => storageValues.set(key, value)
  };

  saveFormBuilderDraft("form-1", updatedTextSchema, storage);
  assert.deepEqual(loadFormBuilderDraft("form-1", storage), updatedTextSchema);

  storage.setItem(createFormBuilderDraftStorageKey("form-broken"), "{not json");
  assert.deepEqual(loadFormBuilderDraft("form-broken", storage), createEmptyFormBuilderSchema());
});

test("form builder exposes record lookup field metadata", () => {
  assert.equal(fieldTypeLabels.recordLookup, "Record lookup");
  assert.equal(fieldTypeDescriptions.recordLookup, "Search and select a record from another form");

  const result = addFieldToSchema(createEmptyFormBuilderSchema(), "recordLookup");

  assert.equal(result.field.type, "recordLookup");
  assert.equal(result.field.label, "Record lookup");
  assert.deepEqual(result.field.lookup, {
    sourceType: "form_records",
    sourceFormId: "",
    labelFieldIds: [],
    searchFieldIds: [],
    filters: []
  });
});

test("form builder exposes guided record lookup settings", () => {
  const source = readFileSync(new URL("./pages/FormBuilderPage.tsx", import.meta.url), "utf8");

  assert.equal(source.includes("RecordLookupSettings"), true);
  assert.equal(source.includes("Source form"), true);
  assert.equal(source.includes("Label fields"), true);
  assert.equal(source.includes("Search fields"), true);
  assert.equal(source.includes("Dependent filters"), true);
  assert.equal(source.includes("sourceFormOptions"), true);
  assert.equal(source.includes("sourceFieldOptions"), true);
  assert.equal(source.includes("parentFieldOptions"), true);
  assert.equal(source.includes("Label field ids"), false);
  assert.equal(source.includes("Search field ids"), false);
});

test("form builder preview layout uses the full viewport", () => {
  assert.equal(formPreviewPanelClassName.includes("100dvh"), true);
  assert.equal(formPreviewPanelClassName.includes("100vw"), true);
  assert.equal(formPreviewPanelClassName.includes("!max-w-[calc(100vw-2rem)]"), true);
  assert.equal(formPreviewPanelClassName.includes("max-w-6xl"), false);
  assert.equal(formPreviewPanelClassName.includes("max-h-[90vh]"), false);
  assert.equal(formPreviewContentClassName.includes("100dvh"), true);
  assert.equal(formPreviewContentClassName.includes("overflow-y-auto"), true);
});

test("form builder workspace layout gives sidebars and canvas independent scroll", () => {
  assert.equal(formBuilderWorkspaceClassName.includes("min-h-0"), true);
  assert.equal(formBuilderWorkspaceClassName.includes("xl:grid-cols-[18rem_minmax(0,1fr)_24rem]"), true);
  assert.equal(formBuilderSidebarClassName.includes("min-h-0"), true);
  assert.equal(formBuilderSidebarClassName.includes("xl:sticky"), true);
  assert.equal(formBuilderSidebarClassName.includes("xl:max-h-[calc(100dvh-14rem)]"), true);
  assert.equal(formBuilderSidebarClassName.includes("calc(100dvh-8rem)"), false);
  assert.equal(formBuilderSidebarClassName.includes("xl:overflow-y-auto"), true);
  assert.equal(formBuilderSidebarClassName.includes("xl:overscroll-contain"), true);
  assert.equal(formBuilderSidebarClassName.includes("[scrollbar-width:thin]"), true);
  assert.equal(formBuilderCanvasScrollClassName.includes("min-h-0"), true);
  assert.equal(formBuilderCanvasScrollClassName.includes("xl:max-h-[calc(100dvh-14rem)]"), true);
  assert.equal(formBuilderCanvasScrollClassName.includes("calc(100dvh-8rem)"), false);
  assert.equal(formBuilderCanvasScrollClassName.includes("xl:overflow-y-auto"), true);
  assert.equal(formBuilderCanvasScrollClassName.includes("xl:overscroll-contain"), true);
  assert.equal(draftDetailsModalPanelClassName.includes("max-w-xl"), true);
});

test("form builder destructive actions use a softer danger treatment", () => {
  assert.equal(formBuilderSoftDangerIconButtonClassName.includes("bg-danger-soft"), true);
  assert.equal(formBuilderSoftDangerIconButtonClassName.includes("text-danger"), true);
  assert.equal(formBuilderSoftDangerIconButtonClassName.includes("border-danger/25"), true);
  assert.equal(formBuilderSoftDangerIconButtonClassName.includes("bg-danger text-white"), false);
  assert.equal(formBuilderSoftDangerButtonClassName.includes("bg-danger-soft"), true);
  assert.equal(formBuilderSoftDangerButtonClassName.includes("text-danger"), true);
  assert.equal(formBuilderSoftDangerButtonClassName.includes("border-danger/25"), true);
  assert.equal(formBuilderSoftDangerButtonClassName.includes("bg-danger text-white"), false);
});

test("form builder field cards expose parent layout selection actions", () => {
  const items = createFieldParentSelectionItems({
    columnId: "column-1",
    rowId: "row-1",
    sectionId: "section-1"
  });

  assert.deepEqual(items.map((item) => item.label), ["Select column", "Select row", "Select section"]);
  assert.deepEqual(items.map((item) => item.selection), [
    { type: "column", id: "column-1" },
    { type: "row", id: "row-1" },
    { type: "section", id: "section-1" }
  ]);
});

test("form builder field cards do not render a layout width badge", () => {
  const source = readFileSync(new URL("./pages/FormBuilderPage.tsx", import.meta.url), "utf8");

  assert.equal(source.includes("<Badge>{getLayoutWidthLabel(column)}</Badge>"), false);
});

test("form builder field cards show the type icon beside the label instead of a type badge", () => {
  const source = readFileSync(new URL("./pages/FormBuilderPage.tsx", import.meta.url), "utf8");

  assert.equal(source.includes('<Badge className="gap-1" variant="default">'), false);
  assert.equal(source.includes('<FieldIcon className="mt-0.5 size-4 shrink-0 text-muted-foreground" />'), true);
});

test("form builder field card action icons render only when the field is selected", () => {
  const source = readFileSync(new URL("./pages/FormBuilderPage.tsx", import.meta.url), "utf8");

  assert.equal(/{selected \? \(\s*<div className="flex shrink-0 flex-wrap justify-end gap-2">/.test(source), true);
  assert.equal(source.includes("{selected && parentMenuOpen ? ("), true);
});

test("form builder section delete controls require an empty section", () => {
  const source = readFileSync(new URL("./pages/FormBuilderPage.tsx", import.meta.url), "utf8");

  assert.equal(source.includes('setNotice("Only empty sections can be deleted. Move or delete fields first.");'), true);
  assert.equal(source.includes("const canDelete = pageSectionCount > 1 && isLayoutSectionEmpty(section);"), true);
  assert.equal(source.includes("disabled={page.sections.length <= 1 || !isLayoutSectionEmpty(section)}"), true);
  assert.equal(source.includes("This removes the empty section from the draft."), true);
  assert.equal(source.includes("deletes every field inside it"), false);
});

import { type FormEvent, type ReactNode, useEffect, useMemo, useState } from "react";
import {
  DndContext,
  DragOverlay,
  KeyboardSensor,
  PointerSensor,
  closestCenter,
  useDraggable,
  useDroppable,
  useSensor,
  useSensors,
  type DragEndEvent,
  type DragStartEvent
} from "@dnd-kit/core";
import {
  AlignLeft,
  ArrowLeft,
  ArrowRight,
  CalendarDays,
  CheckSquare,
  CircleDot,
  Eye,
  Hash,
  LayoutPanelLeft,
  List,
  Mail,
  Minus,
  Monitor,
  Phone,
  Plus,
  Rocket,
  Rows3,
  Save,
  Settings2,
  Smartphone,
  SquareSplitHorizontal,
  SquareStack,
  Tablet,
  Type,
  type LucideIcon,
  Trash2
} from "lucide-react";
import { useNavigate, useParams } from "react-router-dom";
import { Alert } from "../../../components/ui/Alert";
import { Badge } from "../../../components/ui/Badge";
import { Button } from "../../../components/ui/Button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "../../../components/ui/Card";
import { Checkbox } from "../../../components/ui/Checkbox";
import { EmptyState } from "../../../components/ui/EmptyState";
import { Input } from "../../../components/ui/Input";
import { Modal } from "../../../components/ui/Modal";
import { PageHeader } from "../../../components/ui/PageHeader";
import { Select } from "../../../components/ui/Select";
import { Textarea } from "../../../components/ui/Textarea";
import { cn } from "../../../lib/cn";
import { getForm, publishForm, updateFormDraft } from "../api";
import {
  addFieldToSchema,
  createEmptyFormBuilderSchema,
  deleteFieldFromSchema,
  fieldTypeDescriptions,
  fieldTypeLabels,
  getFieldLayoutWidth,
  getDefaultFieldValue,
  isChoiceFieldType,
  layoutWidthOptions,
  loadFormBuilderDraft,
  saveFormBuilderDraft,
  updateFieldLayoutWidth,
  updateFieldInSchema
} from "../builder";
import type { LayoutWidthValue } from "../builder";
import { FormRenderer } from "../components/FormRenderer";
import {
  addColumnNearColumn,
  addLayoutBlockToSchema,
  balanceRowColumns,
  createDesignerWarningMessages,
  deleteColumnIfEmpty,
  deleteLayoutRowIfEmpty,
  deleteLayoutSectionIfEmpty,
  insertNewFieldAtTarget,
  isLayoutRowEmpty,
  isLayoutSectionEmpty,
  moveColumn,
  moveFieldToTarget,
  resizeColumnSpan,
  updateColumnSpan,
  updateSectionDetails,
  type DesignerDropTarget,
  type LayoutBlockDrop
} from "../designer";
import { createInitialRecordValues, type FormPreviewSize } from "../renderer";
import {
  formFieldTypes,
  type FormField,
  type FormFieldOption,
  type FormFieldType,
  type FormLayoutColumn,
  type FormLayoutRow,
  type FormLayoutSection,
  type FormRecordValue,
  type FormRecordValues,
  type ValidationError,
  type FormSchema
} from "../types";
import { validateRecordValues } from "../validation";
import { getFormStatusLabel, type FormStatus } from "../drafts";

const fieldTypeOptions = formFieldTypes.map((type) => ({ label: fieldTypeLabels[type], value: type }));
const fieldTypeIcons: Record<FormFieldType, LucideIcon> = {
  text: Type,
  textarea: AlignLeft,
  number: Hash,
  email: Mail,
  phone: Phone,
  date: CalendarDays,
  select: List,
  checkbox: CheckSquare,
  radio: CircleDot
};
const layoutWidthSelectOptions = layoutWidthOptions.map(({ label, value }) => ({ label, value }));
const spanSelectOptions = Array.from({ length: 12 }, (_, index) => {
  const span = index + 1;
  return { label: `${span} / 12`, value: String(span) };
});
const tabletSpanClasses: Record<number, string> = {
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
const desktopSpanClasses: Record<number, string> = {
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

type DesignerDragData =
  | { kind: "new_field"; fieldType: FormFieldType; label: string }
  | { kind: "existing_field"; fieldId: string; label: string }
  | { kind: "layout_block"; template: LayoutBlockTemplate; label: string };

type LayoutBlockTemplate = { kind: "section" } | { kind: "row"; spans: number[] };

type DesignerDropData = {
  kind: "canvas_target";
  fieldTarget: DesignerDropTarget;
  sectionId: string;
  rowId?: string;
};

type DesignerSelection =
  | { type: "field"; id: string }
  | { type: "section"; id: string }
  | { type: "row"; id: string }
  | { type: "column"; id: string };

type SectionContext = {
  pageSectionCount: number;
  section: FormLayoutSection;
};

type RowContext = {
  section: FormLayoutSection;
  row: FormLayoutRow;
};

type ColumnContext = RowContext & {
  column: FormLayoutColumn;
};

const layoutBlocks = [
  { icon: SquareStack, label: "Section", template: { kind: "section" } },
  { icon: LayoutPanelLeft, label: "One column", template: { kind: "row", spans: [12] } },
  { icon: SquareSplitHorizontal, label: "Two columns", template: { kind: "row", spans: [6, 6] } },
  { icon: Rows3, label: "Three columns", template: { kind: "row", spans: [4, 4, 4] } }
] as const satisfies ReadonlyArray<{ icon: typeof SquareStack; label: string; template: LayoutBlockTemplate }>;

export function FormBuilderPage() {
  const { formId } = useParams<{ formId: string }>();
  const navigate = useNavigate();
  const resolvedFormId = formId ?? "unknown";
  const [schema, setSchema] = useState<FormSchema>(() =>
    formId ? loadFormBuilderDraft(formId) : createEmptyFormBuilderSchema()
  );
  const [selection, setSelection] = useState<DesignerSelection | null>(() =>
    schema.fields[0] ? { type: "field", id: schema.fields[0].id } : null
  );
  const [activeDrag, setActiveDrag] = useState<DesignerDragData | null>(null);
  const [formName, setFormName] = useState("Form draft");
  const [formDescription, setFormDescription] = useState("");
  const [formNameError, setFormNameError] = useState<string | undefined>();
  const [formStatus, setFormStatus] = useState<FormStatus>("draft");
  const [loadingForm, setLoadingForm] = useState(true);
  const [savingDraft, setSavingDraft] = useState(false);
  const [publishing, setPublishing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [previewOpen, setPreviewOpen] = useState(false);
  const [previewSize, setPreviewSize] = useState<FormPreviewSize>("desktop");
  const [previewValues, setPreviewValues] = useState<FormRecordValues>(() => createInitialRecordValues(schema));
  const [previewErrors, setPreviewErrors] = useState<ValidationError[]>([]);
  const [previewNotice, setPreviewNotice] = useState<string | null>(null);
  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 1 } }),
    useSensor(KeyboardSensor)
  );

  useEffect(() => {
    if (!formId) return;

    setSchema(loadFormBuilderDraft(formId));
  }, [formId]);

  useEffect(() => {
    setSelection((current) => {
      if (current?.type === "field" && schema.fields.some((field) => field.id === current.id)) return current;
      if (current?.type === "section" && schema.layout.pages.some((page) => page.sections.some((section) => section.id === current.id))) return current;
      if (
        current?.type === "row" &&
        schema.layout.pages.some((page) => page.sections.some((section) => section.rows.some((row) => row.id === current.id)))
      ) {
        return current;
      }
      if (
        current?.type === "column" &&
        schema.layout.pages.some((page) =>
          page.sections.some((section) => section.rows.some((row) => row.columns.some((column) => column.id === current.id)))
        )
      ) {
        return current;
      }

      return schema.fields[0] ? { type: "field", id: schema.fields[0].id } : null;
    });
  }, [schema]);

  useEffect(() => {
    let active = true;
    setLoadingForm(true);
    setError(null);

    getForm(resolvedFormId)
      .then((form) => {
        if (!active) return;
        setFormName(form.name);
        setFormDescription(form.description ?? "");
        setFormNameError(undefined);
        setFormStatus(form.status);

        if (form.draftSchema) {
          setSchema(form.draftSchema);
          saveFormBuilderDraft(resolvedFormId, form.draftSchema);
        }
      })
      .catch((caught: unknown) => {
        if (!active) return;
        setFormName(`Form ${resolvedFormId}`);
        setFormDescription("");
        setError(getErrorMessage(caught));
      })
      .finally(() => {
        if (active) setLoadingForm(false);
      });

    return () => {
      active = false;
    };
  }, [resolvedFormId]);

  const selectedFieldId = selection?.type === "field" ? selection.id : null;
  const selectedField = useMemo(
    () => schema.fields.find((field) => field.id === selectedFieldId) ?? null,
    [schema.fields, selectedFieldId]
  );
  const selectedFieldLayoutWidth = selectedField ? getFieldLayoutWidth(schema, selectedField.id) : null;
  const selectedSectionContext = useMemo(
    () => (selection?.type === "section" ? findSectionContext(schema, selection.id) : null),
    [schema, selection]
  );
  const selectedRowContext = useMemo(
    () => (selection?.type === "row" ? findRowContext(schema, selection.id) : null),
    [schema, selection]
  );
  const selectedColumnContext = useMemo(
    () => (selection?.type === "column" ? findColumnContext(schema, selection.id) : null),
    [schema, selection]
  );

  function handleAddField(type: FormFieldType) {
    const result = addFieldToSchema(schema, type);
    setSchema(result.schema);
    setSelection({ type: "field", id: result.field.id });
    setNotice(null);
  }

  function handleAddLayoutBlock(template: LayoutBlockTemplate) {
    const firstSectionId = schema.layout.pages[0]?.sections[0]?.id;
    if (!firstSectionId) return;

    const block: LayoutBlockDrop =
      template.kind === "section"
        ? { kind: "section", sectionId: firstSectionId, position: "after" }
        : { kind: "row", sectionId: firstSectionId, position: "end", spans: template.spans };
    setSchema((currentSchema) => addLayoutBlockToSchema(currentSchema, block));
    setNotice(null);
  }

  function handleDragStart(event: DragStartEvent) {
    const data = event.active.data.current as DesignerDragData | undefined;
    setActiveDrag(data ?? null);
  }

  function handleDragEnd(event: DragEndEvent) {
    const dragData = event.active.data.current as DesignerDragData | undefined;
    const dropData = event.over?.data.current as DesignerDropData | undefined;
    setActiveDrag(null);

    if (!dragData || !dropData) return;

    if (dragData.kind === "layout_block") {
      const block: LayoutBlockDrop =
        dragData.template.kind === "section"
          ? { kind: "section", sectionId: dropData.sectionId, position: "after" }
          : {
              kind: "row",
              sectionId: dropData.sectionId,
              rowId: dropData.rowId,
              position: dropData.rowId ? "after" : "end",
              spans: dragData.template.spans
            };
      setSchema((currentSchema) => addLayoutBlockToSchema(currentSchema, block));
      setNotice(null);
      return;
    }

    if (dragData.kind === "new_field") {
      const result = insertNewFieldAtTarget(schema, dragData.fieldType, dropData.fieldTarget);
      setSchema(result.schema);
      setSelection({ type: "field", id: result.field.id });
      setNotice(null);
      return;
    }

    setSchema((currentSchema) => moveFieldToTarget(currentSchema, dragData.fieldId, dropData.fieldTarget));
    setSelection({ type: "field", id: dragData.fieldId });
    setNotice(null);
  }

  function handleUpdateField(field: FormField) {
    setSchema((currentSchema) => updateFieldInSchema(currentSchema, field));
    setNotice(null);
  }

  function handleDeleteField() {
    if (!selectedField) return;

    setSchema((currentSchema) => deleteFieldFromSchema(currentSchema, selectedField.id));
    setNotice(null);
  }

  function handleUpdateFieldLayoutWidth(fieldId: string, width: LayoutWidthValue) {
    setSchema((currentSchema) => updateFieldLayoutWidth(currentSchema, fieldId, width));
    setNotice(null);
  }

  function handleUpdateSectionDetails(sectionId: string, patch: Pick<FormLayoutSection, "title" | "description">) {
    setSchema((currentSchema) => updateSectionDetails(currentSchema, sectionId, patch));
    setNotice(null);
  }

  function handleUpdateColumnSpan(columnId: string, span: FormLayoutColumn["span"]) {
    setSchema((currentSchema) => updateColumnSpan(currentSchema, columnId, span));
    setNotice(null);
  }

  function handleResizeColumn(columnId: string, direction: "grow" | "shrink") {
    setSchema((currentSchema) => resizeColumnSpan(currentSchema, columnId, direction));
    setSelection({ type: "column", id: columnId });
    setNotice(null);
  }

  function handleAddColumn(columnId: string, position: "before" | "after") {
    const result = addColumnNearColumn(schema, columnId, position);
    setSchema(result.schema);

    if (result.column) {
      setSelection({ type: "column", id: result.column.id });
    }

    setNotice(null);
  }

  function handleMoveColumn(columnId: string, direction: "left" | "right") {
    setSchema((currentSchema) => moveColumn(currentSchema, columnId, direction));
    setSelection({ type: "column", id: columnId });
    setNotice(null);
  }

  function handleDeleteEmptyColumn(columnId: string) {
    const fallbackRowId = selectedColumnContext?.row.id;
    setSchema((currentSchema) => deleteColumnIfEmpty(currentSchema, columnId));

    if (fallbackRowId) {
      setSelection({ type: "row", id: fallbackRowId });
    }

    setNotice(null);
  }

  function handleBalanceRowColumns(rowId: string) {
    setSchema((currentSchema) => balanceRowColumns(currentSchema, rowId));
    setNotice(null);
  }

  function handleAddRow(sectionId: string, rowId: string | undefined, position: LayoutBlockDrop["position"]) {
    setSchema((currentSchema) =>
      addLayoutBlockToSchema(currentSchema, {
        kind: "row",
        sectionId,
        rowId,
        position,
        spans: [12]
      })
    );
    setNotice(null);
  }

  function handleDeleteEmptyRow(rowId: string) {
    setSchema((currentSchema) => deleteLayoutRowIfEmpty(currentSchema, rowId));
    setNotice(null);
  }

  function handleDeleteEmptySection(sectionId: string) {
    setSchema((currentSchema) => deleteLayoutSectionIfEmpty(currentSchema, sectionId));
    setNotice(null);
  }

  async function handleSaveDraft() {
    setError(null);
    setNotice(null);
    const request = createDraftUpdateRequest();

    if (!request) return;

    setSavingDraft(true);

    try {
      const form = await updateFormDraft(resolvedFormId, request);
      setFormName(form.name);
      setFormDescription(form.description ?? "");
      setFormNameError(undefined);
      setFormStatus(form.status);

      if (form.draftSchema) {
        setSchema(form.draftSchema);
        saveFormBuilderDraft(resolvedFormId, form.draftSchema);
      } else {
        saveFormBuilderDraft(resolvedFormId, schema);
      }

      setNotice("Draft saved to backend. Recovery cache updated.");
    } catch (caught) {
      saveFormBuilderDraft(resolvedFormId, schema);
      setError(getErrorMessage(caught));
      setNotice("Backend save failed. Draft saved locally as a recovery cache.");
    } finally {
      setSavingDraft(false);
    }
  }

  async function handlePublish() {
    setError(null);
    setNotice(null);
    const request = createDraftUpdateRequest();

    if (!request) return;

    setPublishing(true);

    try {
      const savedForm = await updateFormDraft(resolvedFormId, request);
      const schemaForCache = savedForm.draftSchema ?? schema;
      saveFormBuilderDraft(resolvedFormId, schemaForCache);

      const response = await publishForm(resolvedFormId);
      setFormName(response.form.name);
      setFormDescription(response.form.description ?? "");
      setFormNameError(undefined);
      setFormStatus(response.form.status);

      if (response.form.draftSchema) {
        setSchema(response.form.draftSchema);
        saveFormBuilderDraft(resolvedFormId, response.form.draftSchema);
      }

      setNotice(`Published version ${response.version.versionNumber}.`);
    } catch (caught) {
      saveFormBuilderDraft(resolvedFormId, schema);
      setError(getErrorMessage(caught));
      setNotice("Publish failed. Draft saved locally as a recovery cache.");
    } finally {
      setPublishing(false);
    }
  }

  function createDraftUpdateRequest() {
    const name = formName.trim();

    if (!name) {
      setFormNameError("Form name is required.");
      setError("Form name is required.");
      return null;
    }

    return { name, description: formDescription, schema };
  }

  function handleFormNameChange(value: string) {
    setFormName(value);
    setNotice(null);

    if (formNameError) {
      setFormNameError(undefined);
      setError(null);
    }
  }

  function handleFormDescriptionChange(value: string) {
    setFormDescription(value);
    setNotice(null);
  }

  function handleOpenPreview() {
    setPreviewValues(createInitialRecordValues(schema));
    setPreviewErrors([]);
    setPreviewNotice(null);
    setPreviewOpen(true);
  }

  function handlePreviewValueChange(fieldId: string, value: FormRecordValue) {
    setPreviewValues((currentValues) => ({ ...currentValues, [fieldId]: value }));
    setPreviewErrors((currentErrors) => currentErrors.filter((validationError) => validationError.path !== `values.${fieldId}`));
    setPreviewNotice(null);
  }

  function handleValidatePreview() {
    const result = validateRecordValues(schema, previewValues);
    setPreviewErrors(result.errors);
    setPreviewNotice(result.valid ? "Preview values pass validation." : null);
  }

  return (
    <div className="grid gap-6">
      <PageHeader
        eyebrow="Form builder"
        title={loadingForm ? "Loading form..." : formName}
        description="Edit backend-owned draft fields and responsive layout before preview and publishing."
        actions={
          <div className="flex flex-wrap gap-2">
            <Badge variant={formStatus === "published" ? "success" : formStatus === "archived" ? "danger" : "warning"}>
              {getFormStatusLabel(formStatus)}
            </Badge>
            <Button onClick={() => navigate("/forms")} variant="outline">
              <ArrowLeft className="size-4" />
              Forms
            </Button>
            <Button onClick={handleOpenPreview} variant="outline">
              <Eye className="size-4" />
              Preview
            </Button>
            <Button disabled={savingDraft || publishing} onClick={handleSaveDraft}>
              <Save className="size-4" />
              {savingDraft ? "Saving..." : "Save draft"}
            </Button>
            <Button disabled={savingDraft || publishing} onClick={handlePublish} variant="secondary">
              <Rocket className="size-4" />
              {publishing ? "Publishing..." : "Publish"}
            </Button>
          </div>
        }
      />

      {error ? <Alert title="Form builder">{error}</Alert> : null}
      {notice ? (
        <div className="rounded-xl border border-success/40 bg-success/10 px-4 py-3 text-sm font-semibold text-success">
          {notice}
        </div>
      ) : null}

      <DndContext collisionDetection={closestCenter} onDragEnd={handleDragEnd} onDragStart={handleDragStart} sensors={sensors}>
        <div className="grid gap-4 xl:grid-cols-[18rem_minmax(0,1fr)_24rem]">
          <div className="grid gap-4 self-start">
            <DraftMetadataSettings
              description={formDescription}
              disabled={loadingForm || savingDraft || publishing}
              name={formName}
              nameError={formNameError}
              onDescriptionChange={handleFormDescriptionChange}
              onNameChange={handleFormNameChange}
            />
            <FieldPalette onAddField={handleAddField} />
            <LayoutBlockPalette onAddLayoutBlock={handleAddLayoutBlock} />
          </div>
          <BuilderCanvas schema={schema} selected={selection} onResizeColumn={handleResizeColumn} onSelect={setSelection} />
          <BuilderSettings
            columnContext={selectedColumnContext}
            field={selectedField}
            layoutWidth={selectedFieldLayoutWidth}
            rowContext={selectedRowContext}
            sectionContext={selectedSectionContext}
            selection={selection}
            onAddRow={handleAddRow}
            onAddColumn={handleAddColumn}
            onBalanceRowColumns={handleBalanceRowColumns}
            onChangeColumnSpan={handleUpdateColumnSpan}
            onChangeField={handleUpdateField}
            onChangeFieldLayoutWidth={handleUpdateFieldLayoutWidth}
            onChangeSection={handleUpdateSectionDetails}
            onDeleteEmptyColumn={handleDeleteEmptyColumn}
            onDeleteEmptyRow={handleDeleteEmptyRow}
            onDeleteEmptySection={handleDeleteEmptySection}
            onDeleteField={handleDeleteField}
            onMoveColumn={handleMoveColumn}
          />
        </div>
        <DragOverlay>{activeDrag ? <DragOverlayCard label={activeDrag.label} /> : null}</DragOverlay>
      </DndContext>

      <Modal
        description="Render the current local draft with the shared V1 form renderer."
        onClose={() => setPreviewOpen(false)}
        open={previewOpen}
        panelClassName="max-h-[90vh] max-w-6xl overflow-hidden"
        title={`${formName} preview`}
      >
        <div className="grid max-h-[70vh] gap-4 overflow-y-auto pr-1">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <FormPreviewSizeSelector onChange={setPreviewSize} value={previewSize} />
            <Badge>{schema.fields.length} fields</Badge>
          </div>
          {previewNotice ? (
            <div className="rounded-xl border border-success/40 bg-success/10 px-4 py-3 text-sm font-semibold text-success">
              {previewNotice}
            </div>
          ) : previewErrors.length > 0 ? (
            <Alert title="Preview validation">Fix the highlighted fields before this form can be submitted.</Alert>
          ) : null}
          <div
            className={cn(
              "mx-auto w-full rounded-xl border border-border bg-background p-4 transition-all",
              previewSize === "mobile" ? "max-w-sm" : previewSize === "tablet" ? "max-w-3xl" : "max-w-none"
            )}
          >
            <FormRenderer
              errors={previewErrors}
              onChange={handlePreviewValueChange}
              onSubmit={handleValidatePreview}
              previewSize={previewSize}
              schema={schema}
              submitLabel="Validate preview"
              values={previewValues}
            />
          </div>
        </div>
      </Modal>
    </div>
  );
}

function DraftMetadataSettings({
  description,
  disabled,
  name,
  nameError,
  onDescriptionChange,
  onNameChange
}: {
  description: string;
  disabled: boolean;
  name: string;
  nameError?: string;
  onDescriptionChange: (value: string) => void;
  onNameChange: (value: string) => void;
}) {
  return (
    <Card>
      <CardHeader>
        <CardTitle>Draft details</CardTitle>
        <CardDescription>Name and description.</CardDescription>
      </CardHeader>
      <CardContent>
        <form className="grid gap-4" onSubmit={preventSubmit}>
          <Input
            disabled={disabled}
            error={nameError}
            label="Form name"
            onChange={(event) => onNameChange(event.target.value)}
            value={name}
          />
          <Textarea
            disabled={disabled}
            label="Description"
            onChange={(event) => onDescriptionChange(event.target.value)}
            value={description}
          />
        </form>
      </CardContent>
    </Card>
  );
}

function FormPreviewSizeSelector({
  onChange,
  value
}: {
  onChange: (value: FormPreviewSize) => void;
  value: FormPreviewSize;
}) {
  const options: Array<{ icon: typeof Smartphone; label: string; value: FormPreviewSize }> = [
    { icon: Smartphone, label: "Mobile", value: "mobile" },
    { icon: Tablet, label: "Tablet", value: "tablet" },
    { icon: Monitor, label: "Desktop", value: "desktop" }
  ];

  return (
    <div className="flex flex-wrap gap-2" aria-label="Preview size">
      {options.map((option) => {
        const Icon = option.icon;

        return (
          <Button
            aria-pressed={value === option.value}
            key={option.value}
            onClick={() => onChange(option.value)}
            size="sm"
            variant={value === option.value ? "primary" : "outline"}
          >
            <Icon className="size-4" />
            {option.label}
          </Button>
        );
      })}
    </div>
  );
}

function FieldPalette({ onAddField }: { onAddField: (type: FormFieldType) => void }) {
  return (
    <Card className="self-start">
      <CardHeader>
        <CardTitle>Fields</CardTitle>
        <CardDescription>V1 field palette.</CardDescription>
      </CardHeader>
      <CardContent className="grid grid-cols-3 gap-2">
        {fieldTypeOptions.map((fieldType) => (
          <DraggableFieldPaletteItem fieldType={fieldType} key={fieldType.value} onAddField={onAddField} />
        ))}
      </CardContent>
    </Card>
  );
}

function DraggableFieldPaletteItem({
  fieldType,
  onAddField
}: {
  fieldType: { label: string; value: FormFieldType };
  onAddField: (type: FormFieldType) => void;
}) {
  const Icon = fieldTypeIcons[fieldType.value];
  const { attributes, listeners, setNodeRef, isDragging } = useDraggable({
    id: `palette-field-${fieldType.value}`,
    data: { kind: "new_field", fieldType: fieldType.value, label: fieldType.label } satisfies DesignerDragData
  });

  return (
    <button
      aria-label={`Add ${fieldType.label} field`}
      className={cn(
        "grid min-h-20 place-items-center gap-1 rounded-xl border border-border bg-card/80 px-2 py-3 text-center transition hover:border-primary/50 hover:bg-muted",
        isDragging ? "opacity-40" : ""
      )}
      ref={setNodeRef}
      title={fieldTypeDescriptions[fieldType.value]}
      type="button"
      onClick={() => onAddField(fieldType.value)}
      {...listeners}
      {...attributes}
    >
      <Icon className="size-5 text-muted-foreground" />
      <span className="text-xs font-bold text-foreground">{fieldType.label}</span>
    </button>
  );
}

function LayoutBlockPalette({ onAddLayoutBlock }: { onAddLayoutBlock: (template: LayoutBlockTemplate) => void }) {
  return (
    <Card className="self-start">
      <CardHeader>
        <CardTitle>Layout</CardTitle>
        <CardDescription>Drop structured blocks onto the canvas.</CardDescription>
      </CardHeader>
      <CardContent className="grid gap-2">
        {layoutBlocks.map((item) => (
          <DraggableLayoutBlock item={item} key={item.label} onAddLayoutBlock={onAddLayoutBlock} />
        ))}
      </CardContent>
    </Card>
  );
}

function DraggableLayoutBlock({
  item,
  onAddLayoutBlock
}: {
  item: (typeof layoutBlocks)[number];
  onAddLayoutBlock: (template: LayoutBlockTemplate) => void;
}) {
  const Icon = item.icon;
  const { attributes, listeners, setNodeRef, isDragging } = useDraggable({
    id: `layout-${item.label}`,
    data: { kind: "layout_block", template: item.template, label: item.label } satisfies DesignerDragData
  });

  return (
    <button
      className={cn(
        "flex min-h-11 items-center gap-3 rounded-xl border border-border bg-card/80 px-3 py-2 text-left text-sm font-bold transition hover:border-primary/50 hover:bg-muted",
        isDragging ? "opacity-60" : ""
      )}
      ref={setNodeRef}
      type="button"
      onClick={() => onAddLayoutBlock(item.template)}
      {...listeners}
      {...attributes}
    >
      <Icon className="size-4 text-muted-foreground" />
      {item.label}
    </button>
  );
}

function BuilderCanvas({
  schema,
  selected,
  onResizeColumn,
  onSelect
}: {
  schema: FormSchema;
  selected: DesignerSelection | null;
  onResizeColumn: (columnId: string, direction: "grow" | "shrink") => void;
  onSelect: (selection: DesignerSelection) => void;
}) {
  const fieldsById = new Map(schema.fields.map((field) => [field.id, field]));
  const warnings = createDesignerWarningMessages(schema);

  return (
    <Card className="min-h-[36rem]">
      <CardHeader>
        <div className="flex items-start justify-between gap-3">
          <div>
            <CardTitle>Canvas</CardTitle>
            <CardDescription>Responsive 12-column layout.</CardDescription>
          </div>
          <Badge>{schema.fields.length} fields</Badge>
        </div>
      </CardHeader>
      <CardContent>
        <div className="space-y-5">
          {warnings.length > 0 ? (
            <div className="rounded-xl border border-warning/40 bg-warning/10 px-4 py-3 text-sm font-semibold text-warning">
              {warnings[0]}
            </div>
          ) : null}
          {schema.layout.pages.map((page) => (
            <div className="space-y-5" key={page.id}>
              {page.sections.map((section) => (
                <section
                  className={cn(
                    "rounded-xl border bg-muted/20 p-4 transition",
                    selected?.type === "section" && selected.id === section.id ? "border-primary ring-4 ring-primary/10" : "border-border"
                  )}
                  key={section.id}
                  onClick={() => onSelect({ type: "section", id: section.id })}
                >
                  <div className="mb-4">
                    <h2 className="text-sm font-black uppercase tracking-normal text-foreground">{section.title ?? "Section"}</h2>
                    {section.description ? <p className="mt-1 text-sm text-muted-foreground">{section.description}</p> : null}
                  </div>
                  <DroppableSectionBody sectionId={section.id}>
                    {section.rows.length > 0 ? (
                      section.rows.map((row) => (
                        <DroppableRow
                          key={row.id}
                          onSelect={onSelect}
                          row={row}
                          sectionId={section.id}
                          selected={selected?.type === "row" && selected.id === row.id}
                        >
                          {row.columns.map((column) => {
                            const columnFields = column.fields
                              .map((fieldId) => fieldsById.get(fieldId))
                              .filter((field): field is FormField => Boolean(field));

                            return (
                              <div className={cn("min-w-0", getColumnSpanClass(column))} key={column.id}>
                                <DroppableColumn
                                  column={column}
                                  onResizeColumn={onResizeColumn}
                                  onSelect={onSelect}
                                  rowId={row.id}
                                  sectionId={section.id}
                                  selected={selected?.type === "column" && selected.id === column.id}
                                >
                                  <div className="grid gap-3">
                                    {columnFields.map((field, fieldIndex) => (
                                      <DroppableFieldSlot
                                        column={column}
                                        fieldIndex={fieldIndex}
                                        key={field.id}
                                        rowId={row.id}
                                        sectionId={section.id}
                                      >
                                        <FieldCanvasCard
                                          column={column}
                                          field={field}
                                          onSelect={(selection) => onSelect(selection)}
                                          selected={selected?.type === "field" && selected.id === field.id}
                                        />
                                      </DroppableFieldSlot>
                                    ))}
                                  </div>
                                </DroppableColumn>
                              </div>
                            );
                          })}
                        </DroppableRow>
                      ))
                    ) : (
                      <div className="rounded-xl border border-dashed border-border bg-card/60 p-6 text-center text-sm font-semibold text-muted-foreground">
                        Drop a field or layout block here.
                      </div>
                    )}
                  </DroppableSectionBody>
                </section>
              ))}
            </div>
          ))}
        </div>
      </CardContent>
    </Card>
  );
}

function DroppableSectionBody({ sectionId, children }: { sectionId: string; children: ReactNode }) {
  const { isOver, setNodeRef } = useDroppable({
    id: `section-drop-${sectionId}`,
    data: {
      kind: "canvas_target",
      sectionId,
      fieldTarget: { type: "section", sectionId }
    } satisfies DesignerDropData
  });

  return (
    <div ref={setNodeRef} className={cn("space-y-3 rounded-lg transition", isOver ? "bg-primary/5 ring-2 ring-primary/30" : "")}>
      {children}
    </div>
  );
}

function DroppableRow({
  row,
  sectionId,
  selected,
  onSelect,
  children
}: {
  row: FormLayoutRow;
  sectionId: string;
  selected: boolean;
  onSelect: (selection: DesignerSelection) => void;
  children: ReactNode;
}) {
  const { isOver, setNodeRef } = useDroppable({
    id: `row-drop-${row.id}`,
    data: {
      kind: "canvas_target",
      sectionId,
      rowId: row.id,
      fieldTarget: { type: "row", rowId: row.id, index: row.columns.length }
    } satisfies DesignerDropData
  });

  return (
    <div
      className={cn(
        "grid gap-3 rounded-xl transition md:grid-cols-12",
        selected ? "ring-2 ring-primary/30" : "",
        isOver ? "bg-primary/5 ring-2 ring-primary/30" : ""
      )}
      onClick={(event) => {
        event.stopPropagation();
        onSelect({ type: "row", id: row.id });
      }}
      ref={setNodeRef}
    >
      {children}
    </div>
  );
}

function DroppableColumn({
  column,
  rowId,
  sectionId,
  selected,
  onResizeColumn,
  onSelect,
  children
}: {
  column: FormLayoutColumn;
  rowId: string;
  sectionId: string;
  selected: boolean;
  onResizeColumn: (columnId: string, direction: "grow" | "shrink") => void;
  onSelect: (selection: DesignerSelection) => void;
  children: ReactNode;
}) {
  const { isOver, setNodeRef } = useDroppable({
    id: `column-drop-${column.id}`,
    data: {
      kind: "canvas_target",
      sectionId,
      rowId,
      fieldTarget: { type: "column", columnId: column.id, index: column.fields.length }
    } satisfies DesignerDropData
  });

  return (
    <div
      className={cn(
        "relative min-h-16 rounded-xl border border-dashed p-2 transition",
        selected ? "pt-12" : "",
        selected ? "border-primary ring-2 ring-primary/20" : "border-border/80",
        isOver ? "border-primary bg-primary/5" : ""
      )}
      onClick={(event) => {
        event.stopPropagation();
        onSelect({ type: "column", id: column.id });
      }}
      ref={setNodeRef}
    >
      {selected ? (
        <div className="absolute right-2 top-2 z-10 flex gap-1 rounded-lg border border-border bg-card/95 p-1 shadow-soft">
          <Button
            aria-label="Shrink column"
            className="size-8"
            onClick={(event) => {
              event.stopPropagation();
              onResizeColumn(column.id, "shrink");
            }}
            size="icon"
            title="Shrink column"
            variant="ghost"
          >
            <Minus className="size-4" />
          </Button>
          <Button
            aria-label="Grow column"
            className="size-8"
            onClick={(event) => {
              event.stopPropagation();
              onResizeColumn(column.id, "grow");
            }}
            size="icon"
            title="Grow column"
            variant="ghost"
          >
            <Plus className="size-4" />
          </Button>
        </div>
      ) : null}
      {children}
      {column.fields.length === 0 ? <p className="px-2 py-4 text-center text-xs font-semibold text-muted-foreground">Drop field</p> : null}
    </div>
  );
}

function DroppableFieldSlot({
  column,
  rowId,
  sectionId,
  fieldIndex,
  children
}: {
  column: FormLayoutColumn;
  rowId: string;
  sectionId: string;
  fieldIndex: number;
  children: ReactNode;
}) {
  const { isOver, setNodeRef } = useDroppable({
    id: `field-slot-${column.id}-${fieldIndex}`,
    data: {
      kind: "canvas_target",
      sectionId,
      rowId,
      fieldTarget: { type: "column", columnId: column.id, index: fieldIndex }
    } satisfies DesignerDropData
  });

  return (
    <div className={cn("rounded-xl transition", isOver ? "ring-2 ring-primary/40 ring-offset-2 ring-offset-background" : "")} ref={setNodeRef}>
      {children}
    </div>
  );
}

function FieldCanvasCard({
  column,
  field,
  onSelect,
  selected
}: {
  column: FormLayoutColumn;
  field: FormField;
  onSelect: (selection: DesignerSelection) => void;
  selected: boolean;
}) {
  const FieldIcon = fieldTypeIcons[field.type];
  const { attributes, listeners, setNodeRef, isDragging } = useDraggable({
    id: `field-${field.id}`,
    data: { kind: "existing_field", fieldId: field.id, label: field.label } satisfies DesignerDragData
  });

  return (
    <button
      className={cn(
        "w-full rounded-xl border bg-card/90 p-4 text-left transition",
        selected ? "border-primary shadow-lifted ring-4 ring-primary/10" : "border-border hover:border-primary/40 hover:bg-muted/50",
        isDragging ? "opacity-60" : ""
      )}
      ref={setNodeRef}
      type="button"
      onClick={(event) => {
        event.stopPropagation();
        onSelect({ type: "field", id: field.id });
      }}
      {...listeners}
      {...attributes}
    >
      <div className="mb-3 flex items-start justify-between gap-3">
        <div>
          <div className="flex flex-wrap items-center gap-2">
            <p className="font-bold text-foreground">{field.label}</p>
            {field.required ? <Badge variant="warning">Required</Badge> : null}
          </div>
          {field.helpText ? <p className="mt-1 text-sm text-muted-foreground">{field.helpText}</p> : null}
        </div>
        <div className="flex shrink-0 flex-wrap justify-end gap-2">
          <Badge className="gap-1" variant="default">
            <FieldIcon className="size-3" />
            {fieldTypeLabels[field.type]}
          </Badge>
          <Badge>{getLayoutWidthLabel(column)}</Badge>
        </div>
      </div>
      <FieldPreview field={field} />
    </button>
  );
}

function FieldPreview({ field }: { field: FormField }) {
  const controlClass = "min-h-10 w-full rounded-xl border border-border bg-muted/50 px-3 text-sm text-muted-foreground";

  if (field.type === "textarea") {
    return <textarea className={cn(controlClass, "min-h-24 py-3")} disabled placeholder={field.placeholder} />;
  }

  if (field.type === "select") {
    return (
      <select className={controlClass} disabled>
        {(field.options ?? []).map((option) => (
          <option key={option.id}>{option.label}</option>
        ))}
      </select>
    );
  }

  if (field.type === "radio") {
    return (
      <div className="grid gap-2">
        {(field.options ?? []).map((option) => (
          <label className="flex items-center gap-2 text-sm font-semibold text-muted-foreground" key={option.id}>
            <input disabled name={field.id} type="radio" />
            {option.label}
          </label>
        ))}
      </div>
    );
  }

  if (field.type === "checkbox") {
    return (
      <label className="flex items-center gap-2 text-sm font-semibold text-muted-foreground">
        <input checked={Boolean(getDefaultFieldValue(field))} disabled readOnly type="checkbox" />
        {field.placeholder || field.label}
      </label>
    );
  }

  return <input className={controlClass} disabled placeholder={field.placeholder} type={getInputType(field.type)} />;
}

function DragOverlayCard({ label }: { label: string }) {
  return (
    <div className="rounded-xl border border-primary bg-card px-4 py-3 text-sm font-bold text-foreground shadow-lifted">
      {label}
    </div>
  );
}

function BuilderSettings({
  selection,
  field,
  sectionContext,
  rowContext,
  columnContext,
  layoutWidth,
  onAddColumn,
  onAddRow,
  onBalanceRowColumns,
  onChangeColumnSpan,
  onChangeField,
  onChangeFieldLayoutWidth,
  onChangeSection,
  onDeleteEmptyColumn,
  onDeleteEmptyRow,
  onDeleteEmptySection,
  onDeleteField,
  onMoveColumn
}: {
  selection: DesignerSelection | null;
  field: FormField | null;
  sectionContext: SectionContext | null;
  rowContext: RowContext | null;
  columnContext: ColumnContext | null;
  layoutWidth: LayoutWidthValue | null;
  onAddColumn: (columnId: string, position: "before" | "after") => void;
  onAddRow: (sectionId: string, rowId: string | undefined, position: LayoutBlockDrop["position"]) => void;
  onBalanceRowColumns: (rowId: string) => void;
  onChangeColumnSpan: (columnId: string, span: FormLayoutColumn["span"]) => void;
  onChangeField: (field: FormField) => void;
  onChangeFieldLayoutWidth: (fieldId: string, width: LayoutWidthValue) => void;
  onChangeSection: (sectionId: string, patch: Pick<FormLayoutSection, "title" | "description">) => void;
  onDeleteEmptyColumn: (columnId: string) => void;
  onDeleteEmptyRow: (rowId: string) => void;
  onDeleteEmptySection: (sectionId: string) => void;
  onDeleteField: () => void;
  onMoveColumn: (columnId: string, direction: "left" | "right") => void;
}) {
  if (selection?.type === "section" && sectionContext) {
    return (
      <SectionSettings
        context={sectionContext}
        onAddRow={onAddRow}
        onChange={onChangeSection}
        onDeleteEmptySection={onDeleteEmptySection}
      />
    );
  }

  if (selection?.type === "row" && rowContext) {
    return <RowSettings context={rowContext} onAddRow={onAddRow} onDeleteEmptyRow={onDeleteEmptyRow} />;
  }

  if (selection?.type === "column" && columnContext) {
    return (
      <ColumnSettings
        context={columnContext}
        onAddColumn={onAddColumn}
        onBalanceRowColumns={onBalanceRowColumns}
        onChangeSpan={onChangeColumnSpan}
        onDeleteEmptyColumn={onDeleteEmptyColumn}
        onMoveColumn={onMoveColumn}
      />
    );
  }

  return (
    <FieldSettings
      field={field}
      layoutWidth={layoutWidth}
      onChange={onChangeField}
      onChangeLayoutWidth={onChangeFieldLayoutWidth}
      onDelete={onDeleteField}
    />
  );
}

function SectionSettings({
  context,
  onAddRow,
  onChange,
  onDeleteEmptySection
}: {
  context: SectionContext;
  onAddRow: (sectionId: string, rowId: string | undefined, position: LayoutBlockDrop["position"]) => void;
  onChange: (sectionId: string, patch: Pick<FormLayoutSection, "title" | "description">) => void;
  onDeleteEmptySection: (sectionId: string) => void;
}) {
  const { section, pageSectionCount } = context;
  const canDelete = pageSectionCount > 1 && isLayoutSectionEmpty(section);

  return (
    <Card className="self-start">
      <CardHeader>
        <div className="flex items-start justify-between gap-3">
          <div>
            <CardTitle>Section settings</CardTitle>
            <CardDescription>{section.title ?? "Untitled section"}</CardDescription>
          </div>
          <Settings2 className="size-5 text-muted-foreground" />
        </div>
      </CardHeader>
      <CardContent>
        <form className="grid gap-4" onSubmit={preventSubmit}>
          <Input label="Title" onChange={(event) => onChange(section.id, { title: event.target.value })} value={section.title ?? ""} />
          <Textarea
            label="Description"
            onChange={(event) => onChange(section.id, { description: event.target.value })}
            value={section.description ?? ""}
          />
          <Button onClick={() => onAddRow(section.id, undefined, "end")} variant="outline">
            <Plus className="size-4" />
            Add row
          </Button>
          <div className="grid gap-2">
            <Button disabled={!canDelete} onClick={() => onDeleteEmptySection(section.id)} variant="danger">
              <Trash2 className="size-4" />
              Delete empty section
            </Button>
            <p className="text-xs font-semibold text-muted-foreground">
              Sections can be removed only when they contain no fields and another section remains.
            </p>
          </div>
        </form>
      </CardContent>
    </Card>
  );
}

function RowSettings({
  context,
  onAddRow,
  onDeleteEmptyRow
}: {
  context: RowContext;
  onAddRow: (sectionId: string, rowId: string | undefined, position: LayoutBlockDrop["position"]) => void;
  onDeleteEmptyRow: (rowId: string) => void;
}) {
  const { section, row } = context;
  const canDelete = isLayoutRowEmpty(row);

  return (
    <Card className="self-start">
      <CardHeader>
        <div className="flex items-start justify-between gap-3">
          <div>
            <CardTitle>Row settings</CardTitle>
            <CardDescription>{row.columns.length} columns</CardDescription>
          </div>
          <Settings2 className="size-5 text-muted-foreground" />
        </div>
      </CardHeader>
      <CardContent>
        <form className="grid gap-4" onSubmit={preventSubmit}>
          <div className="grid grid-cols-2 gap-2">
            <Button onClick={() => onAddRow(section.id, row.id, "before")} variant="outline">
              <Plus className="size-4" />
              Row above
            </Button>
            <Button onClick={() => onAddRow(section.id, row.id, "after")} variant="outline">
              <Plus className="size-4" />
              Row below
            </Button>
          </div>
          <div className="grid gap-2">
            <Button disabled={!canDelete} onClick={() => onDeleteEmptyRow(row.id)} variant="danger">
              <Trash2 className="size-4" />
              Delete empty row
            </Button>
            <p className="text-xs font-semibold text-muted-foreground">Rows can be removed only when every column is empty.</p>
          </div>
        </form>
      </CardContent>
    </Card>
  );
}

function ColumnSettings({
  context,
  onAddColumn,
  onBalanceRowColumns,
  onChangeSpan,
  onDeleteEmptyColumn,
  onMoveColumn
}: {
  context: ColumnContext;
  onAddColumn: (columnId: string, position: "before" | "after") => void;
  onBalanceRowColumns: (rowId: string) => void;
  onChangeSpan: (columnId: string, span: FormLayoutColumn["span"]) => void;
  onDeleteEmptyColumn: (columnId: string) => void;
  onMoveColumn: (columnId: string, direction: "left" | "right") => void;
}) {
  const { column, row } = context;
  const columnIndex = row.columns.findIndex((candidate) => candidate.id === column.id);
  const canMoveLeft = columnIndex > 0;
  const canMoveRight = columnIndex >= 0 && columnIndex < row.columns.length - 1;
  const canDelete = row.columns.length > 1 && column.fields.length === 0;
  const canBalance = row.columns.length >= 1 && row.columns.length <= 4;

  function patchSpan(patch: Partial<FormLayoutColumn["span"]>) {
    onChangeSpan(column.id, { ...column.span, ...patch, mobile: 12 });
  }

  return (
    <Card className="self-start">
      <CardHeader>
        <div className="flex items-start justify-between gap-3">
          <div>
            <CardTitle>Column settings</CardTitle>
            <CardDescription>
              Column {columnIndex + 1} of {row.columns.length}
            </CardDescription>
          </div>
          <Settings2 className="size-5 text-muted-foreground" />
        </div>
      </CardHeader>
      <CardContent>
        <form className="grid gap-4" onSubmit={preventSubmit}>
          <div className="grid grid-cols-2 gap-2">
            <Button onClick={() => onAddColumn(column.id, "before")} size="sm" variant="outline">
              <Plus className="size-4" />
              Add left
            </Button>
            <Button onClick={() => onAddColumn(column.id, "after")} size="sm" variant="outline">
              <Plus className="size-4" />
              Add right
            </Button>
          </div>
          <div className="grid grid-cols-2 gap-2">
            <Button disabled={!canMoveLeft} onClick={() => onMoveColumn(column.id, "left")} size="sm" variant="outline">
              <ArrowLeft className="size-4" />
              Move left
            </Button>
            <Button disabled={!canMoveRight} onClick={() => onMoveColumn(column.id, "right")} size="sm" variant="outline">
              <ArrowRight className="size-4" />
              Move right
            </Button>
          </div>
          <div className="grid grid-cols-2 gap-2">
            {layoutWidthOptions.map((option) => (
              <Button key={option.value} onClick={() => onChangeSpan(column.id, option.span)} size="sm" variant="outline">
                {option.label}
              </Button>
            ))}
          </div>
          <Button disabled={!canBalance} onClick={() => onBalanceRowColumns(row.id)} variant="outline">
            Balance row columns
          </Button>
          <Select
            help="Mobile remains full width for readable forms."
            label="Tablet span"
            onChange={(event) => patchSpan({ tablet: Number(event.target.value) })}
            options={spanSelectOptions}
            value={String(column.span.tablet)}
          />
          <Select
            label="Desktop span"
            onChange={(event) => patchSpan({ desktop: Number(event.target.value) })}
            options={spanSelectOptions}
            value={String(column.span.desktop)}
          />
          <div className="rounded-xl border border-border bg-muted/30 px-3 py-2 text-xs font-semibold text-muted-foreground">
            Current span: mobile 12 / tablet {column.span.tablet} / desktop {column.span.desktop}
          </div>
          <div className="grid gap-2">
            <Button disabled={!canDelete} onClick={() => onDeleteEmptyColumn(column.id)} variant="danger">
              <Trash2 className="size-4" />
              Delete empty column
            </Button>
            <p className="text-xs font-semibold text-muted-foreground">Columns can be removed only when empty and another column remains.</p>
          </div>
        </form>
      </CardContent>
    </Card>
  );
}

function FieldSettings({
  field,
  layoutWidth,
  onChange,
  onChangeLayoutWidth,
  onDelete
}: {
  field: FormField | null;
  layoutWidth: LayoutWidthValue | null;
  onChange: (field: FormField) => void;
  onChangeLayoutWidth: (fieldId: string, width: LayoutWidthValue) => void;
  onDelete: () => void;
}) {
  if (!field) {
    return (
      <Card className="self-start">
        <CardHeader>
          <CardTitle>Settings</CardTitle>
          <CardDescription>Select a field to edit its settings.</CardDescription>
        </CardHeader>
        <CardContent>
          <EmptyState title="No field selected" description="Add or select a field on the canvas." />
        </CardContent>
      </Card>
    );
  }

  function patchField(patch: Partial<FormField>) {
    if (!field) return;
    onChange({ ...field, ...patch });
  }

  return (
    <Card className="self-start">
      <CardHeader>
        <div className="flex items-start justify-between gap-3">
          <div>
            <CardTitle>Settings</CardTitle>
            <CardDescription>{fieldTypeLabels[field.type]} field</CardDescription>
          </div>
          <Settings2 className="size-5 text-muted-foreground" />
        </div>
      </CardHeader>
      <CardContent>
        <form className="grid gap-4" onSubmit={preventSubmit}>
          <Select
            label="Field type"
            onChange={(event) => patchField({ type: event.target.value as FormFieldType })}
            options={fieldTypeOptions}
            value={field.type}
          />
          <Input label="Label" onChange={(event) => patchField({ label: event.target.value })} value={field.label} />
          {field.type !== "checkbox" ? (
            <Input
              label="Placeholder"
              onChange={(event) => patchField({ placeholder: event.target.value })}
              value={field.placeholder ?? ""}
            />
          ) : null}
          <Textarea label="Help text" onChange={(event) => patchField({ helpText: event.target.value })} value={field.helpText ?? ""} />
          <Select
            help="Mobile stays full width. Tablet and desktop use this width."
            label="Width"
            onChange={(event) => onChangeLayoutWidth(field.id, event.target.value as LayoutWidthValue)}
            options={layoutWidthSelectOptions}
            value={layoutWidth ?? "full"}
          />
          <Checkbox
            checked={Boolean(field.required)}
            label="Required"
            onChange={(event) => patchField({ required: event.target.checked })}
          />
          <DefaultValueSetting field={field} onChange={(defaultValue) => patchField({ defaultValue })} />
          {isChoiceFieldType(field.type) ? <OptionsEditor field={field} onChange={(options) => patchField({ options })} /> : null}
          <Button onClick={onDelete} variant="danger">
            <Trash2 className="size-4" />
            Delete field
          </Button>
        </form>
      </CardContent>
    </Card>
  );
}

function DefaultValueSetting({
  field,
  onChange
}: {
  field: FormField;
  onChange: (value: FormField["defaultValue"]) => void;
}) {
  if (field.type === "checkbox") {
    return (
      <Checkbox
        checked={Boolean(field.defaultValue)}
        label="Checked by default"
        onChange={(event) => onChange(event.target.checked)}
      />
    );
  }

  if (isChoiceFieldType(field.type)) {
    return (
      <Select
        label="Default value"
        onChange={(event) => onChange(event.target.value || undefined)}
        value={typeof field.defaultValue === "string" ? field.defaultValue : ""}
      >
        <option value="">No default</option>
        {(field.options ?? []).map((option) => (
          <option key={option.id} value={option.value}>
            {option.label}
          </option>
        ))}
      </Select>
    );
  }

  return (
    <Input
      label="Default value"
      onChange={(event) => onChange(event.target.value)}
      type={field.type === "number" ? "number" : getInputType(field.type)}
      value={field.defaultValue === undefined || typeof field.defaultValue === "boolean" ? "" : String(field.defaultValue)}
    />
  );
}

function OptionsEditor({ field, onChange }: { field: FormField; onChange: (options: FormFieldOption[]) => void }) {
  const options = field.options ?? [];

  function updateOption(index: number, patch: Partial<FormFieldOption>) {
    onChange(options.map((option, optionIndex) => (optionIndex === index ? { ...option, ...patch } : option)));
  }

  function addOption() {
    const nextIndex = options.length + 1;
    onChange([...options, { id: "", label: `Option ${nextIndex}`, value: `option_${nextIndex}` }]);
  }

  function removeOption(index: number) {
    onChange(options.filter((_, optionIndex) => optionIndex !== index));
  }

  return (
    <div className="grid gap-3 rounded-xl border border-border bg-muted/30 p-3">
      <div className="flex items-center justify-between gap-3">
        <p className="text-sm font-bold text-foreground">Options</p>
        <Button onClick={addOption} size="sm" variant="outline">
          <Plus className="size-4" />
          Add option
        </Button>
      </div>
      {options.map((option, index) => (
        <div className="grid gap-2 rounded-lg border border-border bg-card/80 p-3" key={`${option.id}-${index}`}>
          <Input label="Label" onChange={(event) => updateOption(index, { label: event.target.value })} value={option.label} />
          <Input label="Value" onChange={(event) => updateOption(index, { value: event.target.value })} value={option.value} />
          <Button disabled={options.length <= 1} onClick={() => removeOption(index)} size="sm" variant="ghost">
            <Trash2 className="size-4" />
            Remove
          </Button>
        </div>
      ))}
    </div>
  );
}

function findSectionContext(schema: FormSchema, sectionId: string): SectionContext | null {
  for (const page of schema.layout.pages) {
    const section = page.sections.find((candidate) => candidate.id === sectionId);

    if (section) {
      return { pageSectionCount: page.sections.length, section };
    }
  }

  return null;
}

function findRowContext(schema: FormSchema, rowId: string): RowContext | null {
  for (const page of schema.layout.pages) {
    for (const section of page.sections) {
      const row = section.rows.find((candidate) => candidate.id === rowId);

      if (row) {
        return { section, row };
      }
    }
  }

  return null;
}

function findColumnContext(schema: FormSchema, columnId: string): ColumnContext | null {
  for (const page of schema.layout.pages) {
    for (const section of page.sections) {
      for (const row of section.rows) {
        const column = row.columns.find((candidate) => candidate.id === columnId);

        if (column) {
          return { section, row, column };
        }
      }
    }
  }

  return null;
}

function getInputType(type: FormFieldType): string {
  if (type === "email") return "email";
  if (type === "number") return "number";
  if (type === "date") return "date";
  if (type === "phone") return "tel";
  return "text";
}

function getColumnSpanClass(column: FormLayoutColumn): string {
  return cn(
    tabletSpanClasses[column.span.tablet] ?? tabletSpanClasses[12],
    desktopSpanClasses[column.span.desktop] ?? desktopSpanClasses[12]
  );
}

function getLayoutWidthLabel(column: FormLayoutColumn): string {
  return layoutWidthOptions.find((option) => option.span.desktop === column.span.desktop)?.label ?? "Custom width";
}

function preventSubmit(event: FormEvent<HTMLFormElement>) {
  event.preventDefault();
}

function getErrorMessage(error: unknown): string {
  return error instanceof Error ? error.message : "Form builder request failed.";
}

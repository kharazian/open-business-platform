export const formBuilderWorkspaceClassName = "grid min-h-0 gap-4 xl:grid-cols-[18rem_minmax(0,1fr)_24rem] xl:items-start";

export const formBuilderSidebarClassName =
  "grid min-h-0 gap-4 self-start xl:sticky xl:top-4 xl:max-h-[calc(100dvh-14rem)] xl:overflow-y-auto xl:overscroll-contain xl:pr-1 [scrollbar-gutter:stable] [scrollbar-width:thin]";

export const formBuilderCanvasScrollClassName =
  "min-h-0 min-w-0 xl:max-h-[calc(100dvh-14rem)] xl:overflow-y-auto xl:overscroll-contain xl:pr-1 [scrollbar-gutter:stable] [scrollbar-width:thin]";

export const formBuilderSoftDangerButtonClassName =
  "!border !border-danger/25 !bg-danger-soft !text-danger hover:!bg-danger/10 hover:!text-danger";

export const formBuilderSoftDangerIconButtonClassName = `size-8 shrink-0 rounded-lg ${formBuilderSoftDangerButtonClassName}`;

export const draftDetailsModalPanelClassName = "max-w-xl";

export type FieldParentSelection = {
  type: "column" | "row" | "section";
  id: string;
};

export type FieldParentSelectionItem = {
  label: string;
  selection: FieldParentSelection;
};

export function createFieldParentSelectionItems({
  columnId,
  rowId,
  sectionId
}: {
  columnId: string;
  rowId: string;
  sectionId: string;
}): FieldParentSelectionItem[] {
  return [
    { label: "Select column", selection: { type: "column", id: columnId } },
    { label: "Select row", selection: { type: "row", id: rowId } },
    { label: "Select section", selection: { type: "section", id: sectionId } }
  ];
}

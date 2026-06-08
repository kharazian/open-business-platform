import type {
  PrintTemplateConfig,
  PrintTemplateDetail,
  PrintTemplateDraft,
  PrintTemplateLayoutConfig,
  PrintTemplateSectionPaginationConfig,
  PrintTemplateSectionConfig,
  PrintTemplateType,
  PrintTemplateValidationError,
  PrintTemplateValidationResult
} from "./types";
import { printTemplateMargins, printTemplateOrientations, printTemplatePageSizes } from "./types";

export function createDefaultPrintTemplateConfig(type: PrintTemplateType, sourceName = "Document"): PrintTemplateConfig {
  const normalizedName = sourceName.trim() || "Document";

  return {
    schemaVersion: 1,
    type,
    layout: createDefaultLayoutConfig(),
    header: {
      title: normalizedName,
      subtitle: type === "record" ? "Record detail" : "Report table",
      logoUrl: null,
      showGeneratedAt: true
    },
    sections: [createDefaultSection(type, 1)],
    footer: {
      text: "Open Business Platform"
    }
  };
}

export function createPrintTemplateDraft(type: PrintTemplateType, sourceName = "Document"): PrintTemplateDraft {
  const normalizedName = sourceName.trim() || "Document";

  return {
    name: `${normalizedName} ${type} template`,
    description: "",
    type,
    reportId: null,
    config: createDefaultPrintTemplateConfig(type, normalizedName)
  };
}

export function createPrintTemplateDraftFromDetail(template: PrintTemplateDetail): PrintTemplateDraft {
  return {
    id: template.id,
    formId: template.formId,
    reportId: template.reportId ?? null,
    name: template.name,
    description: template.description ?? "",
    type: template.type,
    config: normalizePrintTemplateConfig(template.config, template.type),
    concurrencyStamp: template.concurrencyStamp
  };
}

export function buildPrintTemplateRequest(draft: PrintTemplateDraft) {
  return {
    name: draft.name.trim(),
    description: normalizeOptionalText(draft.description),
    type: draft.type,
    reportId: draft.type === "report" ? draft.reportId ?? null : null,
    config: normalizePrintTemplateConfig(draft.config, draft.type)
  };
}

export function validatePrintTemplateDraft(draft: PrintTemplateDraft): PrintTemplateValidationResult {
  const errors: PrintTemplateValidationError[] = [];

  if (!draft.name.trim()) {
    errors.push(error("name", "print_template.name.required", "Template name is required."));
  }

  if (draft.type === "report" && !draft.reportId) {
    errors.push(error("reportId", "print_template.report.required", "Report templates must target a saved report."));
  }

  if (draft.config.schemaVersion !== 1) {
    errors.push(error("config.schemaVersion", "print_template.schema_version.unsupported", "Template schema version must be 1."));
  }

  if (draft.config.type !== draft.type) {
    errors.push(error("config.type", "print_template.type.mismatch", "Template config type must match the template type."));
  }

  if (!draft.config.header.title.trim()) {
    errors.push(error("config.header.title", "print_template.header.title_required", "Header title is required."));
  }

  if (!printTemplatePageSizes.includes(draft.config.layout.pageSize)) {
    errors.push(error("config.layout.pageSize", "print_template.layout.page_size_invalid", "Page size must be Letter or A4."));
  }

  if (!printTemplateOrientations.includes(draft.config.layout.orientation)) {
    errors.push(error("config.layout.orientation", "print_template.layout.orientation_invalid", "Orientation must be portrait or landscape."));
  }

  if (!printTemplateMargins.includes(draft.config.layout.margin)) {
    errors.push(error("config.layout.margin", "print_template.layout.margin_invalid", "Margin must be narrow, normal, or wide."));
  }

  if (draft.config.sections.length === 0) {
    errors.push(error("config.sections", "print_template.sections.required", "At least one section is required."));
  }

  for (const [index, section] of draft.config.sections.entries()) {
    if (!section.id.trim()) {
      errors.push(error(`config.sections[${index}].id`, "print_template.section.id_required", "Section id is required."));
    }

    if (!section.title.trim()) {
      errors.push(error(`config.sections[${index}].title`, "print_template.section.title_required", "Section title is required."));
    }

    if (draft.type === "record" && section.kind === "table") {
      errors.push(error(`config.sections[${index}].kind`, "print_template.section.table_record", "Record templates cannot use table sections."));
    }

    if (draft.type === "report" && section.kind === "fields") {
      errors.push(error(`config.sections[${index}].kind`, "print_template.section.fields_report", "Report templates cannot use field sections."));
    }
  }

  return { valid: errors.length === 0, errors };
}

export function createTemplateSection(type: PrintTemplateType, index: number): PrintTemplateSectionConfig {
  return createDefaultSection(type, index);
}

function createDefaultSection(type: PrintTemplateType, index: number): PrintTemplateSectionConfig {
  return {
    id: type === "record" ? `fields_${index}` : `table_${index}`,
    kind: type === "record" ? "fields" : "table",
    title: type === "record" ? "Record fields" : "Report rows",
    fieldIds: [],
    signatureLabels: [],
    pagination: createDefaultSectionPagination()
  };
}

export function normalizePrintTemplateConfig(config: PrintTemplateConfig, type: PrintTemplateType): PrintTemplateConfig {
  return {
    schemaVersion: 1,
    type,
    layout: normalizeLayoutConfig(config.layout),
    header: {
      title: config.header.title.trim(),
      subtitle: normalizeOptionalText(config.header.subtitle),
      logoUrl: normalizeOptionalText(config.header.logoUrl),
      showGeneratedAt: config.header.showGeneratedAt
    },
    sections: config.sections.map((section) => ({
      id: normalizeKey(section.id),
      kind: section.kind,
      title: section.title.trim(),
      fieldIds: section.fieldIds.map((fieldId) => fieldId.trim()).filter(Boolean),
      signatureLabels: (section.signatureLabels ?? []).map((label) => label.trim()).filter(Boolean),
      pagination: normalizeSectionPagination(section.pagination)
    })),
    footer: {
      text: normalizeOptionalText(config.footer.text)
    }
  };
}

function createDefaultLayoutConfig(): PrintTemplateLayoutConfig {
  return {
    pageSize: "letter",
    orientation: "portrait",
    margin: "normal",
    repeatTableHeaders: true
  };
}

function createDefaultSectionPagination(): PrintTemplateSectionPaginationConfig {
  return {
    pageBreakBefore: false,
    avoidBreakInside: true
  };
}

function normalizeLayoutConfig(layout?: PrintTemplateLayoutConfig | null): PrintTemplateLayoutConfig {
  const defaults = createDefaultLayoutConfig();

  return {
    pageSize: normalizeOption(layout?.pageSize, printTemplatePageSizes, defaults.pageSize),
    orientation: normalizeOption(layout?.orientation, printTemplateOrientations, defaults.orientation),
    margin: normalizeOption(layout?.margin, printTemplateMargins, defaults.margin),
    repeatTableHeaders: layout?.repeatTableHeaders ?? defaults.repeatTableHeaders
  };
}

function normalizeSectionPagination(pagination?: PrintTemplateSectionPaginationConfig | null): PrintTemplateSectionPaginationConfig {
  const defaults = createDefaultSectionPagination();

  return {
    pageBreakBefore: pagination?.pageBreakBefore ?? defaults.pageBreakBefore,
    avoidBreakInside: pagination?.avoidBreakInside ?? defaults.avoidBreakInside
  };
}

function normalizeOption<TOption extends string>(value: unknown, options: readonly TOption[], fallback: TOption): TOption {
  return typeof value === "string" && options.includes(value as TOption) ? value as TOption : fallback;
}

function normalizeOptionalText(value?: string | null): string | null {
  const normalized = value?.trim();
  return normalized ? normalized : null;
}

function normalizeKey(value: string): string {
  return value
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9_]+/g, "_")
    .replace(/^_+|_+$/g, "");
}

function error(path: string, code: string, message: string): PrintTemplateValidationError {
  return { path, code, message };
}

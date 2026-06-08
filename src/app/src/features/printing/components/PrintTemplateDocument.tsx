import { getGeneratedAtPrintMetadata, joinPrintMetadata } from "../printLayout";
import {
  formatRecordValue,
  getPrintTemplateDocumentClassName,
  getPrintTemplateSectionClassName,
  shouldRenderPrintTemplateSection,
  type PrintTemplateConditionSource
} from "../templateRenderer";
import type {
  PrintTemplateConfig,
  PrintTemplateRenderDetail,
  PrintTemplateReportColumn,
  PrintTemplateSectionConfig,
  RecordTemplateSource,
  ReportTemplateExecution
} from "../types";

type PrintTemplateDocumentProps = {
  metadata?: string[];
  record?: RecordTemplateSource;
  report?: ReportTemplateExecution;
  template: PrintTemplateRenderDetail;
};

export function PrintTemplateDocument({ metadata = [], record, report, template }: PrintTemplateDocumentProps) {
  const { config } = template;
  const headerMetadata = joinPrintMetadata([
    ...metadata,
    config.header.showGeneratedAt ? getGeneratedAtPrintMetadata() : null
  ]);

  return (
    <article className={getPrintTemplateDocumentClassName(config)}>
      <header className="print-template-header">
        <div className="print-template-header-main">
          {config.header.logoUrl ? <img alt="" className="print-template-logo" src={config.header.logoUrl} /> : null}
          <div>
            <p className="print-template-eyebrow">{config.type === "record" ? "Record template" : "Report template"}</p>
            <h1 className="print-template-title">{config.header.title}</h1>
            {config.header.subtitle ? <p className="print-template-subtitle">{config.header.subtitle}</p> : null}
          </div>
        </div>
        {headerMetadata ? <p className="print-template-metadata">{headerMetadata}</p> : null}
        {template.description ? <p className="print-template-description">{template.description}</p> : null}
      </header>

      <div className="print-template-sections">
        {config.sections.map((section, index) => (
          <PrintTemplateSection config={config} key={`${section.id}-${index}`} record={record} report={report} section={section} />
        ))}
      </div>

      {config.footer.text ? <footer className="print-template-footer">{config.footer.text}</footer> : null}
    </article>
  );
}

function PrintTemplateSection({
  config,
  record,
  report,
  section
}: {
  config: PrintTemplateConfig;
  record?: RecordTemplateSource;
  report?: ReportTemplateExecution;
  section: PrintTemplateSectionConfig;
}) {
  const conditionSource = getConditionSource(config, record, report);

  if (!shouldRenderPrintTemplateSection(section, conditionSource)) {
    return null;
  }

  if (section.kind === "signature") {
    return <SignatureSection section={section} />;
  }

  if (section.kind === "fields" && config.type === "record" && record) {
    return <RecordFieldsSection record={record} section={section} />;
  }

  if (section.kind === "table" && config.type === "report" && report) {
    return <ReportTableSection report={report} section={section} />;
  }

  return null;
}

function getConditionSource(
  config: PrintTemplateConfig,
  record?: RecordTemplateSource,
  report?: ReportTemplateExecution
): PrintTemplateConditionSource | null {
  if (config.type === "record" && record) {
    return { type: "record", values: record.values };
  }

  if (config.type === "report" && report) {
    return { type: "report", report };
  }

  return null;
}

function RecordFieldsSection({ record, section }: { record: RecordTemplateSource; section: PrintTemplateSectionConfig }) {
  const fieldsById = new Map(record.schema.fields.map((field) => [field.id, field]));
  const selectedFieldIds = section.fieldIds.length > 0 ? section.fieldIds : record.schema.fields.map((field) => field.id);
  const rows = selectedFieldIds
    .flatMap((fieldId) => {
      const field = fieldsById.get(fieldId);
      return field ? [field] : [];
    })
    .map((field) => ({
      fieldId: field.id,
      label: field.label,
      value: formatRecordValue(record.values[field.id])
    }));

  return (
    <section className={getPrintTemplateSectionClassName(section)}>
      <h2 className="print-template-section-title">{section.title}</h2>
      <dl className="print-template-field-grid">
        {rows.map((row) => (
          <div className="print-template-field-row" key={row.fieldId}>
            <dt>{row.label}</dt>
            <dd>{row.value}</dd>
          </div>
        ))}
      </dl>
    </section>
  );
}

function ReportTableSection({ report, section }: { report: ReportTemplateExecution; section: PrintTemplateSectionConfig }) {
  const columns = getSelectedColumns(report.columns, section.fieldIds);

  return (
    <section className={getPrintTemplateSectionClassName(section)}>
      <h2 className="print-template-section-title">{section.title}</h2>
      <table className="print-template-table">
        <thead>
          <tr>
            {columns.map((column) => (
              <th key={column.fieldId}>{column.label}</th>
            ))}
          </tr>
        </thead>
        <tbody>
          {report.rows.map((row) => (
            <tr key={row.id}>
              {columns.map((column) => (
                <td key={column.fieldId}>{row.cells[column.fieldId]?.displayValue?.trim() || "-"}</td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  );
}

function SignatureSection({ section }: { section: PrintTemplateSectionConfig }) {
  const labels = (section.signatureLabels ?? []).map((label) => label.trim()).filter(Boolean);

  if (labels.length === 0) {
    return null;
  }

  return (
    <section className={getPrintTemplateSectionClassName(section)}>
      <h2 className="print-template-section-title">{section.title}</h2>
      <div className="print-template-signatures">
        {labels.map((label) => (
          <div className="print-template-signature-line" key={label}>
            <span>{label}</span>
          </div>
        ))}
      </div>
    </section>
  );
}

function getSelectedColumns(columns: PrintTemplateReportColumn[], fieldIds: string[]): PrintTemplateReportColumn[] {
  if (fieldIds.length === 0) {
    return columns;
  }

  const columnsById = new Map(columns.map((column) => [column.fieldId, column]));

  return fieldIds
    .flatMap((fieldId) => {
      const column = columnsById.get(fieldId);
      return column ? [column] : [];
    });
}

import type { ReactNode } from "react";
import { joinPrintMetadata, type PrintMetadataValue } from "../printLayout";

type PrintDocumentHeaderProps = {
  eyebrow: string;
  title: string;
  description?: string;
  metadata?: PrintMetadataValue[];
};

export function PrintDocumentHeader({ description, eyebrow, metadata = [], title }: PrintDocumentHeaderProps) {
  const metadataText = joinPrintMetadata(metadata);

  return (
    <section className="print-only print-document-header">
      <div className="print-document-header-row">
        <div>
          <p className="print-document-eyebrow">{eyebrow}</p>
          <h1 className="print-document-title">{title}</h1>
        </div>
        <p className="print-document-brand">Open Business Platform</p>
      </div>
      {description ? <p className="print-document-description">{description}</p> : null}
      {metadataText ? <p className="print-document-metadata">{metadataText}</p> : null}
    </section>
  );
}

export function PrintDocumentFooter({ children }: { children?: ReactNode }) {
  return (
    <footer className="print-only print-document-footer">
      {children ?? "Open Business Platform"}
    </footer>
  );
}

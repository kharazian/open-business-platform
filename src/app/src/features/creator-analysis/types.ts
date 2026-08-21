export type CreatorAnalysisStatus = "supported" | "manual_review" | "unsupported" | "unsafe" | "unknown";

export type CreatorAnalysisConstruct = {
  id: string;
  type: string;
  displayName: string;
  lineStart: number;
  lineEnd: number;
  status: CreatorAnalysisStatus;
  proposedModule: string | null;
  proposedType: string | null;
};

export type CreatorAnalysisFinding = {
  id: string;
  severity: "info" | "warning" | "error";
  status: CreatorAnalysisStatus;
  reasonCode: string;
  constructId: string | null;
  message: string;
};

export type CreatorAnalysisReport = {
  analyzerVersion: string;
  canImport: false;
  complete: boolean;
  truncated: boolean;
  source: { byteCount: number; lineCount: number };
  summary: { constructCount: number; findingCount: number; byStatus: Record<CreatorAnalysisStatus, number> };
  credentialSignals: Array<{ category: string; count: number }>;
  constructs: CreatorAnalysisConstruct[];
  findings: CreatorAnalysisFinding[];
};

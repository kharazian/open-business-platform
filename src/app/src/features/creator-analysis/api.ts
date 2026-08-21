import type { CreatorAnalysisReport } from "./types";

type AnalysisResponse = { ok: boolean; status?: number; json: () => Promise<unknown> };
export type CreatorAnalysisFetcher = (input: string, init?: RequestInit) => Promise<AnalysisResponse>;

export class CreatorAnalysisApiError extends Error {
  constructor(message: string) {
    super(message);
    this.name = "CreatorAnalysisApiError";
  }
}

const defaultFetcher: CreatorAnalysisFetcher = (input, init) => fetch(input, init);

export async function analyzeCreatorExport(file: File, fetcher: CreatorAnalysisFetcher = defaultFetcher): Promise<CreatorAnalysisReport> {
  const extension = file.name.toLowerCase().endsWith(".txt") ? ".txt" : ".ds";
  const form = new FormData();
  form.append("source", file.slice(0, file.size, "text/plain"), `creator-export${extension}`);
  const response = await fetcher("/api/creator-analysis", { method: "POST", credentials: "include", body: form });
  const body = await response.json().catch(() => null);
  if (!response.ok) {
    const message = body && typeof body === "object" && "message" in body && typeof body.message === "string"
      ? body.message
      : "Creator export analysis failed.";
    throw new CreatorAnalysisApiError(message);
  }
  return body as CreatorAnalysisReport;
}

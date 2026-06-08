import type {
  CreatePrintTemplateRequest,
  PrintTemplateDetail,
  PrintTemplateSummary,
  PrintTemplateValidationError,
  PrintTemplateVersionDetail,
  PrintTemplateVersionSummary,
  UpdatePrintTemplateRequest
} from "./types";

type ApiFetchResponse = {
  ok: boolean;
  status?: number;
  json: () => Promise<unknown>;
};

type BinaryFetchResponse = {
  ok: boolean;
  status?: number;
  blob: () => Promise<Blob>;
  json?: () => Promise<unknown>;
};

export type PrintingFetcher = (input: string, init?: RequestInit) => Promise<ApiFetchResponse>;
export type PrintingBinaryFetcher = (input: string, init?: RequestInit) => Promise<BinaryFetchResponse>;

export class PrintingApiError extends Error {
  readonly errors: PrintTemplateValidationError[];

  constructor(message: string, errors: PrintTemplateValidationError[] = []) {
    super(message);
    this.name = "PrintingApiError";
    this.errors = errors;
  }
}

const defaultFetcher: PrintingFetcher = (input, init) => fetch(input, init);
const defaultBinaryFetcher: PrintingBinaryFetcher = (input, init) => fetch(input, init);

export async function listPrintTemplates(
  formId: string,
  options: { type?: string; reportId?: string | null } = {},
  fetcher: PrintingFetcher = defaultFetcher
): Promise<PrintTemplateSummary[]> {
  const query = new URLSearchParams();

  if (options.type) {
    query.set("type", options.type);
  }

  if (options.reportId) {
    query.set("reportId", options.reportId);
  }

  const queryString = query.toString();

  return requestItems<PrintTemplateSummary>(
    `/api/forms/${encodeURIComponent(formId)}/print-templates${queryString ? `?${queryString}` : ""}`,
    { method: "GET", credentials: "include" },
    fetcher
  );
}

export async function createPrintTemplate(
  formId: string,
  request: CreatePrintTemplateRequest,
  fetcher: PrintingFetcher = defaultFetcher
): Promise<PrintTemplateDetail> {
  return requestJson<PrintTemplateDetail>(
    `/api/forms/${encodeURIComponent(formId)}/print-templates`,
    {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request)
    },
    fetcher
  );
}

export async function getPrintTemplate(templateId: string, fetcher: PrintingFetcher = defaultFetcher): Promise<PrintTemplateDetail> {
  return requestJson<PrintTemplateDetail>(
    `/api/print-templates/${encodeURIComponent(templateId)}`,
    { method: "GET", credentials: "include" },
    fetcher
  );
}

export async function listPrintTemplateVersions(
  templateId: string,
  fetcher: PrintingFetcher = defaultFetcher
): Promise<PrintTemplateVersionSummary[]> {
  return requestItems<PrintTemplateVersionSummary>(
    `/api/print-templates/${encodeURIComponent(templateId)}/versions`,
    { method: "GET", credentials: "include" },
    fetcher
  );
}

export async function publishPrintTemplateVersion(
  templateId: string,
  concurrencyStamp: string,
  fetcher: PrintingFetcher = defaultFetcher
): Promise<PrintTemplateDetail> {
  return requestJson<PrintTemplateDetail>(
    `/api/print-templates/${encodeURIComponent(templateId)}/versions`,
    {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ concurrencyStamp })
    },
    fetcher
  );
}

export async function getPrintTemplateVersion(
  versionId: string,
  fetcher: PrintingFetcher = defaultFetcher
): Promise<PrintTemplateVersionDetail> {
  return requestJson<PrintTemplateVersionDetail>(
    `/api/print-template-versions/${encodeURIComponent(versionId)}`,
    { method: "GET", credentials: "include" },
    fetcher
  );
}

export async function downloadRecordPrintTemplatePdf(
  versionId: string,
  recordId: string,
  fetcher: PrintingBinaryFetcher = defaultBinaryFetcher
): Promise<Blob> {
  return requestBlob(
    `/api/print-template-versions/${encodeURIComponent(versionId)}/records/${encodeURIComponent(recordId)}.pdf`,
    { method: "GET", credentials: "include" },
    fetcher
  );
}

export async function downloadReportPrintTemplatePdf(
  versionId: string,
  reportId: string,
  options: { page?: number; pageSize?: number; search?: string | null } = {},
  fetcher: PrintingBinaryFetcher = defaultBinaryFetcher
): Promise<Blob> {
  const query = new URLSearchParams();

  if (options.page) {
    query.set("page", String(options.page));
  }

  if (options.pageSize) {
    query.set("pageSize", String(options.pageSize));
  }

  if (options.search) {
    query.set("search", options.search);
  }

  const queryString = query.toString();

  return requestBlob(
    `/api/print-template-versions/${encodeURIComponent(versionId)}/reports/${encodeURIComponent(reportId)}.pdf${queryString ? `?${queryString}` : ""}`,
    { method: "GET", credentials: "include" },
    fetcher
  );
}

export async function updatePrintTemplate(
  templateId: string,
  request: UpdatePrintTemplateRequest,
  fetcher: PrintingFetcher = defaultFetcher
): Promise<PrintTemplateDetail> {
  return requestJson<PrintTemplateDetail>(
    `/api/print-templates/${encodeURIComponent(templateId)}`,
    {
      method: "PUT",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request)
    },
    fetcher
  );
}

export async function deletePrintTemplate(templateId: string, fetcher: PrintingFetcher = defaultFetcher): Promise<void> {
  const response = await fetcher(
    `/api/print-templates/${encodeURIComponent(templateId)}`,
    { method: "DELETE", credentials: "include" }
  );

  if (!response.ok) {
    const body = await readJson(response);
    throw new PrintingApiError(getErrorMessageFromBody(body), getValidationErrorsFromBody(body));
  }
}

async function requestItems<T>(input: string, init: RequestInit, fetcher: PrintingFetcher): Promise<T[]> {
  const body = await requestJson<unknown>(input, init, fetcher);

  if (!isRecord(body) || !Array.isArray(body.items)) {
    throw new PrintingApiError("API response did not include an items collection.");
  }

  return body.items as T[];
}

async function requestJson<T>(input: string, init: RequestInit, fetcher: PrintingFetcher): Promise<T> {
  const response = await fetcher(input, init);
  const body = await readJson(response);

  if (!response.ok) {
    throw new PrintingApiError(getErrorMessageFromBody(body), getValidationErrorsFromBody(body));
  }

  return body as T;
}

async function requestBlob(input: string, init: RequestInit, fetcher: PrintingBinaryFetcher): Promise<Blob> {
  const response = await fetcher(input, init);

  if (!response.ok) {
    const body = response.json ? await readJson(response as ApiFetchResponse) : null;
    throw new PrintingApiError(getErrorMessageFromBody(body), getValidationErrorsFromBody(body));
  }

  return await response.blob();
}

async function readJson(response: ApiFetchResponse): Promise<unknown> {
  try {
    return await response.json();
  } catch {
    return null;
  }
}

function getErrorMessageFromBody(body: unknown): string {
  if (isRecord(body) && typeof body.message === "string" && body.message.trim().length > 0) {
    return body.message;
  }

  return "Printing API request failed.";
}

function getValidationErrorsFromBody(body: unknown): PrintTemplateValidationError[] {
  if (!isRecord(body) || !Array.isArray(body.errors)) {
    return [];
  }

  return body.errors.filter(isPrintTemplateValidationError);
}

function isPrintTemplateValidationError(value: unknown): value is PrintTemplateValidationError {
  return (
    isRecord(value)
    && typeof value.path === "string"
    && typeof value.code === "string"
    && typeof value.message === "string"
  );
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

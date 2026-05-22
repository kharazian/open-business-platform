import type { FormSummary } from "./drafts";
import type { FormRecordValues, FormSchema, ValidationError } from "./types";

type ApiFetchResponse = {
  ok: boolean;
  status?: number;
  json: () => Promise<unknown>;
};

export type FormsFetcher = (input: string, init?: RequestInit) => Promise<ApiFetchResponse>;

export type CreateFormRequest = {
  name: string;
  description?: string;
};

export type FormDetail = FormSummary & {
  draftSchema?: FormSchema | null;
  concurrencyStamp: string;
  createdById?: string | null;
  updatedById?: string | null;
};

export type PublishedFormVersion = {
  id: string;
  formId: string;
  versionNumber: number;
  schema: FormSchema;
  publishedById?: string | null;
  publishedAt: string;
};

export type PublishFormResponse = {
  form: FormDetail;
  version: PublishedFormVersion;
};

export type PublishedFormForSubmission = {
  id: string;
  name: string;
  description?: string | null;
  currentVersionId: string;
  currentVersionNumber: number;
  schema: FormSchema;
};

export type SubmitRecordRequest = {
  values: FormRecordValues;
};

export type UpdateRecordRequest = {
  values: FormRecordValues;
  concurrencyStamp: string;
};

export type FormRecord = {
  id: string;
  formId: string;
  formVersionId: string;
  status: string;
  values: FormRecordValues;
  concurrencyStamp: string;
  createdAt: string;
  createdById?: string | null;
};

export type FormRecordListItem = {
  id: string;
  formId: string;
  formVersionId: string;
  status: string;
  values: FormRecordValues;
  createdAt: string;
  createdById?: string | null;
};

export type FormRecordDetail = FormRecord & {
  schema: FormSchema;
  updatedAt?: string | null;
  updatedById?: string | null;
};

export type ListRecordsOptions = {
  page?: number;
  pageSize?: number;
  search?: string;
};

export type PagedResult<T> = {
  totalCount: number;
  items: T[];
};

export class FormsApiError extends Error {
  readonly errors: ValidationError[];

  constructor(message: string, errors: ValidationError[] = []) {
    super(message);
    this.name = "FormsApiError";
    this.errors = errors;
  }
}

const defaultFetcher: FormsFetcher = (input, init) => fetch(input, init);

export async function listForms(fetcher: FormsFetcher = defaultFetcher): Promise<FormSummary[]> {
  return requestItems<FormSummary>("/api/forms", { method: "GET", credentials: "include" }, fetcher);
}

export async function getForm(formId: string, fetcher: FormsFetcher = defaultFetcher): Promise<FormDetail> {
  return requestJson<FormDetail>(
    `/api/forms/${encodeURIComponent(formId)}`,
    { method: "GET", credentials: "include" },
    fetcher
  );
}

export async function createForm(request: CreateFormRequest, fetcher: FormsFetcher = defaultFetcher): Promise<FormSummary> {
  return requestJson<FormSummary>(
    "/api/forms",
    {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request)
    },
    fetcher
  );
}

export async function updateFormDraft(
  formId: string,
  schema: FormSchema,
  fetcher: FormsFetcher = defaultFetcher
): Promise<FormDetail> {
  return requestJson<FormDetail>(
    `/api/forms/${encodeURIComponent(formId)}`,
    {
      method: "PUT",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ schema })
    },
    fetcher
  );
}

export async function publishForm(formId: string, fetcher: FormsFetcher = defaultFetcher): Promise<PublishFormResponse> {
  return requestJson<PublishFormResponse>(
    `/api/forms/${encodeURIComponent(formId)}/publish`,
    { method: "POST", credentials: "include" },
    fetcher
  );
}

export async function getPublishedFormForSubmission(
  formId: string,
  fetcher: FormsFetcher = defaultFetcher
): Promise<PublishedFormForSubmission> {
  return requestJson<PublishedFormForSubmission>(
    `/api/forms/${encodeURIComponent(formId)}/published`,
    { method: "GET", credentials: "include" },
    fetcher
  );
}

export async function submitRecord(
  formId: string,
  request: SubmitRecordRequest,
  fetcher: FormsFetcher = defaultFetcher
): Promise<FormRecord> {
  return requestJson<FormRecord>(
    `/api/forms/${encodeURIComponent(formId)}/records`,
    {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request)
    },
    fetcher
  );
}

export async function listRecords(
  formId: string,
  options: ListRecordsOptions = {},
  fetcher: FormsFetcher = defaultFetcher
): Promise<PagedResult<FormRecordListItem>> {
  const searchParams = new URLSearchParams();
  searchParams.set("page", String(options.page ?? 1));
  searchParams.set("pageSize", String(options.pageSize ?? 25));

  if (options.search && options.search.trim().length > 0) {
    searchParams.set("search", options.search.trim());
  }

  return requestJson<PagedResult<FormRecordListItem>>(
    `/api/forms/${encodeURIComponent(formId)}/records?${searchParams.toString()}`,
    { method: "GET", credentials: "include" },
    fetcher
  );
}

export async function getRecord(recordId: string, fetcher: FormsFetcher = defaultFetcher): Promise<FormRecordDetail> {
  return requestJson<FormRecordDetail>(
    `/api/records/${encodeURIComponent(recordId)}`,
    { method: "GET", credentials: "include" },
    fetcher
  );
}

export async function updateRecord(
  recordId: string,
  request: UpdateRecordRequest,
  fetcher: FormsFetcher = defaultFetcher
): Promise<FormRecordDetail> {
  return requestJson<FormRecordDetail>(
    `/api/records/${encodeURIComponent(recordId)}`,
    {
      method: "PUT",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request)
    },
    fetcher
  );
}

export async function deleteRecord(recordId: string, fetcher: FormsFetcher = defaultFetcher): Promise<void> {
  await requestNoContent(
    `/api/records/${encodeURIComponent(recordId)}`,
    { method: "DELETE", credentials: "include" },
    fetcher
  );
}

async function requestItems<T>(input: string, init: RequestInit, fetcher: FormsFetcher): Promise<T[]> {
  const body = await requestJson<unknown>(input, init, fetcher);

  if (!isRecord(body) || !Array.isArray(body.items)) {
    throw new FormsApiError("API response did not include an items collection.");
  }

  return body.items as T[];
}

async function requestJson<T>(input: string, init: RequestInit, fetcher: FormsFetcher): Promise<T> {
  const response = await fetcher(input, init);
  const body = await readJson(response);

  if (!response.ok) {
    throw new FormsApiError(getErrorMessageFromBody(body), getValidationErrorsFromBody(body));
  }

  return body as T;
}

async function requestNoContent(input: string, init: RequestInit, fetcher: FormsFetcher): Promise<void> {
  const response = await fetcher(input, init);
  const body = await readJson(response);

  if (!response.ok) {
    throw new FormsApiError(getErrorMessageFromBody(body), getValidationErrorsFromBody(body));
  }
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

  return "Forms request failed.";
}

function getValidationErrorsFromBody(body: unknown): ValidationError[] {
  if (!isRecord(body) || !Array.isArray(body.errors)) {
    return [];
  }

  return body.errors.filter(isValidationError);
}

function isValidationError(value: unknown): value is ValidationError {
  return (
    isRecord(value) &&
    typeof value.path === "string" &&
    typeof value.code === "string" &&
    typeof value.message === "string"
  );
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

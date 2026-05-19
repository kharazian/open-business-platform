import type { FormSummary } from "./drafts";

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

export class FormsApiError extends Error {
  constructor(message: string) {
    super(message);
    this.name = "FormsApiError";
  }
}

const defaultFetcher: FormsFetcher = (input, init) => fetch(input, init);

export async function listForms(fetcher: FormsFetcher = defaultFetcher): Promise<FormSummary[]> {
  return requestItems<FormSummary>("/api/forms", { method: "GET", credentials: "include" }, fetcher);
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
    throw new FormsApiError(getErrorMessageFromBody(body));
  }

  return body as T;
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

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

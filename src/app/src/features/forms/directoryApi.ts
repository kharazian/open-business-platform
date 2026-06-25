export type DirectoryOption = {
  id: string;
  label: string;
  description?: string | null;
};

type ApiFetchResponse = {
  ok: boolean;
  json: () => Promise<unknown>;
};

export type DirectoryFetcher = (input: string, init?: RequestInit) => Promise<ApiFetchResponse>;

export class DirectoryApiError extends Error {
  constructor(message: string) {
    super(message);
    this.name = "DirectoryApiError";
  }
}

const defaultFetcher: DirectoryFetcher = (input, init) => fetch(input, init);

export async function listDirectoryUsers(fetcher: DirectoryFetcher = defaultFetcher): Promise<DirectoryOption[]> {
  return requestItems("/api/directory/users", fetcher);
}

export async function listDirectoryDepartments(fetcher: DirectoryFetcher = defaultFetcher): Promise<DirectoryOption[]> {
  return requestItems("/api/directory/departments", fetcher);
}

async function requestItems(input: string, fetcher: DirectoryFetcher): Promise<DirectoryOption[]> {
  const response = await fetcher(input, { method: "GET", credentials: "include" });

  if (!response.ok) {
    throw new DirectoryApiError(await getErrorMessage(response));
  }

  const body = await response.json();

  if (!isRecord(body) || !Array.isArray(body.items)) {
    throw new DirectoryApiError("API response did not include an items collection.");
  }

  return body.items as DirectoryOption[];
}

async function getErrorMessage(response: ApiFetchResponse): Promise<string> {
  try {
    const body = await response.json();

    if (isRecord(body) && typeof body.message === "string") {
      return body.message;
    }
  } catch {
    // Fall through to the generic API error.
  }

  return "Directory options could not be loaded.";
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

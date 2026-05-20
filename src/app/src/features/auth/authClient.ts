import type { AuthSessionResponse, AuthUser, LoginCredentials } from "./types";

type AuthFetchResponse = {
  ok: boolean;
  status?: number;
  json: () => Promise<unknown>;
};

export type AuthFetcher = (input: string, init: RequestInit) => Promise<AuthFetchResponse>;

export class AuthRequestError extends Error {
  constructor(message: string) {
    super(message);
    this.name = "AuthRequestError";
  }
}

const defaultFetcher: AuthFetcher = (input, init) => fetch(input, init);

export async function login(credentials: LoginCredentials, fetcher: AuthFetcher = defaultFetcher): Promise<AuthUser> {
  const response = await fetcher("/api/auth/login", {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(credentials)
  });

  return parseRequiredAuthUser(response);
}

export async function getCurrentUser(fetcher: AuthFetcher = defaultFetcher): Promise<AuthUser | null> {
  const response = await fetcher("/api/auth/me", {
    method: "GET",
    credentials: "include"
  });

  if (!response.ok && response.status === 401) {
    return null;
  }

  return parseRequiredAuthUser(response);
}

export async function logout(fetcher: AuthFetcher = defaultFetcher): Promise<void> {
  const response = await fetcher("/api/auth/logout", {
    method: "POST",
    credentials: "include"
  });

  if (!response.ok) {
    throw new AuthRequestError("Sign out failed.");
  }
}

async function parseRequiredAuthUser(response: AuthFetchResponse): Promise<AuthUser> {
  const body = await readJson(response);

  if (!response.ok) {
    throw new AuthRequestError(getErrorMessage(body));
  }

  if (!isAuthSessionResponse(body)) {
    throw new AuthRequestError("Authentication response was not recognized.");
  }

  return body.user;
}

async function readJson(response: AuthFetchResponse): Promise<unknown> {
  try {
    return await response.json();
  } catch {
    return null;
  }
}

function getErrorMessage(body: unknown): string {
  if (isRecord(body) && typeof body.message === "string" && body.message.trim().length > 0) {
    return body.message;
  }

  return "Authentication failed.";
}

function isAuthSessionResponse(value: unknown): value is AuthSessionResponse {
  return isRecord(value) && isAuthUser(value.user);
}

function isAuthUser(value: unknown): value is AuthUser {
  return (
    isRecord(value) &&
    typeof value.id === "string" &&
    typeof value.name === "string" &&
    typeof value.email === "string" &&
    Array.isArray(value.roles) &&
    value.roles.every((role) => typeof role === "string") &&
    Array.isArray(value.permissions) &&
    value.permissions.every((permission) => typeof permission === "string")
  );
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

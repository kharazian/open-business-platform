import type { AuthSessionResponse, AuthUser, CompletePasswordResetRequest, LoginCredentials, SsoProvider, StartSsoRequest } from "./types";

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

export async function requestPasswordReset(email: string, fetcher: AuthFetcher = defaultFetcher): Promise<void> {
  const response = await fetcher("/api/auth/forgot-password", {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email })
  });

  await parseEmptyResponse(response, "Password reset request failed.");
}

export async function completePasswordReset(request: CompletePasswordResetRequest, fetcher: AuthFetcher = defaultFetcher): Promise<void> {
  const response = await fetcher("/api/auth/reset-password", {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(request)
  });

  await parseEmptyResponse(response, "Password reset failed.");
}

export async function getSsoProviders(
  tenantSlug: string,
  workspaceSlug: string,
  fetcher: AuthFetcher = defaultFetcher
): Promise<SsoProvider[]> {
  const query = new URLSearchParams({ tenantSlug, workspaceSlug });
  const response = await fetcher(`/api/auth/sso/providers?${query.toString()}`, {
    method: "GET",
    credentials: "include"
  });
  const body = await readJson(response);
  if (!response.ok) {
    throw new AuthRequestError(getErrorMessage(body, "SSO providers could not be loaded."));
  }
  if (!isRecord(body) || !Array.isArray(body.items) || !body.items.every(isSsoProvider)) {
    throw new AuthRequestError("SSO provider response was not recognized.");
  }
  return body.items;
}

export async function startSso(request: StartSsoRequest, fetcher: AuthFetcher = defaultFetcher): Promise<string> {
  const response = await fetcher("/api/auth/sso/start", {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(request)
  });
  const body = await readJson(response);
  if (!response.ok) {
    throw new AuthRequestError(getErrorMessage(body, "SSO sign in could not be started."));
  }
  if (!isRecord(body) || typeof body.authorizationUrl !== "string" || !body.authorizationUrl.startsWith("https://")) {
    throw new AuthRequestError("SSO start response was not recognized.");
  }
  return body.authorizationUrl;
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

async function parseEmptyResponse(response: AuthFetchResponse, fallbackMessage: string): Promise<void> {
  const body = await readJson(response);

  if (!response.ok) {
    throw new AuthRequestError(getErrorMessage(body, fallbackMessage));
  }
}

async function readJson(response: AuthFetchResponse): Promise<unknown> {
  try {
    return await response.json();
  } catch {
    return null;
  }
}

function getErrorMessage(body: unknown, fallback = "Authentication failed."): string {
  if (isRecord(body) && typeof body.message === "string" && body.message.trim().length > 0) {
    return body.message;
  }

  return fallback;
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
    typeof value.workspaceId === "string" &&
    Array.isArray(value.roles) &&
    value.roles.every((role) => typeof role === "string") &&
    Array.isArray(value.permissions) &&
    value.permissions.every((permission) => typeof permission === "string")
  );
}

function isSsoProvider(value: unknown): value is SsoProvider {
  return isRecord(value) && typeof value.id === "string" && typeof value.providerKey === "string" && typeof value.displayName === "string";
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

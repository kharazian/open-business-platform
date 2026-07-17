import type { LocalizationSettings, UserLocalizationPreference, WorkspaceLocalization } from "./types";

export class LocalizationApiError extends Error {}
export type LocalizationFetcher = (input: string, init?: RequestInit) => Promise<Response>;
const defaultFetcher: LocalizationFetcher = (input, init) => fetch(input, init);

async function request(input: string, init: RequestInit | undefined, fetcher: LocalizationFetcher) {
  const response = await fetcher(input, init);
  const body = (await response.json().catch(() => null)) as unknown;
  if (!response.ok) {
    const message = typeof body === "object" && body !== null && "message" in body && typeof body.message === "string" ? body.message : "Localization request failed.";
    throw new LocalizationApiError(message);
  }
  return body as LocalizationSettings;
}

export const getLocalization = (fetcher: LocalizationFetcher = defaultFetcher) => request("/api/localization/current", { credentials: "include" }, fetcher);
export const saveWorkspaceLocalization = (value: WorkspaceLocalization, fetcher: LocalizationFetcher = defaultFetcher) => request("/api/localization/workspace", { method: "PUT", credentials: "include", headers: { "Content-Type": "application/json" }, body: JSON.stringify(value) }, fetcher);
export const saveUserLocalization = (value: UserLocalizationPreference, fetcher: LocalizationFetcher = defaultFetcher) => request("/api/localization/me", { method: "PUT", credentials: "include", headers: { "Content-Type": "application/json" }, body: JSON.stringify(value) }, fetcher);

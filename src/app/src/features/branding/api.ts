import type { SaveWorkspaceBrandingRequest, WorkspaceBranding } from "./types";

export class BrandingApiError extends Error {}

export type BrandingFetcher = (input: string, init?: RequestInit) => Promise<Response>;
const defaultFetcher: BrandingFetcher = (input, init) => fetch(input, init);

async function request(input: string, init: RequestInit | undefined, fetcher: BrandingFetcher): Promise<WorkspaceBranding> {
  const response = await fetcher(input, init);
  const body = (await response.json().catch(() => null)) as unknown;
  if (!response.ok) {
    const message = typeof body === "object" && body !== null && "message" in body && typeof body.message === "string"
      ? body.message : "Workspace branding request failed.";
    throw new BrandingApiError(message);
  }
  return body as WorkspaceBranding;
}

export function getCurrentBranding(fetcher: BrandingFetcher = defaultFetcher) {
  return request("/api/branding/current", { credentials: "include" }, fetcher);
}

export function getPublicBranding(tenant: string, workspace: string, fetcher: BrandingFetcher = defaultFetcher) {
  const query = new URLSearchParams({ tenant, workspace });
  return request(`/api/branding/public?${query.toString()}`, undefined, fetcher);
}

export function saveCurrentBranding(value: SaveWorkspaceBrandingRequest, fetcher: BrandingFetcher = defaultFetcher) {
  return request("/api/branding/current", {
    method: "PUT",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(value)
  }, fetcher);
}

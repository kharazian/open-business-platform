import type { CustomDomain } from "./types";

export class DomainsApiError extends Error {}
export type DomainsFetcher = (input: string, init?: RequestInit) => Promise<Response>;
const defaultFetcher: DomainsFetcher = (input, init) => fetch(input, init);
async function request<T>(input: string, init: RequestInit | undefined, fetcher: DomainsFetcher): Promise<T> {
  const response = await fetcher(input, init); const body = (await response.json().catch(() => null)) as unknown;
  if (!response.ok) { const message = typeof body === "object" && body !== null && "message" in body && typeof body.message === "string" ? body.message : "Custom-domain request failed."; throw new DomainsApiError(message); }
  return body as T;
}
export async function listCustomDomains(fetcher: DomainsFetcher = defaultFetcher) { const body = await request<{ items: CustomDomain[] }>("/api/custom-domains", { credentials: "include" }, fetcher); return body.items; }
export const createCustomDomain = (hostname: string, fetcher: DomainsFetcher = defaultFetcher) => request<CustomDomain>("/api/custom-domains", { method: "POST", credentials: "include", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ hostname }) }, fetcher);
export const mutateCustomDomain = (item: CustomDomain, action: "check" | "enable" | "disable" | "rotate", fetcher: DomainsFetcher = defaultFetcher) => request<CustomDomain>(`/api/custom-domains/${encodeURIComponent(item.id)}/${action}`, { method: "POST", credentials: "include", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ concurrencyStamp: item.concurrencyStamp }) }, fetcher);

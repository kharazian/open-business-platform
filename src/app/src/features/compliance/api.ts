import type { ComplianceAuditFilters, ComplianceAuditPage, CompliancePosture } from "./types";

export class ComplianceApiError extends Error {}
export type ComplianceFetcher = (input: string, init?: RequestInit) => Promise<Response>;
const defaultFetcher: ComplianceFetcher = (input, init) => fetch(input, init);
function queryString(filters: ComplianceAuditFilters) { const query = new URLSearchParams(); Object.entries(filters).forEach(([key, value]) => { if (value !== undefined && value !== "") query.set(key, String(value)); }); return query.toString(); }
async function json<T>(input: string, fetcher: ComplianceFetcher) { const response = await fetcher(input, { credentials: "include" }); const body = (await response.json().catch(() => null)) as unknown; if (!response.ok) { const message = typeof body === "object" && body !== null && "message" in body && typeof body.message === "string" ? body.message : "Compliance request failed."; throw new ComplianceApiError(message); } return body as T; }
export const getCompliancePosture = (fetcher: ComplianceFetcher = defaultFetcher) => json<CompliancePosture>("/api/compliance/posture", fetcher);
export const searchComplianceAudit = (filters: ComplianceAuditFilters, fetcher: ComplianceFetcher = defaultFetcher) => json<ComplianceAuditPage>(`/api/compliance/audit?${queryString(filters)}`, fetcher);
export async function exportComplianceAudit(filters: ComplianceAuditFilters, fetcher: ComplianceFetcher = defaultFetcher) { const response = await fetcher(`/api/compliance/audit/export?${queryString(filters)}`, { credentials: "include" }); if (!response.ok) { const body = await response.json().catch(() => null); throw new ComplianceApiError(body?.message ?? "Audit export failed."); } return { blob: await response.blob(), fileName: response.headers.get("Content-Disposition")?.match(/filename="?([^";]+)"?/)?.[1] ?? "workspace-audit.csv" }; }

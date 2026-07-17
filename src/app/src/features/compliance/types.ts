export type ComplianceControl = { key: string; title: string; status: "pass" | "warning" | "info"; summary: string };
export type CompliancePosture = { generatedAt: string; disclaimer: string; controls: ComplianceControl[] };
export type ComplianceAuditEntry = { id: string; entityType: string; entityId: string; action: string; userId: string | null; metadata: Record<string, unknown> | null; createdAt: string };
export type ComplianceAuditPage = { items: ComplianceAuditEntry[]; page: number; pageSize: number; total: number };
export type ComplianceAuditFilters = { from?: string; to?: string; entityType?: string; action?: string; userId?: string; page?: number; pageSize?: number };

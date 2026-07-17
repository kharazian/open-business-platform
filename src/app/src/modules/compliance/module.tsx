import { lazy } from "react";
import { ShieldCheck } from "lucide-react";
import type { PlatformModule } from "../../platform/moduleRegistry";

const CompliancePage = lazy(() => import("../../features/compliance/pages/CompliancePage").then((module) => ({ default: module.CompliancePage })));
export const complianceModule: PlatformModule = { id: "enterprise.compliance", name: "Compliance", owner: "core", order: 90, routes: [{ path: "/compliance", element: <CompliancePage />, permission: "compliance.manage" }], navigation: [{ label: "Compliance & Audit", path: "/compliance", icon: ShieldCheck, order: 90, permission: "compliance.manage" }] };

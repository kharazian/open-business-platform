import { lazy } from "react";
import { ClipboardCheck, GitBranch } from "lucide-react";
import type { PlatformModule } from "../../platform/moduleRegistry";

const WorkflowsPage = lazy(() => import("../../features/workflows/pages/WorkflowsPage").then((module) => ({ default: module.WorkflowsPage })));
const WorkflowApprovalsPage = lazy(() => import("../../features/workflows/pages/WorkflowApprovalsPage").then((module) => ({ default: module.WorkflowApprovalsPage })));

export const workflowsModule: PlatformModule = {
  id: "app.workflows",
  name: "Workflows",
  owner: "app",
  order: 70,
  routes: [
    { path: "/workflows", element: <WorkflowsPage />, permission: "menu.forms" },
    { path: "/workflow-approvals", element: <WorkflowApprovalsPage /> }
  ],
  navigation: [
    { label: "Workflows", path: "/workflows", icon: GitBranch, order: 70, permission: "menu.forms" },
    { label: "Approvals", path: "/workflow-approvals", icon: ClipboardCheck, order: 71 }
  ]
};

import { lazy } from "react";
import { GitBranch } from "lucide-react";
import type { PlatformModule } from "../../platform/moduleRegistry";

const WorkflowsPage = lazy(() => import("../../features/workflows/pages/WorkflowsPage").then((module) => ({ default: module.WorkflowsPage })));

export const workflowsModule: PlatformModule = {
  id: "app.workflows",
  name: "Workflows",
  owner: "app",
  order: 70,
  routes: [{ path: "/workflows", element: <WorkflowsPage />, permission: "menu.forms" }],
  navigation: [{ label: "Workflows", path: "/workflows", icon: GitBranch, order: 70, permission: "menu.forms" }]
};

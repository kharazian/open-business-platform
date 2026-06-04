import { lazy } from "react";
import { Workflow } from "lucide-react";
import type { PlatformModule } from "../../platform/moduleRegistry";

const TriggersPage = lazy(() => import("../../features/triggers/pages/TriggersPage").then((module) => ({ default: module.TriggersPage })));

export const triggersModule: PlatformModule = {
  id: "app.triggers",
  name: "Triggers",
  owner: "app",
  order: 60,
  routes: [{ path: "/triggers", element: <TriggersPage />, permission: "menu.forms" }],
  navigation: [{ label: "Triggers", path: "/triggers", icon: Workflow, order: 60, permission: "menu.forms" }]
};

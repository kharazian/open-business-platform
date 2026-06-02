import { Workflow } from "lucide-react";
import { TriggersPage } from "../../features/triggers/pages/TriggersPage";
import type { PlatformModule } from "../../platform/moduleRegistry";

export const triggersModule: PlatformModule = {
  id: "app.triggers",
  name: "Triggers",
  owner: "app",
  order: 60,
  routes: [{ path: "/triggers", element: <TriggersPage />, permission: "menu.forms" }],
  navigation: [{ label: "Triggers", path: "/triggers", icon: Workflow, order: 60, permission: "menu.forms" }]
};

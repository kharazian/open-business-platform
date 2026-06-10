import { lazy } from "react";
import { PlugZap } from "lucide-react";
import type { PlatformModule } from "../../platform/moduleRegistry";

const IntegrationsPage = lazy(() => import("../../features/integrations/pages/IntegrationsPage").then((module) => ({ default: module.IntegrationsPage })));

export const integrationsModule: PlatformModule = {
  id: "app.integrations",
  name: "Integrations",
  owner: "app",
  order: 85,
  routes: [{ path: "/integrations", element: <IntegrationsPage />, permission: "integrations.manage" }],
  navigation: [{ label: "Integrations", path: "/integrations", icon: PlugZap, order: 85, permission: "integrations.manage" }]
};

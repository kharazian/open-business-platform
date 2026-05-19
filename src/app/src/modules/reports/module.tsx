import { FileText } from "lucide-react";
import { Reports } from "../../pages/Reports";
import type { PlatformModule } from "../../platform/moduleRegistry";

export const reportsModule: PlatformModule = {
  id: "core.reports",
  name: "Reports",
  owner: "core",
  order: 50,
  routes: [{ path: "/reports", element: <Reports />, permission: "menu.reports" }],
  navigation: [{ label: "Reports", path: "/reports", icon: FileText, order: 50, permission: "menu.reports" }]
};

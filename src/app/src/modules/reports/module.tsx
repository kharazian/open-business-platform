import { FileText } from "lucide-react";
import { ReportsPage } from "../../features/reports/pages/ReportsPage";
import type { PlatformModule } from "../../platform/moduleRegistry";

export const reportsModule: PlatformModule = {
  id: "core.reports",
  name: "Reports (V2 preview)",
  owner: "core",
  order: 50,
  routes: [{ path: "/reports", element: <ReportsPage />, permission: "menu.reports" }],
  navigation: [{ label: "Reports (V2 preview)", path: "/reports", icon: FileText, order: 50, permission: "menu.reports" }]
};

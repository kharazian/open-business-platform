import { BarChart3, FileText } from "lucide-react";
import { ChartBuilderPage } from "../../features/dashboards/pages/ChartBuilderPage";
import { ReportsPage } from "../../features/reports/pages/ReportsPage";
import type { PlatformModule } from "../../platform/moduleRegistry";

export const reportsModule: PlatformModule = {
  id: "core.reports",
  name: "Reports (V2 preview)",
  owner: "core",
  order: 50,
  routes: [
    { path: "/reports", element: <ReportsPage />, permission: "menu.reports" },
    { path: "/charts", element: <ChartBuilderPage />, permission: "menu.reports" }
  ],
  navigation: [
    { label: "Reports (V2 preview)", path: "/reports", icon: FileText, order: 50, permission: "menu.reports" },
    { label: "Charts (V2 preview)", path: "/charts", icon: BarChart3, order: 55, permission: "menu.reports" }
  ]
};

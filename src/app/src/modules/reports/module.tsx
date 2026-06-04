import { lazy } from "react";
import { BarChart3, FileText } from "lucide-react";
import type { PlatformModule } from "../../platform/moduleRegistry";

const ChartBuilderPage = lazy(() => import("../../features/dashboards/pages/ChartBuilderPage").then((module) => ({ default: module.ChartBuilderPage })));
const ReportsPage = lazy(() => import("../../features/reports/pages/ReportsPage").then((module) => ({ default: module.ReportsPage })));

export const reportsModule: PlatformModule = {
  id: "core.reports",
  name: "Reports",
  owner: "core",
  order: 50,
  routes: [
    { path: "/reports", element: <ReportsPage />, permission: "menu.reports" },
    { path: "/charts", element: <ChartBuilderPage />, permission: "menu.reports" }
  ],
  navigation: [
    { label: "Reports", path: "/reports", icon: FileText, order: 50, permission: "menu.reports" },
    { label: "Charts", path: "/charts", icon: BarChart3, order: 55, permission: "menu.reports" }
  ]
};

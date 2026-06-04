import { lazy } from "react";
import { Home, LayoutDashboard, PanelsTopLeft } from "lucide-react";
import type { PlatformModule } from "../../platform/moduleRegistry";

const Dashboard = lazy(() => import("../../pages/Dashboard").then((module) => ({ default: module.Dashboard })));
const DashboardsPage = lazy(() => import("../../features/dashboards/pages/DashboardsPage").then((module) => ({ default: module.DashboardsPage })));

export const dashboardModule: PlatformModule = {
  id: "core.dashboard",
  name: "Dashboard",
  owner: "core",
  order: 10,
  routes: [
    { index: true, element: <Dashboard />, permission: "menu.dashboard" },
    { path: "/dashboard", element: <Dashboard />, permission: "menu.dashboard" },
    { path: "/dashboards", element: <DashboardsPage />, permission: "menu.dashboard" }
  ],
  navigation: [
    { label: "Home", path: "/", icon: Home, order: 10, permission: "menu.dashboard" },
    { label: "Dashboard", path: "/dashboard", icon: LayoutDashboard, order: 20, permission: "menu.dashboard" },
    { label: "Dashboards", path: "/dashboards", icon: PanelsTopLeft, order: 25, permission: "menu.dashboard" }
  ]
};

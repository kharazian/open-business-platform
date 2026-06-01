import { Home, LayoutDashboard, PanelsTopLeft } from "lucide-react";
import { DashboardsPage } from "../../features/dashboards/pages/DashboardsPage";
import { Dashboard } from "../../pages/Dashboard";
import type { PlatformModule } from "../../platform/moduleRegistry";

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

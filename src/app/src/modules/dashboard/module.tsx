import { lazy } from "react";
import { Home, LayoutDashboard, PanelsTopLeft } from "lucide-react";
import type { PlatformModule } from "../../platform/moduleRegistry";
import "../../features/dashboards/sampleDashboardAdapter";

const Dashboard = lazy(() => import("../../pages/Dashboard").then((module) => ({ default: module.Dashboard })));
const DashboardsPage = lazy(() => import("../../features/dashboards/pages/DashboardsPage").then((module) => ({ default: module.DashboardsPage })));
const DashboardDirectoryPage = lazy(() => import("../../features/dashboards/pages/DashboardDirectoryPage").then((module) => ({ default: module.DashboardDirectoryPage })));
const DashboardViewerPage = lazy(() => import("../../features/dashboards/pages/DashboardViewerPage").then((module) => ({ default: module.DashboardViewerPage })));

export const dashboardModule: PlatformModule = {
  id: "core.dashboard",
  name: "Dashboard",
  owner: "core",
  order: 10,
  routes: [
    { index: true, element: <Dashboard />, permission: "menu.dashboard" },
    { path: "/dashboard", element: <Dashboard />, permission: "menu.dashboard" },
    { path: "/dashboards", element: <DashboardDirectoryPage />, permission: "menu.dashboard" },
    { path: "/dashboards/:slug", element: <DashboardViewerPage />, permission: "menu.dashboard" },
    { path: "/dashboard-builder", element: <DashboardsPage />, permission: "dashboards.manage" },
    { path: "/dashboard-builder/:id", element: <DashboardsPage />, permission: "dashboards.manage" }
  ],
  navigation: [
    { label: "Home", path: "/", icon: Home, order: 10, permission: "menu.dashboard" },
    { label: "Dashboard", path: "/dashboard", icon: LayoutDashboard, order: 20, permission: "menu.dashboard" },
    { label: "Dashboards", path: "/dashboards", icon: PanelsTopLeft, order: 25, permission: "menu.dashboard" }
  ]
};

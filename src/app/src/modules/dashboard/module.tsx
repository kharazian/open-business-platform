import { Home, LayoutDashboard } from "lucide-react";
import { Dashboard } from "../../pages/Dashboard";
import type { PlatformModule } from "../../platform/moduleRegistry";

export const dashboardModule: PlatformModule = {
  id: "core.dashboard",
  name: "Dashboard",
  owner: "core",
  order: 10,
  routes: [
    { index: true, element: <Dashboard /> },
    { path: "/dashboard", element: <Dashboard /> }
  ],
  navigation: [
    { label: "Home", path: "/", icon: Home, order: 10 },
    { label: "Dashboard", path: "/dashboard", icon: LayoutDashboard, order: 20 }
  ]
};

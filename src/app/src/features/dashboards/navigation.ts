import { Activity, BriefcaseBusiness, ChartColumn, ChartLine, Factory, Landmark, LayoutDashboard, type LucideIcon } from "lucide-react";
import type { NavigationItem } from "../../platform/moduleRegistry";
import type { DashboardNavigationItem } from "./types";

const approvedDashboardIcons: Record<string, LucideIcon> = {
  activity: Activity,
  "briefcase-business": BriefcaseBusiness,
  "chart-column": ChartColumn,
  "chart-line": ChartLine,
  factory: Factory,
  landmark: Landmark,
  "layout-dashboard": LayoutDashboard
};

export function resolveDashboardIcon(name?: string | null) {
  return name ? approvedDashboardIcons[name] : undefined;
}

export function applyDashboardNavigation(navigation: NavigationItem[], dashboards: DashboardNavigationItem[]): NavigationItem[] {
  return navigation.map((item) => item.path === "/dashboards" ? {
    ...item,
    path: undefined,
    children: [
      { label: "Dashboard directory", path: "/dashboards" },
      ...dashboards.map((dashboard) => ({ label: dashboard.label, path: `/dashboards/${dashboard.slug}`, icon: resolveDashboardIcon(dashboard.icon) }))
    ]
  } : item);
}

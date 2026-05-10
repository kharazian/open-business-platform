import type { ReactElement } from "react";
import {
  BarChart3,
  Boxes,
  Component,
  KeyRound,
  LayoutDashboard,
  LogIn,
  MonitorCog,
  ScrollText,
  Settings,
  ShieldCheck,
  UserCircle,
  Users
} from "lucide-react";
import type { NavigationItem } from "../../config/appNavigation";
import { ThemeAuditLogs } from "../pages/ThemeAuditLogs";
import { ThemeComponents } from "../pages/ThemeComponents";
import { ThemeDashboard } from "../pages/ThemeDashboard";
import { ThemeLayouts } from "../pages/ThemeLayouts";
import { ThemeLogin } from "../pages/ThemeLogin";
import { ThemePermissions } from "../pages/ThemePermissions";
import { ThemeProfile } from "../pages/ThemeProfile";
import { ThemeReports } from "../pages/ThemeReports";
import { ThemeRoles } from "../pages/ThemeRoles";
import { ThemeSettings } from "../pages/ThemeSettings";
import { ThemeUsers } from "../pages/ThemeUsers";

export type ThemePageGroup = "Dashboard" | "Workspace" | "Theme" | "Authentication";

export type ThemePage = {
  label: string;
  path: string;
  routePath?: string;
  index?: boolean;
  group: ThemePageGroup;
  icon: NonNullable<NavigationItem["icon"]>;
  element: ReactElement;
};

export const themePages: ThemePage[] = [
  {
    label: "Dashboard",
    path: "/theme",
    index: true,
    group: "Dashboard",
    icon: LayoutDashboard,
    element: <ThemeDashboard />
  },
  {
    label: "Users",
    path: "/theme/users",
    routePath: "users",
    group: "Workspace",
    icon: Users,
    element: <ThemeUsers />
  },
  {
    label: "Roles",
    path: "/theme/roles",
    routePath: "roles",
    group: "Workspace",
    icon: ShieldCheck,
    element: <ThemeRoles />
  },
  {
    label: "Permissions",
    path: "/theme/permissions",
    routePath: "permissions",
    group: "Workspace",
    icon: KeyRound,
    element: <ThemePermissions />
  },
  {
    label: "Audit Logs",
    path: "/theme/audit-logs",
    routePath: "audit-logs",
    group: "Workspace",
    icon: ScrollText,
    element: <ThemeAuditLogs />
  },
  {
    label: "Reports",
    path: "/theme/reports",
    routePath: "reports",
    group: "Workspace",
    icon: BarChart3,
    element: <ThemeReports />
  },
  {
    label: "Settings",
    path: "/theme/settings",
    routePath: "settings",
    group: "Workspace",
    icon: Settings,
    element: <ThemeSettings />
  },
  {
    label: "Profile",
    path: "/theme/profile",
    routePath: "profile",
    group: "Workspace",
    icon: UserCircle,
    element: <ThemeProfile />
  },
  {
    label: "Login",
    path: "/theme/login",
    routePath: "login",
    group: "Authentication",
    icon: LogIn,
    element: <ThemeLogin />
  },
  {
    label: "Layouts",
    path: "/theme/layouts",
    routePath: "layouts",
    group: "Theme",
    icon: MonitorCog,
    element: <ThemeLayouts />
  },
  {
    label: "Components",
    path: "/theme/components",
    routePath: "components",
    group: "Theme",
    icon: Component,
    element: <ThemeComponents />
  }
];

export const themeNavigation: NavigationItem[] = [
  ...themePages.map(({ label, path, icon, group }) => ({ label, path, icon, section: group })),
  { label: "Back to App", path: "/", icon: Boxes, section: "Links", external: true }
];

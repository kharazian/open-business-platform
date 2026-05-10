import type { ReactElement } from "react";
import {
  BarChart3,
  Bell,
  Boxes,
  CalendarDays,
  ClipboardList,
  Component,
  KeyRound,
  LayoutDashboard,
  LogIn,
  MailQuestion,
  MonitorCog,
  PanelsTopLeft,
  ScrollText,
  Settings,
  ShieldCheck,
  Table2,
  UserCircle,
  UserPlus,
  Users
} from "lucide-react";
import type { NavigationItem } from "../../config/appNavigation";
import { ThemeAuditLogs } from "../pages/ThemeAuditLogs";
import { ThemeCalendar } from "../pages/ThemeCalendar";
import { ThemeComponents } from "../pages/ThemeComponents";
import { ThemeDashboard } from "../pages/ThemeDashboard";
import { ThemeForgotPassword } from "../pages/ThemeForgotPassword";
import { ThemeForms } from "../pages/ThemeForms";
import { ThemeLayouts } from "../pages/ThemeLayouts";
import { ThemeLogin } from "../pages/ThemeLogin";
import { ThemeMfa } from "../pages/ThemeMfa";
import { ThemeNotifications } from "../pages/ThemeNotifications";
import { ThemePermissions } from "../pages/ThemePermissions";
import { ThemeProfile } from "../pages/ThemeProfile";
import { ThemeRegister } from "../pages/ThemeRegister";
import { ThemeReports } from "../pages/ThemeReports";
import { ThemeResetPassword } from "../pages/ThemeResetPassword";
import { ThemeRoles } from "../pages/ThemeRoles";
import { ThemeSettings } from "../pages/ThemeSettings";
import { ThemeTables } from "../pages/ThemeTables";
import { ThemeUtilityPages } from "../pages/ThemeUtilityPages";
import { ThemeUsers } from "../pages/ThemeUsers";

export type ThemePageGroup = "Dashboard" | "Workspace" | "Foundation" | "Theme" | "Authentication";

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
    label: "Notifications",
    path: "/theme/notifications",
    routePath: "notifications",
    group: "Workspace",
    icon: Bell,
    element: <ThemeNotifications />
  },
  {
    label: "Calendar",
    path: "/theme/calendar",
    routePath: "calendar",
    group: "Workspace",
    icon: CalendarDays,
    element: <ThemeCalendar />
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
    label: "Forms",
    path: "/theme/forms",
    routePath: "forms",
    group: "Foundation",
    icon: ClipboardList,
    element: <ThemeForms />
  },
  {
    label: "Tables",
    path: "/theme/tables",
    routePath: "tables",
    group: "Foundation",
    icon: Table2,
    element: <ThemeTables />
  },
  {
    label: "Utility Pages",
    path: "/theme/utility-pages",
    routePath: "utility-pages",
    group: "Foundation",
    icon: PanelsTopLeft,
    element: <ThemeUtilityPages />
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
    label: "Register",
    path: "/theme/register",
    routePath: "register",
    group: "Authentication",
    icon: UserPlus,
    element: <ThemeRegister />
  },
  {
    label: "Forgot Password",
    path: "/theme/forgot-password",
    routePath: "forgot-password",
    group: "Authentication",
    icon: MailQuestion,
    element: <ThemeForgotPassword />
  },
  {
    label: "Reset Password",
    path: "/theme/reset-password",
    routePath: "reset-password",
    group: "Authentication",
    icon: KeyRound,
    element: <ThemeResetPassword />
  },
  {
    label: "MFA",
    path: "/theme/mfa",
    routePath: "mfa",
    group: "Authentication",
    icon: ShieldCheck,
    element: <ThemeMfa />
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

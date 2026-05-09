import {
  BarChart3,
  Boxes,
  Component,
  LayoutDashboard,
  LogIn,
  MonitorCog,
  Settings,
  UserCircle,
  Users
} from "lucide-react";
import type { NavigationItem } from "../../config/appNavigation";

export const themeNavigation: NavigationItem[] = [
  { label: "Dashboard", path: "/theme", icon: LayoutDashboard },
  { label: "Users", path: "/theme/users", icon: Users },
  { label: "Reports", path: "/theme/reports", icon: BarChart3 },
  { label: "Settings", path: "/theme/settings", icon: Settings },
  { label: "Profile", path: "/theme/profile", icon: UserCircle },
  { label: "Login", path: "/theme/login", icon: LogIn },
  { label: "Layouts", path: "/theme/layouts", icon: MonitorCog },
  { label: "Components", path: "/theme/components", icon: Component },
  { label: "Back to App", path: "/", icon: Boxes, external: true }
];

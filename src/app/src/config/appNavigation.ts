import { Home, LayoutDashboard, Settings, UserCircle, Palette } from "lucide-react";

export type NavigationItem = {
  label: string;
  path: string;
  icon?: typeof Home;
  section?: string;
  external?: boolean;
};

export const appNavigation: NavigationItem[] = [
  { label: "Home", path: "/", icon: Home },
  { label: "Dashboard", path: "/dashboard", icon: LayoutDashboard },
  { label: "Settings", path: "/settings", icon: Settings },
  { label: "Profile", path: "/profile", icon: UserCircle },
  { label: "Theme Playground", path: "/theme", icon: Palette }
];

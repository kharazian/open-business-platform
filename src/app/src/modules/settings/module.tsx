import { lazy } from "react";
import { Palette, Settings } from "lucide-react";
import type { PlatformModule } from "../../platform/moduleRegistry";

const SettingsPage = lazy(() => import("../../pages/Settings").then((module) => ({ default: module.Settings })));

export const settingsModule: PlatformModule = {
  id: "core.settings",
  name: "Settings",
  owner: "core",
  order: 80,
  routes: [{ path: "/settings", element: <SettingsPage />, permission: "menu.settings" }],
  navigation: [
    { label: "Settings", path: "/settings", icon: Settings, order: 80, permission: "menu.settings" },
    { label: "Theme Playground", path: "/theme", icon: Palette, order: 100, permission: "menu.settings" }
  ]
};

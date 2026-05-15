import { Palette, Settings } from "lucide-react";
import { Settings as SettingsPage } from "../../pages/Settings";
import type { PlatformModule } from "../../platform/moduleRegistry";

export const settingsModule: PlatformModule = {
  id: "core.settings",
  name: "Settings",
  owner: "core",
  order: 80,
  routes: [{ path: "/settings", element: <SettingsPage /> }],
  navigation: [
    { label: "Settings", path: "/settings", icon: Settings, order: 80 },
    { label: "Theme Playground", path: "/theme", icon: Palette, order: 100 }
  ]
};

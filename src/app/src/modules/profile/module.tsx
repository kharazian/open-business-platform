import { lazy } from "react";
import { UserCircle } from "lucide-react";
import type { PlatformModule } from "../../platform/moduleRegistry";

const Profile = lazy(() => import("../../pages/Profile").then((module) => ({ default: module.Profile })));

export const profileModule: PlatformModule = {
  id: "core.profile",
  name: "Profile",
  owner: "core",
  order: 90,
  routes: [{ path: "/profile", element: <Profile />, permission: "menu.profile" }],
  navigation: [{ label: "Profile", path: "/profile", icon: UserCircle, order: 90, permission: "menu.profile" }]
};

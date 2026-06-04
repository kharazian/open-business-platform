import { lazy } from "react";
import { UserCircle } from "lucide-react";
import type { PlatformModule } from "../../platform/moduleRegistry";

const Users = lazy(() => import("../../pages/Users").then((module) => ({ default: module.Users })));

export const usersModule: PlatformModule = {
  id: "core.users",
  name: "Users",
  owner: "core",
  order: 40,
  routes: [{ path: "/users", element: <Users />, permission: "menu.users_access" }],
  navigation: [{ label: "Users & Access", path: "/users", icon: UserCircle, order: 40, permission: "menu.users_access" }]
};

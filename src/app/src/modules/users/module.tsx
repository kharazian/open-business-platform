import { UserCircle } from "lucide-react";
import { Users } from "../../pages/Users";
import type { PlatformModule } from "../../platform/moduleRegistry";

export const usersModule: PlatformModule = {
  id: "core.users",
  name: "Users",
  owner: "core",
  order: 40,
  routes: [{ path: "/users", element: <Users />, permission: "menu.users_access" }],
  navigation: [{ label: "Users & Access", path: "/users", icon: UserCircle, order: 40, permission: "menu.users_access" }]
};

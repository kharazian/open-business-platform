import { UserCircle } from "lucide-react";
import { Profile } from "../../pages/Profile";
import type { PlatformModule } from "../../platform/moduleRegistry";

export const profileModule: PlatformModule = {
  id: "core.profile",
  name: "Profile",
  owner: "core",
  order: 90,
  routes: [{ path: "/profile", element: <Profile /> }],
  navigation: [{ label: "Profile", path: "/profile", icon: UserCircle, order: 90 }]
};

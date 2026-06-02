import { Bell } from "lucide-react";
import { NotificationsPage } from "../../features/notifications/pages/NotificationsPage";
import type { PlatformModule } from "../../platform/moduleRegistry";

export const notificationsModule: PlatformModule = {
  id: "app.notifications",
  name: "Notifications",
  owner: "app",
  order: 65,
  routes: [{ path: "/notifications", element: <NotificationsPage /> }],
  navigation: [{ label: "Notifications", path: "/notifications", icon: Bell, order: 65 }]
};

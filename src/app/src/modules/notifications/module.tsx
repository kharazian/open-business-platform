import { lazy } from "react";
import { Bell } from "lucide-react";
import type { PlatformModule } from "../../platform/moduleRegistry";

const NotificationsPage = lazy(() => import("../../features/notifications/pages/NotificationsPage").then((module) => ({ default: module.NotificationsPage })));

export const notificationsModule: PlatformModule = {
  id: "app.notifications",
  name: "Notifications",
  owner: "app",
  order: 65,
  routes: [{ path: "/notifications", element: <NotificationsPage /> }],
  navigation: [{ label: "Notifications", path: "/notifications", icon: Bell, order: 65 }]
};

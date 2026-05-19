import { ClipboardList } from "lucide-react";
import { FormsListPage } from "../../features/forms/pages/FormsListPage";
import type { PlatformModule } from "../../platform/moduleRegistry";

export const formsModule: PlatformModule = {
  id: "app.forms",
  name: "Forms",
  owner: "app",
  order: 20,
  routes: [{ path: "/forms", element: <FormsListPage />, permission: "menu.forms" }],
  navigation: [{ label: "Forms", path: "/forms", icon: ClipboardList, order: 30, permission: "menu.forms" }]
};

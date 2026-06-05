import { lazy } from "react";
import { Printer } from "lucide-react";
import type { PlatformModule } from "../../platform/moduleRegistry";

const PrintingPage = lazy(() => import("../../features/printing/pages/PrintingPage").then((module) => ({ default: module.PrintingPage })));

export const printingModule: PlatformModule = {
  id: "app.printing",
  name: "Printing",
  owner: "app",
  order: 65,
  routes: [
    { path: "/printing", element: <PrintingPage />, permission: "menu.reports" }
  ],
  navigation: [
    { label: "Printing", path: "/printing", icon: Printer, order: 65, permission: "menu.reports" }
  ]
};

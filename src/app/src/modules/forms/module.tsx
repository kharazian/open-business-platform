import { lazy } from "react";
import { ClipboardList } from "lucide-react";
import type { PlatformModule } from "../../platform/moduleRegistry";

const FormBuilderPage = lazy(() => import("../../features/forms/pages/FormBuilderPage").then((module) => ({ default: module.FormBuilderPage })));
const FormsListPage = lazy(() => import("../../features/forms/pages/FormsListPage").then((module) => ({ default: module.FormsListPage })));
const SubmitFormPage = lazy(() => import("../../features/forms/pages/SubmitFormPage").then((module) => ({ default: module.SubmitFormPage })));
const RecordDetailPage = lazy(() => import("../../features/records/pages/RecordDetailPage").then((module) => ({ default: module.RecordDetailPage })));
const RecordListPage = lazy(() => import("../../features/records/pages/RecordListPage").then((module) => ({ default: module.RecordListPage })));

export const formsModule: PlatformModule = {
  id: "app.forms",
  name: "Forms",
  owner: "app",
  order: 20,
  routes: [
    { path: "/forms", element: <FormsListPage />, permission: "menu.forms" },
    { path: "/forms/:formId/builder", element: <FormBuilderPage />, permission: "menu.forms" },
    { path: "/forms/:formId/submit", element: <SubmitFormPage />, permission: "menu.forms" },
    { path: "/forms/:formId/records", element: <RecordListPage />, permission: "menu.forms" },
    { path: "/records/:recordId", element: <RecordDetailPage />, permission: "menu.forms" }
  ],
  navigation: [{ label: "Forms", path: "/forms", icon: ClipboardList, order: 30, permission: "menu.forms" }]
};

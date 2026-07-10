import assert from "node:assert/strict";
import { test } from "vitest";
import {
  grantReportAccessBundle,
  hasFormAccessPermission,
  hasReportAccessPermission,
  reportAccessFormActions,
  reportAccessMenuPermission,
  reportAccessPlatformManagePermission,
  setFormAccessPermission,
  setGlobalPermission,
  setReportAccessPermission,
  rolePermissionDraftChanged
} from "./reportAccess.ts";

test("report access helpers toggle menu and report permissions predictably", () => {
  assert.equal(reportAccessMenuPermission, "menu.reports");
  assert.deepEqual(setGlobalPermission(["menu.forms"], reportAccessMenuPermission, true), ["menu.forms", "menu.reports"]);
  assert.deepEqual(setGlobalPermission(["menu.forms", "menu.reports"], reportAccessMenuPermission, false), ["menu.forms"]);

  const granted = setReportAccessPermission([{ reportId: "report-2", action: "view" }], "report-1", "export", true);

  assert.deepEqual(granted, [
    { reportId: "report-1", action: "export" },
    { reportId: "report-2", action: "view" }
  ]);
  assert.equal(hasReportAccessPermission(granted, "report-1", "export"), true);
  assert.deepEqual(setReportAccessPermission(granted, "report-1", "export", false), [{ reportId: "report-2", action: "view" }]);
});

test("report access helpers grant required source form dependencies", () => {
  const draft = {
    roleId: "role-1",
    permissions: [],
    formPermissions: [],
    reportPermissions: [],
    fieldPermissions: []
  };

  assert.deepEqual(reportAccessFormActions, ["view", "export", "manage"]);

  const viewGrant = grantReportAccessBundle(draft, "form-1", "report-1", "view", true);
  assert.deepEqual(viewGrant.permissions, [reportAccessMenuPermission]);
  assert.equal(hasFormAccessPermission(viewGrant.formPermissions, "form-1", "view"), true);
  assert.equal(hasReportAccessPermission(viewGrant.reportPermissions, "report-1", "view"), true);

  const exportGrant = grantReportAccessBundle(draft, "form-1", "report-1", "export", true);
  assert.equal(hasFormAccessPermission(exportGrant.formPermissions, "form-1", "view"), true);
  assert.equal(hasFormAccessPermission(exportGrant.formPermissions, "form-1", "export"), true);
  assert.equal(hasReportAccessPermission(exportGrant.reportPermissions, "report-1", "view"), true);
  assert.equal(hasReportAccessPermission(exportGrant.reportPermissions, "report-1", "export"), true);

  const manageGrant = grantReportAccessBundle(draft, "form-1", "report-1", "manage", true);
  assert.equal(manageGrant.permissions.includes(reportAccessPlatformManagePermission), true);
  assert.equal(hasFormAccessPermission(manageGrant.formPermissions, "form-1", "manage"), true);
  assert.equal(hasReportAccessPermission(manageGrant.reportPermissions, "report-1", "manage"), true);

  assert.deepEqual(setFormAccessPermission(manageGrant.formPermissions, "form-1", "manage", false), []);
});

test("report access helpers detect dirty role permission drafts", () => {
  const original = {
    roleId: "role-1",
    permissions: ["menu.forms"],
    formPermissions: [],
    reportPermissions: [{ reportId: "report-1", action: "view" }],
    fieldPermissions: []
  };
  const unchanged = {
    ...original,
    permissions: ["menu.forms"],
    reportPermissions: [{ reportId: "report-1", action: "view" }]
  };
  const changed = {
    ...original,
    permissions: ["menu.forms", "menu.reports"],
    reportPermissions: [{ reportId: "report-1", action: "view" }]
  };

  assert.equal(rolePermissionDraftChanged(original, unchanged), false);
  assert.equal(rolePermissionDraftChanged(original, changed), true);
});

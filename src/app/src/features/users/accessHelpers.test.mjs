import assert from "node:assert/strict";
import { test } from "vitest";
import {
  normalizeScopedFormPermissions,
  toggleFieldPermission,
  toggleReportPermission
} from "./accessHelpers.ts";

test("scoped form permissions default missing scopes to all and sort payloads", () => {
  const normalized = normalizeScopedFormPermissions([
    { formId: "form-b", action: "view" },
    { formId: "form-a", action: "edit", scope: "own" }
  ]);

  assert.deepEqual(normalized, [
    { formId: "form-a", action: "edit", scope: "own" },
    { formId: "form-b", action: "view", scope: "all" }
  ]);
});

test("report and field helper toggles produce stable permission payloads", () => {
  const reportPermissions = toggleReportPermission([], "report-1", "export");
  assert.deepEqual(reportPermissions, [{ reportId: "report-1", action: "export" }]);
  assert.deepEqual(toggleReportPermission(reportPermissions, "report-1", "export"), []);

  const hiddenField = toggleFieldPermission([], "form-1", "salary", "hidden");
  assert.deepEqual(hiddenField, [{ formId: "form-1", fieldId: "salary", access: "hidden" }]);
  assert.deepEqual(toggleFieldPermission(hiddenField, "form-1", "salary", "read_only"), [
    { formId: "form-1", fieldId: "salary", access: "read_only" }
  ]);
});

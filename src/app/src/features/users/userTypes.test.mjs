import assert from "node:assert/strict";
import { test } from "vitest";
import {
  accessManagementTabs,
  departmentStatuses,
  fieldAccessLevels,
  isUserStatus,
  recordAccessScopes,
  reportAccessActions,
  roleStatuses,
  userStatuses
} from "./types.ts";

test("user access types expose v3 statuses, access options, and management tabs", () => {
  assert.deepEqual(accessManagementTabs.map((tab) => tab.label), ["Users", "Roles & permissions", "Departments", "Groups"]);
  assert.equal(accessManagementTabs.some((tab) => tab.value === "permissions"), false);
  assert.deepEqual(userStatuses, ["active", "inactive"]);
  assert.deepEqual(roleStatuses, ["active", "inactive"]);
  assert.deepEqual(departmentStatuses, ["active", "inactive"]);
  assert.deepEqual(recordAccessScopes, ["all", "own", "department", "managed_department", "group", "assigned"]);
  assert.deepEqual(reportAccessActions, ["view", "export", "manage"]);
  assert.deepEqual(fieldAccessLevels, ["hidden", "read_only"]);
  assert.equal(isUserStatus("active"), true);
  assert.equal(isUserStatus("invited"), false);
});

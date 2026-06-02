import assert from "node:assert/strict";
import { test } from "vitest";
import {
  validateResetPassword,
  validateRoleDraft,
  validateUserDraft
} from "./validation.ts";

test("users validation reports visible field errors for create and reset password forms", () => {
  const emptyCreate = validateUserDraft(
    { name: "", email: "", password: "", isActive: true, roleIds: [], departmentIds: [], groupIds: [] },
    "create"
  );

  assert.equal(emptyCreate.valid, false);
  assert.equal(emptyCreate.errors.name, "Full name is required.");
  assert.equal(emptyCreate.errors.email, "Email is required.");
  assert.equal(emptyCreate.errors.password, "Password must be at least 8 characters.");

  const shortPassword = validateUserDraft(
    { name: "Jane Cooper", email: "jane@company.test", password: "simple", isActive: true, roleIds: [], departmentIds: [], groupIds: [] },
    "create"
  );

  assert.equal(shortPassword.valid, false);
  assert.equal(shortPassword.errors.password, "Password must be at least 8 characters.");

  const resetPassword = validateResetPassword("simple");
  assert.equal(resetPassword.valid, false);
  assert.equal(resetPassword.error, "Password must be at least 8 characters.");
});

test("users validation trims valid drafts and skips password validation when editing users", () => {
  const createResult = validateUserDraft(
    { name: " Jane Cooper ", email: " JANE@Company.Test ", password: "temporary-password-1", isActive: true, roleIds: ["role-1"], departmentIds: ["department-1"], groupIds: ["group-1"] },
    "create"
  );

  assert.equal(createResult.valid, true);
  assert.equal(createResult.value.name, "Jane Cooper");
  assert.equal(createResult.value.email, "jane@company.test");
  assert.equal(createResult.value.password, "temporary-password-1");
  assert.deepEqual(createResult.value.departmentIds, ["department-1"]);
  assert.deepEqual(createResult.value.groupIds, ["group-1"]);

  const editResult = validateUserDraft(
    { name: " Jane Cooper ", email: " jane@company.test ", password: "", isActive: false, roleIds: [], departmentIds: [], groupIds: [] },
    "edit"
  );

  assert.equal(editResult.valid, true);
  assert.equal(editResult.value.name, "Jane Cooper");
  assert.equal(editResult.value.password, "");

  const roleResult = validateRoleDraft({ name: " Manager ", description: " Approves requests ", isActive: true });
  assert.equal(roleResult.valid, true);
  assert.equal(roleResult.value.name, "Manager");
  assert.equal(roleResult.value.description, "Approves requests");
});

test("users validation does not block edits on the disabled email field", () => {
  const editResult = validateUserDraft(
    { name: "Jane Cooper", email: "", password: "", isActive: false, roleIds: ["role-1"], departmentIds: [], groupIds: [] },
    "edit"
  );

  assert.equal(editResult.valid, true);
  assert.equal(editResult.value.name, "Jane Cooper");
});

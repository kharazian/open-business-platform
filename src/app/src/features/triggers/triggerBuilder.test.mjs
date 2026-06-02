import assert from "node:assert/strict";
import { test } from "vitest";
import {
  buildTriggerRequest,
  createEmptyTriggerDraft,
  createTriggerDraftFromDetail,
  formatTriggerEventLabel,
  formatTriggerJson,
  formatTriggerLogStatus,
  getTriggerFieldOptions,
  validateTriggerDraft
} from "./builder.ts";

test("trigger builder creates and validates normalized trigger requests", () => {
  const draft = createEmptyTriggerDraft("Expense request");

  assert.equal(draft.name, "Expense request automation");
  assert.equal(draft.eventName, "record.created");
  assert.equal(draft.actions[0].type, "write_audit_entry");

  const invalid = validateTriggerDraft({
    ...draft,
    name: "",
    actions: [],
    conditions: [{ clientId: "condition-1", type: "field_equals", fieldId: "", value: "" }]
  });

  assert.equal(invalid.valid, false);
  assert.equal(invalid.errors.some((error) => error.path === "name"), true);
  assert.equal(invalid.errors.some((error) => error.path === "actions"), true);
  assert.equal(invalid.errors.some((error) => error.path === "conditions[0].fieldId"), true);

  const request = buildTriggerRequest({
    ...draft,
    name: " Route HR submissions ",
    description: " Move HR records. ",
    isEnabled: true,
    conditions: [
      { clientId: "condition-1", type: "field_equals", fieldId: "department", value: " HR " },
      { clientId: "condition-2", type: "status_changed_to", status: " submitted " }
    ],
    actions: [
      { clientId: "action-1", id: "audit-1", type: "write_audit_entry", message: " Matched " },
      { clientId: "action-2", id: "email-1", type: "send_email", toText: "hr@example.test, finance@example.test", subject: " New record ", body: " Please review. " },
      { clientId: "action-3", id: "assign-1", type: "assign_record", assignedToUserId: "user-1", assignedGroupId: "" },
      { clientId: "action-4", id: "field-1", type: "update_field", fieldId: "email", value: " jane@example.test " },
      {
        clientId: "action-5",
        id: "notify-1",
        type: "send_notification",
        title: " New HR record ",
        body: " Please review this record. ",
        recipientUserId: "user-1",
        recipientGroupId: "group-1"
      }
    ]
  });

  assert.equal(request.name, "Route HR submissions");
  assert.equal(request.description, "Move HR records.");
  assert.deepEqual(request.conditions.conditions, [
    { type: "field_equals", fieldId: "department", value: "HR" },
    { type: "status_changed_to", status: "submitted" }
  ]);
  assert.deepEqual(request.actions[1].to, ["hr@example.test", "finance@example.test"]);
  assert.equal(request.actions[2].assignedToUserId, "user-1");
  assert.equal("assignedGroupId" in request.actions[2], false);
  assert.equal(request.actions[3].fieldId, "email");
  assert.equal(request.actions[3].value, "jane@example.test");
  assert.equal(request.actions[4].title, "New HR record");
  assert.equal(request.actions[4].body, "Please review this record.");
  assert.deepEqual(request.actions[4].recipientUserIds, ["user-1"]);
  assert.deepEqual(request.actions[4].recipientGroupIds, ["group-1"]);
});

test("trigger builder maps saved trigger details and form fields", () => {
  const detail = {
    id: "trigger-1",
    formId: "form-1",
    name: "Route HR submissions",
    description: null,
    eventName: "record.created",
    conditions: { mode: "all", conditions: [{ type: "field_changed", fieldId: "email" }] },
    actions: [{ id: "field-1", type: "update_field", fieldId: "email", value: "jane@example.test" }],
    isEnabled: true,
    concurrencyStamp: "stamp-1",
    createdAt: "2026-06-02T12:00:00.000Z",
    createdById: null,
    updatedAt: null,
    updatedById: null
  };
  const schema = {
    schemaVersion: 1,
    fields: [
      { id: "email", type: "email", label: "Email" },
      {
        id: "department",
        type: "select",
        label: "Department",
        options: [{ id: "opt-hr", label: "Human Resources", value: "HR" }]
      }
    ],
    layout: { pages: [] }
  };

  const draft = createTriggerDraftFromDetail(detail);
  const fields = getTriggerFieldOptions(schema);

  assert.equal(draft.id, "trigger-1");
  assert.equal(draft.concurrencyStamp, "stamp-1");
  assert.equal(draft.conditions[0].clientId, "condition-1");
  assert.equal(draft.actions[0].clientId, "field-1");
  assert.equal(draft.actions[0].fieldId, "email");
  assert.equal(draft.actions[0].value, "jane@example.test");
  assert.equal(fields[0].label, "Email");
  assert.equal(fields[1].options[0].value, "HR");
});

test("trigger builder validates update field action requirements", () => {
  const draft = createEmptyTriggerDraft("Employee form");

  const invalid = validateTriggerDraft({
    ...draft,
    actions: [{ clientId: "field-1", id: "field-1", type: "update_field", fieldId: "", value: "" }]
  });

  assert.equal(invalid.valid, false);
  assert.equal(invalid.errors.some((error) => error.path === "actions[0].fieldId"), true);
  assert.equal(invalid.errors.some((error) => error.path === "actions[0].value"), true);
});

test("trigger builder validates notification action requirements", () => {
  const draft = createEmptyTriggerDraft("Employee form");

  const invalid = validateTriggerDraft({
    ...draft,
    actions: [
      {
        clientId: "notify-1",
        id: "notify-1",
        type: "send_notification",
        title: "",
        body: "",
        recipientUserId: "",
        recipientGroupId: ""
      }
    ]
  });

  assert.equal(invalid.valid, false);
  assert.equal(invalid.errors.some((error) => error.path === "actions[0].title"), true);
  assert.equal(invalid.errors.some((error) => error.path === "actions[0].body"), true);
  assert.equal(invalid.errors.some((error) => error.path === "actions[0].recipients"), true);
});

test("trigger builder formats labels, statuses, and JSON details", () => {
  assert.equal(formatTriggerEventLabel("record.created"), "Record created");
  assert.equal(formatTriggerEventLabel("status.changed"), "Status changed");
  assert.equal(formatTriggerLogStatus("success").variant, "success");
  assert.equal(formatTriggerLogStatus("failed").label, "Failed");
  assert.equal(formatTriggerJson({ actions: [{ actionId: "audit-1" }] }), '{\n  "actions": [\n    {\n      "actionId": "audit-1"\n    }\n  ]\n}');
  assert.equal(formatTriggerJson(null), "No details");
});

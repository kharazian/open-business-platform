import assert from "node:assert/strict";
import { test } from "vitest";
import {
  buildTriggerRequest,
  createEmptyTriggerDraft,
  createTriggerActionDraft,
  createTriggerDraftFromDetail,
  formatTriggerEventLabel,
  formatTriggerJson,
  formatTriggerLogStatus,
  formatTriggerRetryState,
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
      {
        clientId: "action-2",
        id: "email-1",
        type: "send_email",
        toText: "hr@example.test, finance@example.test",
        subject: " New record ",
        body: " Please review. ",
        printTemplateId: "template-1"
      },
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
      },
      {
        clientId: "action-6",
        id: "create-1",
        type: "create_record",
        targetFormId: "form-2",
        valueMappingsText: '{ "email": { "sourceFieldId": "email" }, "department": { "literal": "HR" } }'
      },
      {
        clientId: "action-7",
        id: "webhook-1",
        type: "call_webhook",
        webhookUrl: " https://hooks.example.test/records ",
        webhookMethod: "post",
        webhookHeadersText: '{ "X-Source": "open-business-platform" }'
      },
      {
        clientId: "action-8",
        id: "workflow-1",
        type: "start_workflow",
        workflowDefinitionId: "workflow-1"
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
  assert.equal(request.actions[1].printTemplateId, "template-1");
  assert.equal(request.actions[2].assignedToUserId, "user-1");
  assert.equal("assignedGroupId" in request.actions[2], false);
  assert.equal(request.actions[3].fieldId, "email");
  assert.equal(request.actions[3].value, "jane@example.test");
  assert.equal(request.actions[4].title, "New HR record");
  assert.equal(request.actions[4].body, "Please review this record.");
  assert.deepEqual(request.actions[4].recipientUserIds, ["user-1"]);
  assert.deepEqual(request.actions[4].recipientGroupIds, ["group-1"]);
  assert.equal(request.actions[5].targetFormId, "form-2");
  assert.deepEqual(request.actions[5].values, {
    email: { sourceFieldId: "email" },
    department: { literal: "HR" }
  });
  assert.equal(request.actions[6].webhookUrl, "https://hooks.example.test/records");
  assert.equal(request.actions[6].webhookMethod, "POST");
  assert.deepEqual(request.actions[6].webhookHeaders, { "X-Source": "open-business-platform" });
  assert.equal(request.actions[7].workflowDefinitionId, "workflow-1");
  assert.deepEqual(request.retryPolicy, { isEnabled: true, maxAttempts: 3, delaySeconds: 60 });
  assert.equal(request.schedule, null);
});

test("trigger builder maps saved trigger details and form fields", () => {
  const detail = {
    id: "trigger-1",
    formId: "form-1",
    name: "Route HR submissions",
    description: null,
    eventName: "schedule.daily",
    conditions: { mode: "all", conditions: [{ type: "field_changed", fieldId: "email" }] },
    actions: [
      { id: "webhook-1", type: "call_webhook", webhookUrl: "https://hooks.example.test/records", webhookMethod: "POST" },
      { id: "email-1", type: "send_email", to: ["manager@example.test"], subject: "Employee record", body: "Attached.", printTemplateId: "template-1" }
    ],
    isEnabled: true,
    retryPolicy: { isEnabled: true, maxAttempts: 5, delaySeconds: 300 },
    schedule: { kind: "daily", timeZone: "Etc/UTC", startAt: "2026-06-04T12:00:00.000Z" },
    scheduleNextRunAt: "2026-06-05T12:00:00.000Z",
    scheduleLastRunAt: null,
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
  assert.equal(draft.eventName, "schedule.daily");
  assert.deepEqual(draft.retryPolicy, { isEnabled: true, maxAttempts: 5, delaySeconds: 300 });
  assert.equal(draft.schedule?.kind, "daily");
  assert.equal(draft.schedule?.startAt, "2026-06-04T12:00:00.000Z");
  assert.equal(draft.conditions[0].clientId, "condition-1");
  assert.equal(draft.actions[0].clientId, "webhook-1");
  assert.equal(draft.actions[0].webhookUrl, "https://hooks.example.test/records");
  assert.equal(draft.actions[0].webhookMethod, "POST");
  assert.equal(draft.actions[1].printTemplateId, "template-1");
  assert.equal(fields[0].label, "Email");
  assert.equal(fields[1].options[0].value, "HR");
});

test("trigger builder validates schedule, retry policy, and webhook action requirements", () => {
  const draft = createEmptyTriggerDraft("Employee form");

  const invalid = validateTriggerDraft({
    ...draft,
    eventName: "schedule.daily",
    retryPolicy: { isEnabled: true, maxAttempts: 25, delaySeconds: 10 },
    schedule: null,
    conditions: [{ clientId: "condition-1", type: "field_equals", fieldId: "email", value: "jane@example.test" }],
    actions: [{ clientId: "webhook-1", id: "webhook-1", type: "call_webhook", webhookUrl: "ftp://example.test/hook", webhookMethod: "TRACE" }]
  });

  assert.equal(invalid.valid, false);
  assert.equal(invalid.errors.some((error) => error.path === "schedule"), true);
  assert.equal(invalid.errors.some((error) => error.path === "conditions"), true);
  assert.equal(invalid.errors.some((error) => error.path === "retryPolicy.maxAttempts"), true);
  assert.equal(invalid.errors.some((error) => error.path === "retryPolicy.delaySeconds"), true);
  assert.equal(invalid.errors.some((error) => error.path === "actions[0].webhookUrl"), true);
  assert.equal(invalid.errors.some((error) => error.path === "actions[0].webhookMethod"), true);

  const request = buildTriggerRequest({
    ...draft,
    eventName: "schedule.daily",
    conditions: [],
    retryPolicy: { isEnabled: true, maxAttempts: 4, delaySeconds: 120 },
    schedule: { kind: "daily", timeZone: "Etc/UTC", startAt: "2026-06-04T12:00:00.000Z" },
    actions: [{ clientId: "webhook-1", id: "webhook-1", type: "call_webhook", webhookUrl: "https://hooks.example.test/daily", webhookMethod: "POST" }]
  });

  assert.deepEqual(request.retryPolicy, { isEnabled: true, maxAttempts: 4, delaySeconds: 120 });
  assert.deepEqual(request.schedule, { kind: "daily", timeZone: "Etc/UTC", startAt: "2026-06-04T12:00:00.000Z", interval: 1 });
  assert.equal(request.actions[0].type, "call_webhook");

  const weeklyRequest = buildTriggerRequest({
    ...draft,
    eventName: "schedule.weekly",
    conditions: [],
    schedule: { kind: "weekly", timeZone: "Etc/UTC", startAt: "2026-06-01T09:30:00.000Z", interval: 2 },
    actions: [{ clientId: "webhook-1", id: "webhook-1", type: "call_webhook", webhookUrl: "https://hooks.example.test/weekly", webhookMethod: "POST" }]
  });

  assert.deepEqual(weeklyRequest.schedule, { kind: "weekly", timeZone: "Etc/UTC", startAt: "2026-06-01T09:30:00.000Z", interval: 2, dayOfWeek: 1 });

  const invalidWeekly = validateTriggerDraft({
    ...draft,
    eventName: "schedule.weekly",
    conditions: [],
    schedule: { kind: "weekly", timeZone: "Etc/UTC", startAt: "2026-06-01T09:30:00.000Z", interval: 0, dayOfWeek: 7 },
    actions: [{ clientId: "webhook-1", id: "webhook-1", type: "call_webhook", webhookUrl: "https://hooks.example.test/weekly", webhookMethod: "POST" }]
  });

  assert.equal(invalidWeekly.valid, false);
  assert.equal(invalidWeekly.errors.some((error) => error.path === "schedule.interval"), true);
  assert.equal(invalidWeekly.errors.some((error) => error.path === "schedule.dayOfWeek"), true);
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

test("trigger builder validates create record action requirements", () => {
  const draft = createEmptyTriggerDraft("Employee form");

  const invalid = validateTriggerDraft({
    ...draft,
    actions: [{ clientId: "create-1", id: "create-1", type: "create_record", targetFormId: "", valueMappingsText: "" }]
  });

  assert.equal(invalid.valid, false);
  assert.equal(invalid.errors.some((error) => error.path === "actions[0].targetFormId"), true);
  assert.equal(invalid.errors.some((error) => error.path === "actions[0].values"), true);
});

test("trigger builder validates and serializes workflow start actions", () => {
  const draft = createEmptyTriggerDraft("Employee form");
  const action = createTriggerActionDraft("start_workflow", 42);

  assert.equal(action.type, "start_workflow");
  assert.equal(action.workflowDefinitionId, "");

  const invalid = validateTriggerDraft({
    ...draft,
    actions: [{ clientId: "workflow-1", id: "workflow-1", type: "start_workflow", workflowDefinitionId: "" }]
  });

  assert.equal(invalid.valid, false);
  assert.equal(invalid.errors.some((error) => error.path === "actions[0].workflowDefinitionId"), true);

  const request = buildTriggerRequest({
    ...draft,
    actions: [{ clientId: "workflow-1", id: "workflow-1", type: "start_workflow", workflowDefinitionId: "workflow-1" }]
  });

  assert.deepEqual(request.actions[0], {
    id: "workflow-1",
    type: "start_workflow",
    workflowDefinitionId: "workflow-1"
  });

  const mapped = createTriggerDraftFromDetail({
    id: "trigger-1",
    formId: "form-1",
    name: "Start workflow",
    description: null,
    eventName: "record.created",
    conditions: { mode: "all", conditions: [] },
    actions: [{ id: "workflow-1", type: "start_workflow", workflowDefinitionId: "workflow-1" }],
    isEnabled: true,
    retryPolicy: { isEnabled: true, maxAttempts: 3, delaySeconds: 60 },
    schedule: null,
    scheduleNextRunAt: null,
    scheduleLastRunAt: null,
    concurrencyStamp: "stamp-1",
    createdAt: "2026-06-05T12:00:00.000Z",
    createdById: null,
    updatedAt: null,
    updatedById: null
  });

  assert.equal(mapped.actions[0].workflowDefinitionId, "workflow-1");
});

test("trigger builder formats labels, statuses, and JSON details", () => {
  assert.equal(formatTriggerEventLabel("record.created"), "Record created");
  assert.equal(formatTriggerEventLabel("schedule.daily"), "Schedule daily");
  assert.equal(formatTriggerEventLabel("status.changed"), "Status changed");
  assert.equal(formatTriggerLogStatus("success").variant, "success");
  assert.equal(formatTriggerLogStatus("failed").label, "Failed");
  assert.equal(formatTriggerRetryState("pending", 1, 3), "Retry pending (1/3)");
  assert.equal(formatTriggerRetryState("exhausted", 3, 3), "Retries exhausted (3/3)");
  assert.equal(formatTriggerRetryState(null, 0, 0), "");
  assert.equal(formatTriggerJson({ actions: [{ actionId: "audit-1" }] }), '{\n  "actions": [\n    {\n      "actionId": "audit-1"\n    }\n  ]\n}');
  assert.equal(formatTriggerJson(null), "No details");
});

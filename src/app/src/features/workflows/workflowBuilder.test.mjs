import assert from "node:assert/strict";
import { test } from "vitest";
import {
  buildWorkflowRequest,
  createEmptyWorkflowDraft,
  createWorkflowDraftFromDetail,
  formatWorkflowStatus,
  validateWorkflowDraft
} from "./builder.ts";

test("workflow builder creates and validates normalized workflow requests", () => {
  const draft = createEmptyWorkflowDraft("Employee form");

  assert.equal(draft.name, "Employee form workflow");
  assert.equal(draft.isEnabled, true);

  const invalid = validateWorkflowDraft({ ...draft, name: "", configText: "{ nope" });

  assert.equal(invalid.valid, false);
  assert.equal(invalid.errors.some((error) => error.path === "name"), true);
  assert.equal(invalid.errors.some((error) => error.path === "config"), true);

  const request = buildWorkflowRequest({
    ...draft,
    name: " Employee approval ",
    description: " Manager review flow. ",
    configText: JSON.stringify(
      {
        schemaVersion: 1,
        initialStateKey: "draft",
        states: [
          { key: "draft", name: "Draft" },
          { key: "approved", name: "Approved", isFinal: true }
        ],
        transitions: [{ key: "approve", name: "Approve", fromStateKey: "draft", toStateKey: "approved" }],
        approvalSteps: []
      },
      null,
      2
    )
  });

  assert.equal(request.name, "Employee approval");
  assert.equal(request.description, "Manager review flow.");
  assert.equal(request.config.initialStateKey, "draft");
  assert.equal(request.config.states[1].isFinal, true);
  assert.equal(request.config.transitions[0].key, "approve");
});

test("workflow builder maps saved details and formats statuses", () => {
  const detail = {
    id: "workflow-1",
    formId: "form-1",
    name: "Employee approval",
    description: null,
    status: "published",
    isEnabled: true,
    hasUnpublishedChanges: true,
    currentVersionId: "version-1",
    currentVersionNumber: 2,
    config: {
      schemaVersion: 1,
      initialStateKey: "draft",
      states: [{ key: "draft", name: "Draft" }, { key: "approved", name: "Approved", isFinal: true }],
      transitions: [{ key: "approve", name: "Approve", fromStateKey: "draft", toStateKey: "approved" }],
      approvalSteps: []
    },
    concurrencyStamp: "stamp-1",
    createdAt: "2026-06-04T12:00:00.000Z",
    createdById: null,
    updatedAt: null,
    updatedById: null
  };

  const draft = createWorkflowDraftFromDetail(detail);
  const status = formatWorkflowStatus(detail);

  assert.equal(draft.id, "workflow-1");
  assert.equal(draft.concurrencyStamp, "stamp-1");
  assert.ok(draft.configText.includes("\"initialStateKey\": \"draft\""));
  assert.equal(status.label, "Published changes");
  assert.equal(status.tone, "warning");
});


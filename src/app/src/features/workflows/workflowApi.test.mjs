import assert from "node:assert/strict";
import { test } from "vitest";
import {
  createWorkflow,
  disableWorkflow,
  enableWorkflow,
  executeRecordWorkflowTransition,
  getRecordWorkflow,
  listWorkflows,
  publishWorkflow,
  startRecordWorkflow,
  updateWorkflow,
  WorkflowsApiError
} from "./api.ts";

function createFetcher(responseBody = {}) {
  const calls = [];
  const fetcher = async (input, init) => {
    calls.push({ input, init });
    return {
      ok: true,
      json: async () => responseBody
    };
  };

  return { calls, fetcher };
}

test("workflow API helpers call backend workflow endpoints", async () => {
  const request = {
    name: "Employee approval",
    description: null,
    config: { schemaVersion: 1, initialStateKey: "draft", states: [], transitions: [], approvalSteps: [] },
    isEnabled: true
  };
  const response = { id: "workflow-1", ...request };
  const { calls, fetcher } = createFetcher({ items: [response] });

  const items = await listWorkflows("form 1", fetcher);

  assert.equal(items.length, 1);
  assert.equal(calls[0].input, "/api/forms/form%201/workflows");
  assert.equal(calls[0].init.method, "GET");

  const create = createFetcher(response);
  await createWorkflow("form 1", request, create.fetcher);
  assert.equal(create.calls[0].input, "/api/forms/form%201/workflows");
  assert.equal(create.calls[0].init.method, "POST");
  assert.equal(JSON.parse(create.calls[0].init.body).name, "Employee approval");

  const update = createFetcher(response);
  await updateWorkflow("workflow 1", { ...request, concurrencyStamp: "stamp" }, update.fetcher);
  assert.equal(update.calls[0].input, "/api/workflows/workflow%201");
  assert.equal(update.calls[0].init.method, "PUT");

  const publish = createFetcher(response);
  await publishWorkflow("workflow 1", "stamp", publish.fetcher);
  assert.equal(publish.calls[0].input, "/api/workflows/workflow%201/publish");
  assert.equal(JSON.parse(publish.calls[0].init.body).concurrencyStamp, "stamp");

  const enable = createFetcher(response);
  await enableWorkflow("workflow 1", "stamp", enable.fetcher);
  assert.equal(enable.calls[0].input, "/api/workflows/workflow%201/enable");

  const disable = createFetcher(response);
  await disableWorkflow("workflow 1", "stamp", disable.fetcher);
  assert.equal(disable.calls[0].input, "/api/workflows/workflow%201/disable");
});

test("record workflow API helpers call record workflow endpoints", async () => {
  const response = {
    recordId: "record-1",
    formId: "form-1",
    workflowDefinitionId: "workflow-1",
    workflowDefinitionVersionId: "version-1",
    workflowName: "Employee approval",
    workflowVersionNumber: 1,
    stateKey: "draft",
    availableWorkflows: [],
    availableTransitions: [],
    history: [],
    recordConcurrencyStamp: "record-stamp"
  };

  const state = createFetcher(response);
  await getRecordWorkflow("record 1", state.fetcher);
  assert.equal(state.calls[0].input, "/api/records/record%201/workflow");
  assert.equal(state.calls[0].init.method, "GET");

  const start = createFetcher(response);
  await startRecordWorkflow("record 1", { workflowDefinitionId: "workflow 1", concurrencyStamp: "record-stamp" }, start.fetcher);
  assert.equal(start.calls[0].input, "/api/records/record%201/workflow/start");
  assert.equal(start.calls[0].init.method, "POST");
  assert.equal(JSON.parse(start.calls[0].init.body).workflowDefinitionId, "workflow 1");

  const transition = createFetcher({ ...response, stateKey: "manager_review" });
  await executeRecordWorkflowTransition("record 1", "submit for review", { concurrencyStamp: "record-stamp-2" }, transition.fetcher);
  assert.equal(transition.calls[0].input, "/api/records/record%201/workflow/transitions/submit%20for%20review");
  assert.equal(transition.calls[0].init.method, "POST");
  assert.equal(JSON.parse(transition.calls[0].init.body).concurrencyStamp, "record-stamp-2");
});

test("workflow API helpers surface validation errors", async () => {
  const fetcher = async () => ({
    ok: false,
    json: async () => ({ message: "Workflow definition is invalid.", errors: [{ path: "name", code: "required", message: "Name required" }] })
  });

  await assert.rejects(
    () => listWorkflows("form-1", fetcher),
    (error) => {
      assert.equal(error instanceof WorkflowsApiError, true);
      assert.equal(error.message, "Workflow definition is invalid.");
      assert.equal(error.errors[0].path, "name");
      return true;
    }
  );
});

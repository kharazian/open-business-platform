import assert from "node:assert/strict";
import { test } from "vitest";
import {
  createWorkflow,
  disableWorkflow,
  enableWorkflow,
  listWorkflows,
  publishWorkflow,
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

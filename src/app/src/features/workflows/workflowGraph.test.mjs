import assert from "node:assert/strict";
import { test } from "vitest";
import {
  buildWorkflowGraph,
  createWorkflowApprovalStep,
  createWorkflowState,
  createWorkflowTransition,
  removeWorkflowState,
  upsertWorkflowState
} from "./graph.ts";
import { defaultWorkflowConfig } from "./builder.ts";

test("workflow graph marks initial and final states and connects transitions", () => {
  const graph = buildWorkflowGraph(defaultWorkflowConfig, []);

  assert.equal(graph.nodes.length, 3);
  assert.equal(graph.edges.length, 2);
  assert.equal(graph.nodes.find((node) => node.id === "state:draft")?.data.isInitial, true);
  assert.equal(graph.nodes.find((node) => node.id === "state:approved")?.data.isFinal, true);
  assert.deepEqual(
    graph.edges.map((edge) => [edge.id, edge.source, edge.target]),
    [
      ["transition:submit", "state:draft", "state:submitted"],
      ["transition:approve", "state:submitted", "state:approved"]
    ]
  );
});

test("workflow graph maps validation errors to states and transitions", () => {
  const graph = buildWorkflowGraph(defaultWorkflowConfig, [
    { path: "config.states[1].name", code: "workflow.state_name_required", message: "State name required." },
    { path: "config.transitions[0].toStateKey", code: "workflow.transition_to_missing", message: "Missing target." }
  ]);

  assert.deepEqual(graph.nodes.find((node) => node.id === "state:submitted")?.data.validationErrors, ["State name required."]);
  assert.deepEqual(graph.edges.find((edge) => edge.id === "transition:submit")?.data.validationErrors, ["Missing target."]);
});

test("workflow graph edit helpers update config semantics without layout data", () => {
  const renamed = upsertWorkflowState(defaultWorkflowConfig, { key: "request", name: "Request" }, "draft");

  assert.equal(renamed.initialStateKey, "request");
  assert.equal(renamed.transitions[0].fromStateKey, "request");
  assert.equal(renamed.states.some((state) => state.key === "draft"), false);

  const removed = removeWorkflowState(renamed, "submitted");

  assert.equal(removed.transitions.length, 0);
  assert.equal(removed.initialStateKey, "request");
  assert.equal(removed.states.length, 2);

  const approvalStep = createWorkflowApprovalStep(removed, "manager_review");

  assert.equal(approvalStep.key, "manager_review");
  assert.deepEqual(approvalStep.assigneeRules, [{ type: "record_owner" }]);

  const state = createWorkflowState(removed);
  const transition = createWorkflowTransition(removed);

  assert.equal(state.key, "state_3");
  assert.equal(transition.fromStateKey, "request");
});

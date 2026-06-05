import type {
  WorkflowApprovalStepDefinition,
  WorkflowDefinitionConfig,
  WorkflowStateDefinition,
  WorkflowTransitionDefinition,
  WorkflowValidationError
} from "./types";

export type WorkflowGraphNodeData = {
  stateKey: string;
  stateName: string;
  isInitial: boolean;
  isFinal: boolean;
  validationErrors: string[];
};

export type WorkflowGraphEdgeData = {
  transitionKey: string;
  transitionName: string;
  approvalStepKey?: string | null;
  actionCount: number;
  validationErrors: string[];
};

export type WorkflowGraphNode = {
  id: string;
  type: "workflowState";
  position: { x: number; y: number };
  data: WorkflowGraphNodeData;
};

export type WorkflowGraphEdge = {
  id: string;
  type: "smoothstep";
  source: string;
  target: string;
  label: string;
  data: WorkflowGraphEdgeData;
};

export type WorkflowGraph = {
  nodes: WorkflowGraphNode[];
  edges: WorkflowGraphEdge[];
};

type ValidationIndex = {
  stateErrors: Map<string, string[]>;
  transitionErrors: Map<string, string[]>;
};

const graphColumnWidth = 260;
const graphRowHeight = 170;

export function buildWorkflowGraph(config: WorkflowDefinitionConfig, errors: WorkflowValidationError[]): WorkflowGraph {
  const validationIndex = buildValidationIndex(config, errors);

  return {
    nodes: config.states.map((state, index) => ({
      id: getStateNodeId(state.key),
      type: "workflowState",
      position: getStatePosition(index, state),
      data: {
        stateKey: state.key,
        stateName: state.name,
        isInitial: state.key === config.initialStateKey,
        isFinal: Boolean(state.isFinal),
        validationErrors: validationIndex.stateErrors.get(state.key) ?? []
      }
    })),
    edges: config.transitions.map((transition) => ({
      id: getTransitionEdgeId(transition.key),
      type: "smoothstep",
      source: getStateNodeId(transition.fromStateKey),
      target: getStateNodeId(transition.toStateKey),
      label: transition.name || transition.key,
      data: {
        transitionKey: transition.key,
        transitionName: transition.name,
        approvalStepKey: transition.approvalStepKey ?? null,
        actionCount: transition.actions?.length ?? 0,
        validationErrors: validationIndex.transitionErrors.get(transition.key) ?? []
      }
    }))
  };
}

export function getStateNodeId(stateKey: string): string {
  return `state:${stateKey}`;
}

export function getTransitionEdgeId(transitionKey: string): string {
  return `transition:${transitionKey}`;
}

export function createWorkflowState(config: WorkflowDefinitionConfig): WorkflowStateDefinition {
  const nextNumber = config.states.length + 1;
  const key = createUniqueKey(`state_${nextNumber}`, config.states.map((state) => state.key));

  return {
    key,
    name: sentenceCase(key)
  };
}

export function createWorkflowTransition(config: WorkflowDefinitionConfig): WorkflowTransitionDefinition {
  const nextNumber = config.transitions.length + 1;
  const key = createUniqueKey(`transition_${nextNumber}`, config.transitions.map((transition) => transition.key));
  const sourceState = config.states.find((state) => state.key === config.initialStateKey && !state.isFinal)
    ?? config.states.find((state) => !state.isFinal)
    ?? config.states[0];
  const targetState = config.states.find((state) => state.key !== sourceState?.key && state.isFinal)
    ?? config.states.find((state) => state.key !== sourceState?.key)
    ?? sourceState;

  return {
    key,
    name: sentenceCase(key),
    fromStateKey: sourceState?.key ?? "",
    toStateKey: targetState?.key ?? ""
  };
}

export function createWorkflowApprovalStep(config: WorkflowDefinitionConfig, requestedKey?: string): WorkflowApprovalStepDefinition {
  const nextNumber = config.approvalSteps.length + 1;
  const baseKey = normalizeKey(requestedKey) || `approval_${nextNumber}`;
  const key = createUniqueKey(baseKey, config.approvalSteps.map((step) => step.key));

  return {
    key,
    name: sentenceCase(key),
    mode: "any",
    assigneeRules: [{ type: "record_owner" }]
  };
}

export function upsertWorkflowState(
  config: WorkflowDefinitionConfig,
  state: WorkflowStateDefinition,
  previousKey = state.key
): WorkflowDefinitionConfig {
  const normalized = normalizeState(state);
  const stateExists = config.states.some((existing) => existing.key === previousKey);
  const states = stateExists
    ? config.states.map((existing) => (existing.key === previousKey ? normalized : existing))
    : [...config.states, normalized];
  const initialStateKey = config.initialStateKey === previousKey || !config.initialStateKey
    ? normalized.key
    : config.initialStateKey;
  const transitions = config.transitions.map((transition) => ({
    ...transition,
    fromStateKey: transition.fromStateKey === previousKey ? normalized.key : transition.fromStateKey,
    toStateKey: transition.toStateKey === previousKey ? normalized.key : transition.toStateKey
  }));

  return {
    ...config,
    initialStateKey,
    states,
    transitions
  };
}

export function removeWorkflowState(config: WorkflowDefinitionConfig, stateKey: string): WorkflowDefinitionConfig {
  const states = config.states.filter((state) => state.key !== stateKey);
  const stateKeys = new Set(states.map((state) => state.key));
  const transitions = config.transitions.filter((transition) => stateKeys.has(transition.fromStateKey) && stateKeys.has(transition.toStateKey));
  const initialStateKey = stateKeys.has(config.initialStateKey)
    ? config.initialStateKey
    : states.find((state) => !state.isFinal)?.key ?? states[0]?.key ?? "";

  return {
    ...config,
    initialStateKey,
    states,
    transitions
  };
}

export function upsertWorkflowTransition(
  config: WorkflowDefinitionConfig,
  transition: WorkflowTransitionDefinition,
  previousKey = transition.key
): WorkflowDefinitionConfig {
  const normalized = normalizeTransition(transition);
  const transitionExists = config.transitions.some((existing) => existing.key === previousKey);
  const transitions = transitionExists
    ? config.transitions.map((existing) => (existing.key === previousKey ? normalized : existing))
    : [...config.transitions, normalized];

  return {
    ...config,
    transitions
  };
}

export function removeWorkflowTransition(config: WorkflowDefinitionConfig, transitionKey: string): WorkflowDefinitionConfig {
  return {
    ...config,
    transitions: config.transitions.filter((transition) => transition.key !== transitionKey)
  };
}

export function upsertWorkflowApprovalStep(
  config: WorkflowDefinitionConfig,
  approvalStep: WorkflowApprovalStepDefinition,
  previousKey = approvalStep.key
): WorkflowDefinitionConfig {
  const normalized = normalizeApprovalStep(approvalStep);
  const stepExists = config.approvalSteps.some((existing) => existing.key === previousKey);
  const approvalSteps = stepExists
    ? config.approvalSteps.map((existing) => (existing.key === previousKey ? normalized : existing))
    : [...config.approvalSteps, normalized];
  const transitions = config.transitions.map((transition) => ({
    ...transition,
    approvalStepKey: transition.approvalStepKey === previousKey ? normalized.key : transition.approvalStepKey
  }));

  return {
    ...config,
    approvalSteps,
    transitions
  };
}

function buildValidationIndex(config: WorkflowDefinitionConfig, errors: WorkflowValidationError[]): ValidationIndex {
  const stateErrors = new Map<string, string[]>();
  const transitionErrors = new Map<string, string[]>();

  for (const error of errors) {
    const stateIndex = getPathIndex(error.path, "states");
    const transitionIndex = getPathIndex(error.path, "transitions");

    if (stateIndex !== null) {
      addError(stateErrors, config.states[stateIndex]?.key, error.message);
      continue;
    }

    if (transitionIndex !== null) {
      addError(transitionErrors, config.transitions[transitionIndex]?.key, error.message);
      continue;
    }

    if (error.path === "config.initialStateKey") {
      addError(stateErrors, config.initialStateKey, error.message);
    }
  }

  return { stateErrors, transitionErrors };
}

function getStatePosition(index: number, state: WorkflowStateDefinition): { x: number; y: number } {
  return {
    x: (index % 3) * graphColumnWidth,
    y: Math.floor(index / 3) * graphRowHeight + (state.isFinal ? 70 : 0)
  };
}

function getPathIndex(path: string, collectionName: "states" | "transitions"): number | null {
  const match = new RegExp(`^config\\.${collectionName}\\[(\\d+)\\]`).exec(path);
  return match ? Number(match[1]) : null;
}

function addError(map: Map<string, string[]>, key: string | undefined, message: string) {
  if (!key) return;

  map.set(key, [...(map.get(key) ?? []), message]);
}

function normalizeState(state: WorkflowStateDefinition): WorkflowStateDefinition {
  return {
    key: normalizeKey(state.key),
    name: normalizeName(state.name),
    isFinal: state.isFinal ? true : undefined
  };
}

function normalizeTransition(transition: WorkflowTransitionDefinition): WorkflowTransitionDefinition {
  const actions = transition.actions?.length ? transition.actions : undefined;

  return {
    key: normalizeKey(transition.key),
    name: normalizeName(transition.name),
    fromStateKey: normalizeKey(transition.fromStateKey),
    toStateKey: normalizeKey(transition.toStateKey),
    approvalStepKey: normalizeOptionalKey(transition.approvalStepKey),
    actions
  };
}

function normalizeApprovalStep(approvalStep: WorkflowApprovalStepDefinition): WorkflowApprovalStepDefinition {
  return {
    key: normalizeKey(approvalStep.key),
    name: normalizeName(approvalStep.name),
    mode: approvalStep.mode,
    assigneeRules: approvalStep.assigneeRules.length > 0 ? approvalStep.assigneeRules : [{ type: "record_owner" }]
  };
}

function createUniqueKey(baseKey: string, existingKeys: string[]): string {
  const normalizedBase = normalizeKey(baseKey) || "item";
  const existing = new Set(existingKeys);

  if (!existing.has(normalizedBase)) {
    return normalizedBase;
  }

  for (let suffix = 2; ; suffix += 1) {
    const candidate = `${normalizedBase}_${suffix}`;

    if (!existing.has(candidate)) {
      return candidate;
    }
  }
}

function normalizeKey(value?: string | null): string {
  return (value ?? "")
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9_]+/g, "_")
    .replace(/^_+|_+$/g, "");
}

function normalizeOptionalKey(value?: string | null): string | null {
  const normalized = normalizeKey(value);
  return normalized ? normalized : null;
}

function normalizeName(value: string): string {
  return value.trim();
}

function sentenceCase(value: string): string {
  const sentence = value.replace(/[_-]+/g, " ").trim();
  return sentence.replace(/\b\w/g, (letter) => letter.toUpperCase());
}

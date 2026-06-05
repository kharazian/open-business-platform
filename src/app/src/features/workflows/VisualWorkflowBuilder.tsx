import { useEffect, useMemo, useState, type ReactNode } from "react";
import {
  Background,
  Controls,
  MarkerType,
  MiniMap,
  ReactFlow,
  ReactFlowProvider,
  type Edge,
  type EdgeMouseHandler,
  type Node,
  type NodeMouseHandler
} from "@xyflow/react";
import "@xyflow/react/dist/style.css";
import { Plus, Trash2 } from "lucide-react";
import { Badge } from "../../components/ui/Badge";
import { Button } from "../../components/ui/Button";
import { Checkbox } from "../../components/ui/Checkbox";
import { Input } from "../../components/ui/Input";
import { Select } from "../../components/ui/Select";
import { Textarea } from "../../components/ui/Textarea";
import { cn } from "../../lib/cn";
import {
  createWorkflowAction,
  workflowActionOptions
} from "./builder";
import {
  buildWorkflowGraph,
  createWorkflowApprovalStep,
  createWorkflowState,
  createWorkflowTransition,
  getStateNodeId,
  getTransitionEdgeId,
  removeWorkflowState,
  removeWorkflowTransition,
  upsertWorkflowApprovalStep,
  upsertWorkflowState,
  upsertWorkflowTransition,
  type WorkflowGraphEdgeData,
  type WorkflowGraphNodeData
} from "./graph";
import type {
  WorkflowActionDefinition,
  WorkflowActionType,
  WorkflowApprovalStepDefinition,
  WorkflowDefinitionConfig,
  WorkflowStateDefinition,
  WorkflowTransitionDefinition,
  WorkflowValidationError
} from "./types";

type VisualWorkflowBuilderProps = {
  config: WorkflowDefinitionConfig;
  validationErrors: WorkflowValidationError[];
  onChange: (config: WorkflowDefinitionConfig) => void;
};

type WorkflowFlowNodeData = Record<string, unknown> & WorkflowGraphNodeData & {
  label: ReactNode;
};

type WorkflowFlowEdgeData = Record<string, unknown> & WorkflowGraphEdgeData;
type WorkflowFlowNode = Node<WorkflowFlowNodeData, "default">;
type WorkflowFlowEdge = Edge<WorkflowFlowEdgeData, "smoothstep">;

export function VisualWorkflowBuilder({ config, validationErrors, onChange }: VisualWorkflowBuilderProps) {
  const [selectedStateKey, setSelectedStateKey] = useState<string | null>(() => config.initialStateKey || config.states[0]?.key || null);
  const [selectedTransitionKey, setSelectedTransitionKey] = useState<string | null>(null);
  const isCompactGraph = useCompactWorkflowGraph();
  const graph = useMemo(() => buildWorkflowGraph(config, validationErrors), [config, validationErrors]);
  const selectedStateIndex = config.states.findIndex((state) => state.key === selectedStateKey);
  const selectedTransitionIndex = config.transitions.findIndex((transition) => transition.key === selectedTransitionKey);
  const selectedState = selectedStateIndex >= 0 ? config.states[selectedStateIndex] : null;
  const selectedTransition = selectedTransitionIndex >= 0 ? config.transitions[selectedTransitionIndex] : null;
  const selectedApprovalStep = selectedTransition?.approvalStepKey
    ? config.approvalSteps.find((step) => step.key === selectedTransition.approvalStepKey) ?? null
    : null;
  const selectedStateErrors = selectedStateKey
    ? graph.nodes.find((node) => node.id === getStateNodeId(selectedStateKey))?.data.validationErrors ?? []
    : [];
  const selectedTransitionErrors = selectedTransitionKey
    ? graph.edges.find((edge) => edge.id === getTransitionEdgeId(selectedTransitionKey))?.data.validationErrors ?? []
    : [];

  useEffect(() => {
    if (selectedStateKey && !config.states.some((state) => state.key === selectedStateKey)) {
      setSelectedStateKey(config.states[0]?.key ?? null);
    }

    if (selectedTransitionKey && !config.transitions.some((transition) => transition.key === selectedTransitionKey)) {
      setSelectedTransitionKey(null);
    }
  }, [config.states, config.transitions, selectedStateKey, selectedTransitionKey]);

  const nodes = useMemo<WorkflowFlowNode[]>(
    () =>
      graph.nodes.map((node, index) => ({
        id: node.id,
        type: "default",
        position: isCompactGraph ? { x: 0, y: index * 125 } : node.position,
        selected: selectedStateKey === node.data.stateKey,
        draggable: false,
        data: {
          ...node.data,
          label: <StateNodeLabel data={node.data} />
        },
        className: cn(
          "!rounded-lg !border-2 !bg-card !px-1 !py-1 !text-left !shadow-soft",
          node.data.validationErrors.length > 0
            ? "!border-danger"
            : selectedStateKey === node.data.stateKey
              ? "!border-primary"
              : "!border-border"
        )
      })),
    [graph.nodes, isCompactGraph, selectedStateKey]
  );

  const edges = useMemo<WorkflowFlowEdge[]>(
    () =>
      graph.edges.map((edge) => ({
        id: edge.id,
        type: "smoothstep",
        source: edge.source,
        target: edge.target,
        label: <TransitionEdgeLabel data={edge.data} />,
        selected: selectedTransitionKey === edge.data.transitionKey,
        markerEnd: { type: MarkerType.ArrowClosed },
        data: edge.data,
        style: {
          strokeWidth: selectedTransitionKey === edge.data.transitionKey ? 3 : 2,
          stroke: edge.data.validationErrors.length > 0 ? "var(--color-danger)" : "var(--color-primary)"
        },
        labelBgStyle: {
          fill: "var(--color-card)",
          fillOpacity: 0.96
        },
        labelStyle: {
          fill: "var(--color-foreground)",
          fontWeight: 700
        }
      })),
    [graph.edges, selectedTransitionKey]
  );

  const handleNodeClick: NodeMouseHandler<WorkflowFlowNode> = (_event, node) => {
    setSelectedStateKey(node.data.stateKey);
    setSelectedTransitionKey(null);
  };

  const handleEdgeClick: EdgeMouseHandler<WorkflowFlowEdge> = (_event, edge) => {
    setSelectedTransitionKey(edge.data?.transitionKey ?? null);
    setSelectedStateKey(null);
  };

  function handleAddState() {
    const state = createWorkflowState(config);
    onChange(upsertWorkflowState(config, state));
    setSelectedStateKey(state.key);
    setSelectedTransitionKey(null);
  }

  function handleDeleteState() {
    if (!selectedState) return;

    const nextConfig = removeWorkflowState(config, selectedState.key);
    onChange(nextConfig);
    setSelectedStateKey(nextConfig.states[0]?.key ?? null);
    setSelectedTransitionKey(null);
  }

  function updateSelectedState(patch: Partial<WorkflowStateDefinition>) {
    if (!selectedState) return;

    const nextConfig = upsertWorkflowState(config, { ...selectedState, ...patch }, selectedState.key);
    onChange(nextConfig);
    setSelectedStateKey(nextConfig.states[selectedStateIndex]?.key ?? selectedState.key);
    setSelectedTransitionKey(null);
  }

  function setSelectedStateAsInitial() {
    if (!selectedState) return;

    onChange({ ...config, initialStateKey: selectedState.key });
  }

  function handleAddTransition() {
    const transition = createWorkflowTransition(config);
    onChange(upsertWorkflowTransition(config, transition));
    setSelectedTransitionKey(transition.key);
    setSelectedStateKey(null);
  }

  function handleDeleteTransition() {
    if (!selectedTransition) return;

    const nextConfig = removeWorkflowTransition(config, selectedTransition.key);
    onChange(nextConfig);
    setSelectedTransitionKey(nextConfig.transitions[0]?.key ?? null);
    setSelectedStateKey(null);
  }

  function updateSelectedTransition(patch: Partial<WorkflowTransitionDefinition>) {
    if (!selectedTransition) return;

    const nextConfig = upsertWorkflowTransition(config, { ...selectedTransition, ...patch }, selectedTransition.key);
    onChange(nextConfig);
    setSelectedTransitionKey(nextConfig.transitions[selectedTransitionIndex]?.key ?? selectedTransition.key);
    setSelectedStateKey(null);
  }

  function handleCreateApprovalStep() {
    if (!selectedTransition) return;

    const approvalStep = createWorkflowApprovalStep(config, `${selectedTransition.key}_approval`);
    const configWithStep = upsertWorkflowApprovalStep(config, approvalStep);
    const nextConfig = upsertWorkflowTransition(
      configWithStep,
      { ...selectedTransition, approvalStepKey: approvalStep.key },
      selectedTransition.key
    );

    onChange(nextConfig);
    setSelectedTransitionKey(selectedTransition.key);
  }

  function updateSelectedApprovalStep(patch: Partial<WorkflowApprovalStepDefinition>) {
    if (!selectedApprovalStep) return;

    onChange(upsertWorkflowApprovalStep(config, { ...selectedApprovalStep, ...patch }, selectedApprovalStep.key));
  }

  function handleAddAction() {
    if (!selectedTransition) return;

    const actions = selectedTransition.actions ?? [];
    updateSelectedTransition({
      actions: [...actions, createWorkflowAction("write_audit_entry", getNextActionIndex(actions))]
    });
  }

  function updateTransitionAction(index: number, patch: Partial<WorkflowActionDefinition>) {
    if (!selectedTransition) return;

    const actions = [...(selectedTransition.actions ?? [])];
    const existing = actions[index];

    if (!existing) return;

    actions[index] = { ...existing, ...patch };
    updateSelectedTransition({ actions });
  }

  function updateTransitionActionType(index: number, type: WorkflowActionType) {
    if (!selectedTransition) return;

    const action = selectedTransition.actions?.[index];

    if (!action) return;

    updateTransitionAction(index, { ...createWorkflowAction(type, index + 1), id: action.id });
  }

  function removeTransitionAction(index: number) {
    if (!selectedTransition) return;

    updateSelectedTransition({
      actions: (selectedTransition.actions ?? []).filter((_action, actionIndex) => actionIndex !== index)
    });
  }

  return (
    <div className="grid gap-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex flex-wrap gap-2">
          <Button onClick={handleAddState} size="sm" variant="outline">
            <Plus className="size-4" />
            State
          </Button>
          <Button disabled={config.states.length === 0} onClick={handleAddTransition} size="sm" variant="outline">
            <Plus className="size-4" />
            Transition
          </Button>
        </div>
        <div className="flex flex-wrap gap-2">
          <Badge>{config.states.length} states</Badge>
          <Badge>{config.transitions.length} transitions</Badge>
          <Badge>{config.approvalSteps.length} approvals</Badge>
        </div>
      </div>

      <div className="grid gap-4 2xl:grid-cols-[minmax(0,1fr)_24rem]">
        <div className="h-[34rem] min-h-[28rem] overflow-hidden rounded-lg border border-border bg-card/80">
          <ReactFlowProvider>
            <ReactFlow<WorkflowFlowNode, WorkflowFlowEdge>
              colorMode="system"
              deleteKeyCode={null}
              edges={edges}
              fitView
              fitViewOptions={{ padding: isCompactGraph ? 0.14 : 0.22 }}
              key={`${isCompactGraph ? "compact" : "wide"}-${nodes.length}-${edges.length}`}
              maxZoom={1.25}
              minZoom={0.35}
              nodes={nodes}
              nodesConnectable={false}
              nodesDraggable={false}
              onEdgeClick={handleEdgeClick}
              onNodeClick={handleNodeClick}
              onPaneClick={() => {
                setSelectedTransitionKey(null);
                setSelectedStateKey(config.initialStateKey || config.states[0]?.key || null);
              }}
              panOnScroll
              proOptions={{ hideAttribution: true }}
            >
              <Background color="var(--color-border)" gap={18} />
              {isCompactGraph ? null : (
                <MiniMap
                  maskColor="color-mix(in oklch, var(--color-card) 70%, transparent)"
                  nodeColor={(node) => (hasValidationErrors(node) ? "var(--color-danger)" : "var(--color-primary)")}
                  pannable
                  zoomable
                />
              )}
              <Controls />
            </ReactFlow>
          </ReactFlowProvider>
        </div>

        <div className="grid content-start gap-4 rounded-lg border border-border bg-card/70 p-4">
          {selectedTransition ? (
            <TransitionPanel
              approvalStep={selectedApprovalStep}
              config={config}
              errors={selectedTransitionErrors}
              onAddAction={handleAddAction}
              onCreateApprovalStep={handleCreateApprovalStep}
              onDelete={handleDeleteTransition}
              onRemoveAction={removeTransitionAction}
              onUpdate={updateSelectedTransition}
              onUpdateAction={updateTransitionAction}
              onUpdateActionType={updateTransitionActionType}
              onUpdateApprovalStep={updateSelectedApprovalStep}
              transition={selectedTransition}
            />
          ) : selectedState ? (
            <StatePanel
              config={config}
              errors={selectedStateErrors}
              onDelete={handleDeleteState}
              onSetInitial={setSelectedStateAsInitial}
              onUpdate={updateSelectedState}
              state={selectedState}
            />
          ) : (
            <div className="rounded-lg border border-dashed border-border bg-muted/40 p-4 text-sm font-semibold text-muted-foreground">
              No workflow state selected.
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

function StatePanel({
  config,
  errors,
  onDelete,
  onSetInitial,
  onUpdate,
  state
}: {
  config: WorkflowDefinitionConfig;
  errors: string[];
  onDelete: () => void;
  onSetInitial: () => void;
  onUpdate: (patch: Partial<WorkflowStateDefinition>) => void;
  state: WorkflowStateDefinition;
}) {
  return (
    <>
      <PanelHeader title="State" onDelete={onDelete} deleteDisabled={config.states.length <= 1} />
      <Input label="Key" onChange={(event) => onUpdate({ key: event.target.value })} value={state.key} />
      <Input label="Name" onChange={(event) => onUpdate({ name: event.target.value })} value={state.name} />
      <Checkbox
        checked={state.key === config.initialStateKey}
        label="Initial"
        onChange={(event) => {
          if (event.target.checked) {
            onSetInitial();
          }
        }}
      />
      <Checkbox checked={Boolean(state.isFinal)} label="Final" onChange={(event) => onUpdate({ isFinal: event.target.checked })} />
      {state.key === config.initialStateKey ? null : (
        <Button onClick={onSetInitial} size="sm" variant="outline">
          Set initial
        </Button>
      )}
      <PanelErrors errors={errors} />
    </>
  );
}

function TransitionPanel({
  approvalStep,
  config,
  errors,
  onAddAction,
  onCreateApprovalStep,
  onDelete,
  onRemoveAction,
  onUpdate,
  onUpdateAction,
  onUpdateActionType,
  onUpdateApprovalStep,
  transition
}: {
  approvalStep: WorkflowApprovalStepDefinition | null;
  config: WorkflowDefinitionConfig;
  errors: string[];
  onAddAction: () => void;
  onCreateApprovalStep: () => void;
  onDelete: () => void;
  onRemoveAction: (index: number) => void;
  onUpdate: (patch: Partial<WorkflowTransitionDefinition>) => void;
  onUpdateAction: (index: number, patch: Partial<WorkflowActionDefinition>) => void;
  onUpdateActionType: (index: number, type: WorkflowActionType) => void;
  onUpdateApprovalStep: (patch: Partial<WorkflowApprovalStepDefinition>) => void;
  transition: WorkflowTransitionDefinition;
}) {
  return (
    <>
      <PanelHeader title="Transition" onDelete={onDelete} />
      <Input label="Key" onChange={(event) => onUpdate({ key: event.target.value })} value={transition.key} />
      <Input label="Name" onChange={(event) => onUpdate({ name: event.target.value })} value={transition.name} />
      <div className="grid gap-3 sm:grid-cols-2 2xl:grid-cols-1">
        <Select label="From" onChange={(event) => onUpdate({ fromStateKey: event.target.value })} value={transition.fromStateKey}>
          {config.states.map((state) => (
            <option key={state.key} value={state.key}>
              {state.name || state.key}
            </option>
          ))}
        </Select>
        <Select label="To" onChange={(event) => onUpdate({ toStateKey: event.target.value })} value={transition.toStateKey}>
          {config.states.map((state) => (
            <option key={state.key} value={state.key}>
              {state.name || state.key}
            </option>
          ))}
        </Select>
      </div>
      <div className="grid gap-3">
        <Select
          label="Approval"
          onChange={(event) => onUpdate({ approvalStepKey: event.target.value || null })}
          value={transition.approvalStepKey ?? ""}
        >
          <option value="">None</option>
          {config.approvalSteps.map((step) => (
            <option key={step.key} value={step.key}>
              {step.name || step.key}
            </option>
          ))}
        </Select>
        <Button onClick={onCreateApprovalStep} size="sm" variant="outline">
          <Plus className="size-4" />
          Approval
        </Button>
      </div>
      {approvalStep ? (
        <div className="grid gap-3 rounded-lg border border-border bg-muted/30 p-3">
          <div className="flex items-center justify-between gap-2">
            <p className="text-sm font-bold text-foreground">Approval step</p>
            <Badge>{approvalStep.mode === "all" ? "All" : "Any"}</Badge>
          </div>
          <Input label="Approval key" onChange={(event) => onUpdateApprovalStep({ key: event.target.value })} value={approvalStep.key} />
          <Input label="Approval name" onChange={(event) => onUpdateApprovalStep({ name: event.target.value })} value={approvalStep.name} />
          <Select label="Mode" onChange={(event) => onUpdateApprovalStep({ mode: event.target.value === "all" ? "all" : "any" })} value={approvalStep.mode}>
            <option value="any">Any approver</option>
            <option value="all">All approvers</option>
          </Select>
        </div>
      ) : null}
      <div className="grid gap-3">
        <div className="flex items-center justify-between gap-2">
          <p className="text-sm font-bold text-foreground">Actions</p>
          <Button onClick={onAddAction} size="sm" variant="outline">
            <Plus className="size-4" />
            Action
          </Button>
        </div>
        {(transition.actions ?? []).length > 0 ? (
          <div className="grid gap-3">
            {(transition.actions ?? []).map((action, index) => (
              <ActionEditor
                action={action}
                index={index}
                key={`${action.id}-${index}`}
                onRemove={onRemoveAction}
                onUpdate={onUpdateAction}
                onUpdateType={onUpdateActionType}
              />
            ))}
          </div>
        ) : (
          <div className="rounded-lg border border-dashed border-border bg-muted/40 p-3 text-sm font-semibold text-muted-foreground">
            No transition actions.
          </div>
        )}
      </div>
      <PanelErrors errors={errors} />
    </>
  );
}

function ActionEditor({
  action,
  index,
  onRemove,
  onUpdate,
  onUpdateType
}: {
  action: WorkflowActionDefinition;
  index: number;
  onRemove: (index: number) => void;
  onUpdate: (index: number, patch: Partial<WorkflowActionDefinition>) => void;
  onUpdateType: (index: number, type: WorkflowActionType) => void;
}) {
  return (
    <div className="grid gap-3 rounded-lg border border-border bg-card/80 p-3">
      <div className="flex items-center justify-between gap-2">
        <Badge>{index + 1}</Badge>
        <Button aria-label="Remove action" onClick={() => onRemove(index)} size="icon" variant="ghost">
          <Trash2 className="size-4" />
        </Button>
      </div>
      <Input label="Action ID" onChange={(event) => onUpdate(index, { id: event.target.value })} value={action.id} />
      <Select label="Type" onChange={(event) => onUpdateType(index, event.target.value as WorkflowActionType)} value={action.type}>
        {workflowActionOptions.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </Select>
      <Textarea
        className="min-h-20"
        label="Message"
        onChange={(event) => onUpdate(index, { message: event.target.value })}
        value={action.message ?? ""}
      />
    </div>
  );
}

function PanelHeader({ deleteDisabled, onDelete, title }: { deleteDisabled?: boolean; onDelete: () => void; title: string }) {
  return (
    <div className="flex items-center justify-between gap-3">
      <h3 className="text-base font-black text-foreground">{title}</h3>
      <Button aria-label={`Delete ${title.toLowerCase()}`} disabled={deleteDisabled} onClick={onDelete} size="icon" variant="ghost">
        <Trash2 className="size-4" />
      </Button>
    </div>
  );
}

function StateNodeLabel({ data }: { data: WorkflowGraphNodeData }) {
  return (
    <div className="min-w-40 max-w-48 px-2 py-1">
      <p className="truncate text-sm font-black text-foreground">{data.stateName || data.stateKey}</p>
      <p className="mt-0.5 truncate text-xs font-semibold text-muted-foreground">{data.stateKey}</p>
      <div className="mt-2 flex flex-wrap gap-1">
        {data.isInitial ? <Badge variant="info">Initial</Badge> : null}
        {data.isFinal ? <Badge variant="success">Final</Badge> : null}
        {data.validationErrors.length > 0 ? <Badge variant="danger">{data.validationErrors.length}</Badge> : null}
      </div>
    </div>
  );
}

function TransitionEdgeLabel({ data }: { data: WorkflowGraphEdgeData }) {
  return (
    <span className="inline-flex max-w-48 items-center gap-1 truncate rounded-full border border-border bg-card px-2 py-1 text-xs font-bold text-foreground shadow-soft">
      <span className="truncate">{data.transitionName || data.transitionKey}</span>
      {data.approvalStepKey ? <span className="text-primary">A</span> : null}
      {data.actionCount > 0 ? <span className="text-muted-foreground">{data.actionCount}</span> : null}
      {data.validationErrors.length > 0 ? <span className="text-danger">!</span> : null}
    </span>
  );
}

function PanelErrors({ errors }: { errors: string[] }) {
  if (errors.length === 0) {
    return null;
  }

  return (
    <div className="rounded-lg border border-danger/30 bg-danger-soft p-3">
      <ul className="grid gap-1 text-sm font-semibold text-danger">
        {errors.map((error) => (
          <li key={error}>{error}</li>
        ))}
      </ul>
    </div>
  );
}

function getNextActionIndex(actions: WorkflowActionDefinition[]): number {
  const ids = new Set(actions.map((action) => action.id));

  for (let index = actions.length + 1; ; index += 1) {
    if (!ids.has(`action-${index}`)) {
      return index;
    }
  }
}

function hasValidationErrors(node: Node): boolean {
  const validationErrors = node.data.validationErrors;
  return Array.isArray(validationErrors) && validationErrors.length > 0;
}

function useCompactWorkflowGraph(): boolean {
  const [isCompact, setIsCompact] = useState(() => (
    typeof window === "undefined" ? false : window.matchMedia("(max-width: 720px)").matches
  ));

  useEffect(() => {
    const mediaQuery = window.matchMedia("(max-width: 720px)");
    const updateCompactState = () => setIsCompact(mediaQuery.matches);

    updateCompactState();
    mediaQuery.addEventListener("change", updateCompactState);

    return () => {
      mediaQuery.removeEventListener("change", updateCompactState);
    };
  }, []);

  return isCompact;
}

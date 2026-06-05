# V5 Task 007: Visual Workflow Builder

## Goal

Add an optional visual workflow builder using XYFlow over the existing typed workflow config, after the table/form workflow authoring, execution, approval, and action foundations are stable.

## Context

Read:

- `docs/MASTER_PRD_FOR_AI.md`
- `docs/ROADMAP.md`
- `docs/TRIGGERS_AND_WORKFLOWS.md`
- `docs/UI_SPEC.md`
- `docs/API_SPEC.md`
- `docs/DATA_MODEL.md`
- `docs/PERMISSIONS.md`
- `tasks/v5/001-workflow-engine-foundation.md`
- `tasks/v5/002-workflow-management-ui.md`
- `tasks/v5/003-record-workflow-transition-execution.md`
- `tasks/v5/004-approval-inbox-and-notifications.md`
- `tasks/v5/005-workflow-transition-action-execution.md`
- `tasks/v5/006-trigger-to-workflow-starts.md`
- `AGENTS.md`

## Requirements

- Use XYFlow only for workflow diagrams and editing. Do not use XYFlow for form layout, reports, permissions, or trigger basics.
- Keep the typed workflow config as the source of truth. The visual builder must read and write the same states, transitions, approval steps, assignee rules, and action definitions used by the backend APIs.
- Provide a visual graph for states and transitions with clear final-state and initial-state treatment.
- Let users create/edit/delete states and transitions through structured side panels or dialogs, not raw graph-node JSON blobs.
- Support approval-step selection/creation for transitions without hiding the current table/form workflow semantics.
- Preserve publish/version behavior, validation errors, enabled state, and unpublished-change behavior from V5 task 002.
- Provide a fallback path to the existing table/form or JSON-backed editor for debugging and power-user edits.
- Add graph layout persistence only if it can be stored without changing the core workflow execution semantics.
- Keep record execution, approval response handling, action execution, and trigger-to-workflow starts out of this task unless they are only displayed as read-only context.

## Acceptance Criteria

- [x] `/workflows` includes a visual builder mode for editing workflow draft configs.
- [x] Visual state/transition edits produce the same typed config accepted by backend validation.
- [x] Users can edit states, transitions, approval references, and basic action metadata through structured UI.
- [x] Backend validation errors map back to visible graph/editor elements where practical.
- [x] Publish/enable/disable workflows continue to use the existing APIs and concurrency stamps.
- [x] Existing non-visual workflow management remains available.
- [x] The implementation does not introduce XYFlow into form layout or unrelated modules.
- [x] Documentation is updated for visual-builder scope and layout metadata.
- [x] Relevant tests/builds and frontend visual checks are run.

## Current Status

Implemented. `/workflows` now offers a workflow-only XYFlow visual mode over the existing typed draft config, with the JSON editor still available for debugging and power-user edits. No graph layout metadata is persisted and no backend API or database shape changed.

## Out of Scope

- Replacing the typed workflow config with graph-only data.
- A visual trigger builder.
- A visual form layout builder.
- Runtime execution changes.
- Custom code execution.
- PDF generation or print templates.

## Tests

- Add frontend unit tests for graph/config conversion helpers.
- Add frontend tests for validation error mapping where practical.
- Run `npm test` and `npm run build` in `src/app`.
- Use Playwright/webapp testing for at least one desktop and one mobile viewport if the visual builder changes interactive graph rendering.
- Run backend harness/build only if API contracts or persistence change.

## Notes

- The visual builder is a UI layer over the existing config, not a new workflow model.
- If layout metadata is introduced, keep it separate from execution semantics so workflow history and published versions stay stable.

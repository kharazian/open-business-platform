# V5 Task 002: Workflow Management UI

## Goal

Add a simple frontend workflow management workspace for the backend workflow definition APIs from V5 task 001 without introducing a visual builder.

## Context

Read:

- `docs/MASTER_PRD_FOR_AI.md`
- `docs/ROADMAP.md`
- `docs/TRIGGERS_AND_WORKFLOWS.md`
- `docs/API_SPEC.md`
- `docs/DATA_MODEL.md`
- `docs/PERMISSIONS.md`
- `tasks/v5/001-workflow-engine-foundation.md`
- `AGENTS.md`

## Requirements

- Add a real app workflow route and navigation entry.
- Let form managers choose a form and list that form's workflow definitions.
- Support creating and editing workflow definitions through structured metadata plus JSON config editing.
- Show workflow status, enabled state, current published version, unpublished changes, state count, transition count, and approval step count.
- Support publish, enable, and disable operations through the backend APIs.
- Surface backend validation errors in the workspace.
- Keep XYFlow, drag/drop visual workflow authoring, transition execution, approval inboxes, and workflow notifications out of scope.

## Acceptance Criteria

- [x] Frontend has typed workflow API helpers for list/create/get/update/publish/enable/disable.
- [x] Frontend builder helpers normalize workflow drafts, parse JSON config, validate required fields, and format workflow status labels.
- [x] `/workflows` route appears in the real app navigation for users with form management visibility.
- [x] The page can select a form, list workflows, create a workflow, edit an existing workflow, publish, enable, and disable.
- [x] Backend validation errors are displayed without losing the local draft.
- [x] The UI does not use XYFlow or implement transition execution.
- [x] Documentation and task index are updated.
- [x] Relevant tests/builds are run.

## Current Status

Completed for the current V5 frontend management slice.

Implemented files:

- `src/app/src/features/workflows/types.ts`
- `src/app/src/features/workflows/api.ts`
- `src/app/src/features/workflows/builder.ts`
- `src/app/src/features/workflows/pages/WorkflowsPage.tsx`
- `src/app/src/modules/workflows/module.tsx`

Verification:

- `npm test -- src/features/workflows/workflowBuilder.test.mjs src/features/workflows/workflowApi.test.mjs src/modules/moduleLabels.test.mjs`
- `npm run build`

## Out of Scope

- Visual workflow diagrams.
- Record workflow transition execution.
- Approval inbox.
- Workflow notifications.
- Trigger-to-workflow starts.
- Scheduled workflow starts.
- Workflow action execution.
- Backend schema changes.

## Tests

- Add frontend helper/API tests for request normalization, JSON config parsing, validation, status labels, and API endpoint shapes.
- Run `npm test` and `npm run build` in `src/app`.
- Run backend harness/build if backend permission constants or contracts change.

## Notes

- Start with a table/form-based workspace. A JSON config editor is acceptable for this slice because the backend contract is still proving itself.
- Reuse shared UI components and existing form/trigger page patterns.

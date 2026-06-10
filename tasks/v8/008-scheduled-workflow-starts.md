# V8 Task 008: Scheduled Workflow Starts

## Goal

Allow scheduled automation to start eligible workflows only when record selection and workflow context are explicit and safe.

## Context

Read:

- `docs/V8_START_HERE.md`
- `docs/TRIGGERS_AND_WORKFLOWS.md`
- `tasks/v5/006-trigger-to-workflow-starts.md`
- `tasks/v8/007-scheduled-automation-expansion.md`
- `AGENTS.md`

## Requirements

- Reuse published workflow start validation.
- Require explicit form, workflow, and record selection/filter rules.
- Prevent recursive or ambiguous workflow starts.
- Respect backend permissions and record scopes.
- Write workflow history and integration/trigger logs.
- Keep approval notifications consistent with current workflow behavior.

## Acceptance Criteria

- [x] Scheduled workflow start contracts are typed.
- [x] Only eligible published workflows can be started.
- [x] Record selection rules are explicit and validated.
- [x] Workflow history is written.
- [x] Trigger/integration logs capture run results.
- [x] Documentation is updated.
- [x] Tests are added where practical.
- [x] Relevant build/test commands are run.

## Implementation Notes

- Added `scheduled_start_workflow` as a scheduled-only trigger action separate from current-record `start_workflow`.
- Added explicit record selection metadata with supported modes: all records without active workflow, status equals, and field equals.
- Reused same-form enabled/published workflow validation with current version checks.
- Runtime execution skips records with active workflow state, starts the workflow on selected same-form records, writes workflow history/audit entries, and records per-record results in trigger logs.
- Updated frontend trigger builder types/helpers for the new action contract without adding a full scheduler UI.

## Out of Scope

- Starting workflows without records.
- Cross-form workflow starts.
- Custom code conditions.
- Full workflow scheduler UI beyond necessary controls.

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

- [ ] Scheduled workflow start contracts are typed.
- [ ] Only eligible published workflows can be started.
- [ ] Record selection rules are explicit and validated.
- [ ] Workflow history is written.
- [ ] Trigger/integration logs capture run results.
- [ ] Documentation is updated.
- [ ] Tests are added where practical.
- [ ] Relevant build/test commands are run.

## Out of Scope

- Starting workflows without records.
- Cross-form workflow starts.
- Custom code conditions.
- Full workflow scheduler UI beyond necessary controls.

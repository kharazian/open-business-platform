# V8 Task 001: API Keys And Integration Auth

## Goal

Add a secure API key foundation for integrations before any new public/internal API, webhook, import, or export endpoint is exposed.

## Context

Read:

- `docs/V8_START_HERE.md`
- `docs/MASTER_PRD_FOR_AI.md`
- `docs/API_SPEC.md`
- `docs/DATA_MODEL.md`
- `tasks/v8/README.md`
- `AGENTS.md`

## Requirements

- Store API keys hashed; never persist raw key material.
- Return the raw key only once at creation.
- Support active/revoked states and last-used metadata.
- Associate keys with a stable integration identity and optional created-by user.
- Add backend authentication plumbing for API key requests.
- Keep scopes conservative and typed.
- Add audit logs for create, revoke, and rotate actions.
- Do not expose record/report data in this task.

## Acceptance Criteria

- [ ] API key contracts are typed.
- [ ] API keys are stored hashed.
- [ ] Raw key material is only returned once.
- [ ] Revoked/inactive keys cannot authenticate.
- [ ] Last-used metadata is updated safely.
- [ ] Management endpoints require backend permissions.
- [ ] Documentation is updated.
- [ ] Tests are added where practical.
- [ ] Relevant build/test commands are run.

## Out of Scope

- Public record APIs.
- Incoming webhook listeners.
- Import/export jobs.
- External database sync.
- Custom code execution.

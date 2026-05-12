# Task V1: Project Inventory and Setup

## Goal

Inspect the existing React/.NET/PostgreSQL project and update documentation with real commands, folders, libraries, and conventions.

This task aligns the documentation set with this actual repository before feature work begins.

## Context

Read before starting:

- `docs/MASTER_PRD_FOR_AI.md`
- `AGENTS.md`
- Relevant docs for this task

## Requirements

- Follow the existing project conventions.
- Keep frontend and backend responsibilities separated.
- Confirm current frontend commands.
- Confirm current backend commands.
- Confirm Docker Compose services.
- Confirm current frontend folder structure.
- Confirm current backend folder structure.
- Confirm current product docs are accurate for this repo.
- Update documentation if commands, architecture, or contracts are inaccurate.

## Acceptance Criteria

- [ ] `README.md` describes this repository, not a generic template.
- [ ] `AGENTS.md` contains real current commands.
- [ ] `docs/TECH_STACK.md` lists the current stack and known planned additions.
- [ ] `docs/ARCHITECTURE.md` reflects the current `src/api` and `src/app` structure.
- [ ] `docs/UI_SPEC.md` mentions the shared component system and `/theme` playground.
- [ ] Existing behavior is not broken.
- [ ] Backend authorization is respected where applicable. This task should not weaken it.
- [ ] Frontend build is run if frontend docs are changed.
- [ ] Backend build is run if backend docs are changed.
- [ ] Files changed and risks are summarized.

## Out of Scope

Do not build form features yet.

## Notes

Implement only this task. Do not add unrelated features.

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

- [x] `README.md` describes this repository, not a generic template.
- [x] `AGENTS.md` contains real current commands.
- [x] `docs/TECH_STACK.md` lists the current stack and known planned additions.
- [x] `docs/ARCHITECTURE.md` reflects the current `src/api` and `src/app` structure.
- [x] `docs/UI_SPEC.md` mentions the shared component system and `/theme` playground.
- [x] Existing behavior is not broken.
- [x] Backend authorization is respected where applicable. This task should not weaken it.
- [x] Frontend build is run if frontend docs are changed.
- [x] Backend build is run if backend docs are changed.
- [x] Files changed and risks are summarized.

## Current Status

Completed for the V1 baseline. `npm test`, `npm run build`, the backend harness, and `dotnet build` passed with Node.js `v24.14.1` and .NET SDK `10.0.107`.

## Out of Scope

Do not build form features yet.

## Notes

Implement only this task. Do not add unrelated features.

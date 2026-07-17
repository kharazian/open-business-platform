# V9 Task 004: Advanced RBAC/ABAC Policy Model

## Status

Complete.

## Goal

Layer workspace-scoped, typed enterprise policy guardrails over the existing role and scoped-permission model without changing existing grants or introducing an unrestricted second grant system.

## Scope

- Persist workspace-owned access policies with name, resource type, optional resource ID, action, typed JSON conditions, enabled state, priority, audit metadata, and optimistic concurrency.
- Support deny-overrides policies for platform permissions, forms, reports, and records.
- Limit record policies to actions over existing rows: view, edit, delete, print, export, assign, and change-status.
- Support subject conditions for platform role, workspace membership role, department, and group.
- Support record conditions for status and current-user ownership.
- Apply policies after existing RBAC authorization succeeds; policies never create access that roles/scopes did not already grant.
- Apply record policies to both individual authorization and database-backed record queries.
- Exempt only the bootstrap recovery administrator from policy denial to preserve a lockout recovery path.
- Add `roles.manage`-protected policy list/create/update and simulation APIs.
- Audit policy mutations and document evaluation semantics.

## Out Of Scope

- Allow-effect policies, arbitrary expression languages, scripts, custom claims, time/IP/device conditions, or cross-workspace policies.
- Replacing existing role, form scope, report, or field permission tables.
- Row policies over arbitrary JSON form values.
- Policy authoring UI, approval workflow, version publishing, or bulk import/export.

## Evaluation Rules

- Existing RBAC and record scopes evaluate first.
- Any matching enabled deny policy rejects access.
- Condition dimensions combine with AND; values inside one dimension combine with OR.
- Empty condition dimensions are unconstrained.
- A resource ID of `null` applies to every resource of that type in the workspace.
- Bootstrap administration bypasses policies; normal `Admin` role users do not.
- Invalid or unsupported policies are rejected at write time rather than ignored at evaluation time.

## Acceptance Criteria

- [x] Access policy persistence is workspace-owned, indexed, concurrency-safe, and migration-documented.
- [x] Policy management requires `roles.manage`, returns typed contracts, and writes audit logs.
- [x] Platform, form, report, and record deny policies are enforced after existing grants.
- [x] Record policy filtering remains database-side for list/report/export consumers.
- [x] Role, membership-role, department, group, status, and ownership conditions have deterministic tests.
- [x] Policy simulation explains the matching deny policies without exposing another workspace.
- [x] Existing RBAC behavior is unchanged when no enabled policies exist.
- [x] Backend harness/build, migration consistency, frontend tests/build, and `git diff --check` pass.
- [x] Permission, API, data model, security, architecture, roadmap, master PRD, and V9 handoff docs are updated.

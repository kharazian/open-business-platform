# V9 Task 008: Localization Foundation

## Status

Complete.

## Goal

Establish workspace localization defaults, per-user overrides, and shared formatting/message APIs without attempting a full product translation in one task.

## Scope

- Persist one localization-default record per workspace and one optional preference row per workspace user.
- Support locale, IANA timezone, and first-day-of-week defaults.
- Let authenticated users manage their own locale/timezone overrides.
- Protect workspace-default mutations with `localization.manage` and audit all changes.
- Add a frontend localization context with date, date-time, number, and message formatting helpers.
- Add workspace-default and personal-preference controls to the real Settings page.

## Out Of Scope

- Translating all existing product copy, machine translation, locale-specific routes, content translation, RTL layout, or localized email/PDF templates.

## Safety Rules

- Backend validation is authoritative.
- Timezones must resolve through the server timezone database.
- Preferences are bound to the signed current user/workspace; request bodies cannot select another owner.
- User overrides never mutate workspace defaults.

## Acceptance Criteria

- [x] Workspace defaults and user preferences are workspace-isolated, unique, concurrency-safe, and audited.
- [x] Effective settings resolve user override, then workspace default, then platform fallback.
- [x] Workspace changes require `localization.manage`; users can change only their own preferences.
- [x] Shared frontend helpers format dates, date-times, and numbers through `Intl`.
- [x] The `/theme` playground remains independent.
- [x] Migration, backend harness/build, frontend tests/build, and `git diff --check` pass.
- [x] Architecture, API, data, security, roadmap, master PRD, and V9 handoff docs are updated.

# V9 Task 007: White Labeling And Workspace Branding

## Status

Complete.

## Goal

Let each workspace manage a safe, persistent visual identity for login and real app chrome while keeping per-user appearance preferences and the `/theme` playground independent.

## Scope

- Persist one concurrency-safe branding record per workspace.
- Support app name, short logo text, optional PNG/JPEG/WebP logo data, primary color, and login message.
- Expose a minimal anonymous projection selected by tenant/workspace slug and an authenticated current-workspace projection.
- Protect updates with `branding.manage` and record an audit entry for each change.
- Apply branding to login, document title, and real app navigation chrome.
- Replace the static workspace controls on Settings with a permission-aware branding editor.

## Out Of Scope

- Custom domains, email templates, arbitrary CSS/HTML, remote image fetching, SVG uploads, per-form branding, and changes to the `/theme` playground.

## Safety Rules

- Anonymous responses expose only display fields and never internal IDs, audit metadata, or concurrency state.
- Image uploads are restricted to PNG/JPEG/WebP data URLs and 256 KiB decoded size.
- Colors are restricted to six-digit hexadecimal values.
- Backend authorization is authoritative for mutations.

## Acceptance Criteria

- [x] Branding is workspace-owned, unique per workspace, audited, and concurrency-safe.
- [x] Anonymous lookup requires active tenant/workspace slugs and returns only safe display data.
- [x] Authenticated reads and authorized updates are workspace-isolated.
- [x] Login and real app chrome use persisted branding with deployment defaults as fallback.
- [x] User appearance settings and `/theme` remain independent.
- [x] Backend harness/build, migration consistency, frontend tests/build, and `git diff --check` pass.
- [x] Architecture, data model, security, roadmap, master PRD, and V9 handoff docs are updated.

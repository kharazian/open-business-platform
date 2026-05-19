# Persistent Forms List Create Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement `tasks/v1/004-form-list-and-create.md` end to end with persisted backend forms and a frontend `/forms` page that reads and creates real draft forms.

**Architecture:** Keep backend form behavior in the Forms module with explicit DTOs, a small service, and minimal API endpoints protected by the existing permission service. Keep frontend network code in `src/app/src/features/forms/api.ts`, pure list helpers in `drafts.ts`, and React state/error handling in `FormsListPage.tsx`.

**Tech Stack:** ASP.NET Core minimal APIs, EF Core/Npgsql, React, TypeScript, Vite, Tailwind CSS, lucide-react.

---

## File Structure

- Create `src/api/Modules/Forms/FormManagementContracts.cs`: list/create DTOs and API error shape for the Forms module.
- Create `src/api/Modules/Forms/FormManagementService.cs`: validation, list query, create draft persistence, and audit writing.
- Modify `src/api/Modules/Forms/FormsEndpoints.cs`: add `GET /api/forms` and `POST /api/forms` using `forms.create` / `forms.manage_all`.
- Modify `src/api.Tests/Program.cs`: add DTO/service contract assertions for the backend lightweight harness.
- Create `src/app/src/features/forms/api.ts`: typed fetch client for list/create forms.
- Create `src/app/src/features/forms/formsApi.test.mjs`: frontend API client behavior tests.
- Modify `src/app/src/features/forms/pages/FormsListPage.tsx`: load forms from API, create through API, and show loading/error states.
- Modify `src/app/src/features/forms/index.ts` and `src/app/package.json`: export API helpers and include the new test.
- Modify docs only if the API contract diverges from existing docs.

## Tasks

- [ ] Add backend red assertions for form list/create DTOs and service availability.
- [ ] Implement backend form contracts and `FormManagementService`.
- [ ] Add `GET /api/forms` and `POST /api/forms` endpoints with backend permission checks.
- [ ] Add frontend API client red tests for request shape, response parsing, and error handling.
- [ ] Implement frontend forms API client.
- [ ] Wire `FormsListPage` to the API with loading, error, refresh, and create states.
- [ ] Run frontend tests/build and backend test/build, then fix any failures from evidence.

## Acceptance Checklist

- [ ] `/api/forms` lists non-deleted forms ordered by recent update/create time.
- [ ] `POST /api/forms` creates a persisted draft with trimmed name and optional description.
- [ ] Backend validates blank names and enforces `forms.create` or `forms.manage_all`.
- [ ] `/forms` no longer depends on in-memory sample data for real app behavior.
- [ ] Existing form draft helper tests continue to pass.
- [ ] No field builder, layout builder, publishing, or record features are added.

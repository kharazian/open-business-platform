# Basic Field Builder Frontend Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the frontend V1 field builder for draft forms with add/edit/delete field behavior and local schema draft saving.

**Architecture:** Put pure schema-editing and draft-storage behavior in `src/app/src/features/forms/builder.ts` with tests. Render the builder experience in `FormBuilderPage.tsx`, register `/forms/:formId/builder`, and add build links from the Forms list.

**Tech Stack:** React, React Router, TypeScript, Vite, Tailwind CSS, lucide-react.

---

## File Structure

- Create `src/app/src/features/forms/builder.ts`: field metadata, empty schema creation, field add/update/delete helpers, option helpers, and local draft storage helpers.
- Create `src/app/src/features/forms/formBuilder.test.mjs`: helper tests compiled through `tsc`.
- Create `src/app/src/features/forms/pages/FormBuilderPage.tsx`: three-panel builder page.
- Modify `src/app/src/features/forms/pages/FormsListPage.tsx`: add builder actions.
- Modify `src/app/src/modules/forms/module.tsx`: register `/forms/:formId/builder`.
- Modify `src/app/src/features/forms/index.ts`: export builder helpers.
- Modify `src/app/package.json`: include the builder helper test in `npm test`.

## Tasks

- [ ] Add a failing helper test for builder schema operations.
- [ ] Implement `builder.ts` helper logic.
- [ ] Add route registration and list-page builder links.
- [ ] Build `FormBuilderPage.tsx` with palette, canvas, and settings panels.
- [ ] Run frontend tests and build.
- [ ] Fix any failures from evidence.

## Acceptance Checklist

- [ ] Builders can open `/forms/:formId/builder`.
- [ ] Builders can add all V1 field types.
- [ ] Builders can edit label, placeholder, help text, required, default value, and select/radio options.
- [ ] Builders can delete selected fields.
- [ ] Field schema and layout placement stay in sync.
- [ ] Draft schema can be saved and loaded from localStorage by form ID.
- [ ] No backend schema persistence, publishing, records, or responsive layout builder behavior is added.

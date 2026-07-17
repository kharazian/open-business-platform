# Task: Protected File Attachment Storage

## Goal

Replace `fileUpload` string placeholders with bounded, workspace-owned attachment metadata, protected binary storage, safe upload validation, and permission-checked audited downloads.

## Context

Read `AGENTS.md`, `docs/MASTER_PRD_FOR_AI.md`, `docs/ARCHITECTURE.md`, `docs/API_SPEC.md`, `docs/DATA_MODEL.md`, `docs/V10_START_HERE.md`, and `tasks/v10/README.md`.

The current schema accepts `fileUpload` as arbitrary text. Attachments need a two-phase lifecycle because content is uploaded before its record exists: an authorized upload creates a pending attachment, and record creation or editing claims it in the same transaction as the record mutation.

## Requirements

- Add bounded `fileUpload` schema configuration for maximum size and a subset of platform-supported content types.
- Store record values as attachment ID strings, never public paths, data URLs, original client paths, or raw bytes.
- Add workspace-owned attachment metadata with form/version/field, optional record, uploader, safe filename, verified type, size, SHA-256, storage provider/key, lifecycle/scan state, and timestamps.
- Put binary persistence and content safety inspection behind replaceable interfaces; provide PostgreSQL storage and deterministic local type/signature inspection.
- Limit files to 10 MiB; reject empty files, unsafe filenames, executable/script types, mismatched signatures, unsupported types, and malformed IDs.
- Add authenticated multipart upload, pending-delete, metadata, and download endpoints.
- Require form submit permission to upload for a published `fileUpload` field. Only the uploader may inspect/delete pending content.
- During create/edit, claim only pending attachments from the same workspace, form, immutable version, field, and current user, in the record transaction.
- For attached downloads, re-evaluate form view permission, record scope, and field visibility. Never emit a public download URL.
- Audit upload, claim/replacement, pending delete, and download without logging content.
- Render a single-file control in entry/edit mode and protected filename/download UI in read-only mode, including progress and field errors.
- Use safe filename display in records, reports, CSV, print, lookup labels, triggers, and workflows while retaining the ID as source data.
- Preserve legacy filename strings as display-only values; they must not become downloadable references.

## Acceptance Criteria

- [x] Schema validation accepts bounded attachment configuration and rejects unsupported types or excessive sizes.
- [x] Upload validation enforces size, filename, declared type, and content signature bounds.
- [x] Binary storage is accessed through an interface and protected metadata is workspace-owned.
- [x] Pending attachments cannot be claimed by another user, form, version, field, or workspace.
- [x] Record creation/edit atomically claims valid attachments and rejects invalid references.
- [x] Downloads enforce form, record-scope, and hidden-field permissions and write audit entries.
- [x] The frontend uploads, replaces, displays, downloads, and removes one attachment without embedding bytes in record JSON.
- [x] Reports, CSV, print, lookup, trigger, and workflow display paths use safe filenames.
- [x] Existing schemas and legacy string values remain readable.
- [x] Migration, API, data-model, and security behavior are documented.
- [x] Backend harness/build, frontend tests/build, migration consistency, PostgreSQL/API acceptance, and `git diff --check` pass.

## Out of Scope

- Multiple files in one field, folders, public sharing, anonymous uploads, or direct object-store URLs.
- External antivirus integration; this task defines its replaceable boundary and deterministic local inspection only.
- Image transformation, previews, OCR, indexing, resumable upload, or deduplication.
- Emailing user attachments or exposing bytes to triggers/webhooks.
- Background orphan cleanup; pending content is visible only to its uploader and can be deleted explicitly.
- Rewriting historical values or published versions.

## Tests

- Add backend schema, inspection, filename, ownership/claim, permission, and EF model tests.
- Add frontend API, renderer, upload-state, and display-format tests.
- Apply the migration to clean PostgreSQL and smoke-test authenticated upload, record claim, download, and denials.

## Migration Notes

- Add workspace-owned `file_attachments` with restrictive workspace/form/form-version/record/uploader foreign keys and record/field/uploader/lifecycle/created indexes.
- PostgreSQL-backed content is private `bytea`; DTOs and list projections never include it.
- Do not rewrite existing `fileUpload` strings, record JSON, or published schemas.

## Notes

- Cookie-authenticated multipart writes stay within the existing same-origin authenticated API boundary.
- Treat declared MIME type as untrusted; deterministic inspection selects the stored type.
- Clean API acceptance verified upload, SHA-256-identical download, pending-to-record claim, filename display resolution, authenticated no-access `404`, anonymous `401`, double-claim `400`, extension mismatch `400`, audited upload/claim/download, and pending deletion with content reduced to zero bytes.
- Implement only this task.

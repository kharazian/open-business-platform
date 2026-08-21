# Task: Analysis-Only Creator Export Assistant

## Goal

Add a bounded, permission-protected assistant that analyzes a user-supplied Creator-style text export and produces a secret-safe compatibility report without executing source code or creating, updating, publishing, or deleting platform definitions, records, permissions, connections, or jobs.

## Context

Read `AGENTS.md`, `docs/MASTER_PRD_FOR_AI.md`, `docs/ARCHITECTURE.md`, `docs/API_SPEC.md`, `docs/PERMISSIONS.md`, `docs/SECURITY_MODEL.md`, `docs/CREATOR_APP_SUPPORT_ROADMAP.md`, `docs/V10_START_HERE.md`, `tasks/v10/README.md`, tasks 001 through 009, and this task file.

V10 tasks 001 through 009 implement the platform capabilities needed to model a useful subset of operational apps: richer fields, durable relationships, nested reports, related-record workspaces, typed actions, bounded processing jobs, operational diagnostics, and failure notifications. Existing Creator-style exports remain untrusted source artifacts that may contain scripts, connection references, tokens, credentials, customer identifiers, or unsupported product constructs. The first migration-assistant slice must therefore be analysis-only and must not pretend to be a complete or authoritative parser for every source-product version.

## Requirements

- Add a dedicated `CreatorAnalysis` backend/frontend feature boundary. Do not place parsing logic in forms, reports, integrations, React components, or generic upload utilities.
- Accept one local UTF-8 text export per request. Initially allow `.ds` and `.txt` filenames with a text content type, cap source size at 1 MiB, reject empty/binary/malformed UTF-8 input, cap lines at 50,000, and do not accept ZIP archives, directories, URLs, remote fetches, or multipart batches.
- Require authentication plus effective `forms.manage_all` and `integrations.manage`. Apply the active-workspace boundary even though no source object is imported. Return a non-disclosing `403` for missing permission and do not add an anonymous analysis endpoint.
- Treat the source as hostile text. Never compile, evaluate, interpret, render as HTML, execute, shell out with, dynamically load, or send source text to an external service or model.
- Keep the source request-scoped and memory-only. Do not write it to PostgreSQL, local files, object storage, caches, integration logs, operational logs, audit metadata, exception messages, console logs, telemetry, snapshots, test fixtures, or frontend persistence.
- Add layered inspection rather than claiming a full vendor grammar: bounded lexical scanning, delimiter/string/comment awareness sufficient to avoid obvious false section matches, allowlisted construct recognition, and deterministic findings for unknown or malformed constructs. Parser failure must return a safe partial/incomplete report or a bounded validation error, never source excerpts or exception text.
- Detect credential-like material before constructing report strings. Use case-insensitive key/context categories such as password, secret, token, API key, private key, authorization, connection credential, and client secret, plus bounded high-entropy literal detection as a conservative signal. Report only category, safe construct location, and count; never return or log the matched value, surrounding source line, entropy sample, connection string, endpoint query, or header.
- Treat connection definitions/references, OAuth/API authentication, source functions/scripts, custom pages, embedded HTML, remote endpoints, and record-data literals as sensitive or executable constructs. Inventory them by safe type/name when possible, mark them `manual_review` or `unsafe`, and never project their bodies, parameters, URLs, payloads, or values.
- Generate stable typed findings with `supported`, `manual_review`, `unsupported`, `unsafe`, and `unknown` statuses; fixed reason codes; severity; source construct type; a bounded safe display name or generated ordinal; optional line number/range only; proposed platform module/type when allowlisted; and platform-authored guidance. Do not include arbitrary source messages or snippets.
- Redact a construct display name when it is credential-like, control-character-bearing, invalid UTF-8, longer than 160 characters, or otherwise unsafe for plain-text UI. Normalize safe names for display only; do not silently use them as future platform IDs.
- Bound the report to at most 500 construct entries and 1,000 findings. Set explicit truncation/incomplete flags and aggregate counts when limits are reached. Do not use unbounded regex backtracking, recursion, token arrays, or in-memory result growth.
- Inventory applications/components, forms, fields, reports, relationships, workflows, functions, schedules, pages, permissions, connections, and possible data sections when recognized. Unknown sections remain visible as counts/findings instead of being ignored.
- Use a fixed initial field compatibility catalog. Direct candidates may include text, textarea, email, phone, number/decimal, currency, percent, date, datetime, time, URL, checkbox/decision, single-select/dropdown, radio, autonumber, address, and bounded file-upload metadata. Lookup, multi-select, subform/grid, user, and organization pickers require manual review because target identity, cardinality, or ownership semantics must be resolved.
- Report field mapping is advisory only. Do not emit a platform form schema, layout, draft, published version, record, attachment, lookup edge, autonumber allocation, or executable import mapping in this task.
- Recognize only list-style report candidates that can plausibly map to saved list reports. Mark calendar, pivot, summary, chart/page, custom HTML, and other report styles for manual review or unsupported status. Never execute source filters, formulas, actions, or queries.
- Inventory lookup/report dependencies as safe symbolic edges and flag missing, ambiguous, cyclic, or unsupported targets. Do not query current workspace forms/reports to auto-bind names and do not disclose existing workspace object metadata in the analysis response.
- Inventory workflows, schedules, actions, and functions but never translate them into trigger/workflow/processing definitions. Classify only against existing fixed platform capabilities and require manual redesign for source scripts, arbitrary expressions, connector calls, unsafe scheduled work, and unsupported action types.
- Inventory source roles/permissions as advisory names and counts only. Never create roles or grants, treat a source permission as authorization, simulate a user, or weaken backend enforcement. Flag owner/creator/everyone/custom criteria and unknown principals for manual security review.
- Return a migration-readiness summary with safe counts by construct/status, detected credential categories/counts, dependency issues, supported candidates, manual work, unsupported/unsafe blockers, completeness/truncation state, and a clear `canImport: false`. Do not return confidence claims that imply vendor certification or guaranteed semantic equivalence.
- Allow a client-side download of the already-sanitized report JSON if useful. Do not create a server-side analysis artifact or public/protected download record in this task.
- Write one payload-free audit entry after a completed analysis containing only source byte/line counts, safe aggregate construct/finding/status counts, credential-category counts, truncation/incomplete flags, analyzer version, and a SHA-256 source fingerprint. Never include the filename, source names, snippets, matched values, URLs, or raw parser errors. Failed validation before analysis does not need an audit row.
- Provide an `/integrations` Creator analysis tab or similarly bounded administration surface. It must explain analysis-only behavior, require explicit local file selection, show upload/validation/analyzing/report/error states, render only sanitized typed findings, support status/type filters without raw source preview, and make it impossible to trigger an import from this task.
- Keep the browser source only long enough to submit the request. Clear file/input state after success, failure, route exit, and component unmount where practical; do not use `localStorage`, `sessionStorage`, IndexedDB, service-worker caches, query strings, or analytics events for the source or report.
- Use platform-authored generic error messages. Never return parser stack traces, source excerpts, raw filenames, credential matches, or line contents. Application logs may contain only aggregate counters, analyzer version, request correlation, elapsed time, and safe error codes.
- Version the analyzer/report contract independently (initially `creator-analysis-v1`) so later grammar/catalog improvements do not imply that earlier results were complete.
- Do not add a database migration unless implementation review finds authoritative analysis state that cannot be avoided. The preferred implementation stores only the normal payload-free audit entry.

## Proposed API Contract

- `POST /api/creator-analysis`
- Content type: `multipart/form-data`
- File part: `source` (`.ds` or `.txt`, UTF-8 text, maximum 1 MiB)
- Response: `200 OK` with an ephemeral sanitized report.

Illustrative response shape:

```json
{
  "analyzerVersion": "creator-analysis-v1",
  "canImport": false,
  "complete": true,
  "truncated": false,
  "source": {
    "byteCount": 48210,
    "lineCount": 1320
  },
  "summary": {
    "constructCount": 42,
    "findingCount": 17,
    "byStatus": {
      "supported": 21,
      "manual_review": 13,
      "unsupported": 3,
      "unsafe": 4,
      "unknown": 1
    }
  },
  "credentialSignals": [
    { "category": "connection_credential", "count": 2 }
  ],
  "constructs": [
    {
      "id": "construct-12",
      "type": "field",
      "displayName": "Order Number",
      "lineStart": 84,
      "lineEnd": 90,
      "status": "supported",
      "proposedModule": "forms",
      "proposedType": "autonumber"
    }
  ],
  "findings": [
    {
      "id": "finding-7",
      "severity": "warning",
      "status": "manual_review",
      "reasonCode": "lookup_target_requires_mapping",
      "constructId": "construct-18",
      "message": "Lookup targets require an explicit platform form mapping."
    }
  ]
}
```

Exact DTO names may follow repository conventions. Unknown multipart fields are rejected. The API never echoes the filename or source content. `413` is used for oversized input; malformed/binary/unsupported input returns a bounded `400`; authorization returns `403`.

## Acceptance Criteria

- [x] A dedicated, versioned analyzer accepts one bounded UTF-8 `.ds`/`.txt` source and rejects empty, binary, oversized, excessive-line, batch, archive, URL, and unknown multipart inputs.
- [x] Analysis requires active-workspace authentication plus `forms.manage_all` and `integrations.manage` and has no anonymous or API-key route.
- [x] Source content is request-scoped and never persisted, logged, cached, audited, rendered, executed, or sent externally.
- [x] Credential detection occurs before report construction and returns categories/counts only; values, snippets, URLs, headers, connection strings, and filenames never leave the analyzer.
- [x] The analyzer uses bounded deterministic scanning and typed catalogs without claiming complete vendor grammar coverage.
- [x] Supported, manual-review, unsupported, unsafe, and unknown constructs receive fixed reason codes and platform-authored guidance with safe names/locations only.
- [x] Field/report/relationship candidates align with current platform contracts while lookups, multi-selects, subforms, permissions, workflows, functions, pages, schedules, connections, and data receive conservative treatment.
- [x] The report is capped at 500 constructs and 1,000 findings and explicitly reports truncation/incompleteness.
- [x] Every response has `canImport: false`; no endpoint/UI action can create or mutate forms, records, reports, permissions, triggers, workflows, connections, processing jobs, or attachments.
- [x] A completed analysis writes exactly one payload-free audit event with aggregate counts, version, flags, and source fingerprint only.
- [x] The frontend provides local-file, validation, analysis, filtering, safe report, JSON-download, reset, error, and stale-authorization states without source preview or browser persistence.
- [x] No migration or server-side artifact is added unless separately justified during implementation review.
- [x] API, architecture, security, permission, roadmap, Creator-support, and V10 documentation plus backend/frontend tests are complete.
- [x] Backend harness/build, frontend tests/build, authenticated PostgreSQL/API acceptance, payload-leak scans, bounded adversarial input tests, browser acceptance, and `git diff --check` pass.

## Out of Scope

- Creating, updating, publishing, deleting, or auto-mapping platform forms, layouts, records, reports, dashboards, permissions, triggers, workflows, print templates, connectors, jobs, notifications, or settings.
- A complete or vendor-certified parser, semantic equivalence guarantee, round-trip export, source-version upgrade, dependency installer, or migration-success percentage claim.
- Executing or translating Deluge/source scripts, formulas, queries, custom HTML/pages, embedded JavaScript, API calls, connector actions, schedules, or workflow expressions.
- Uploading ZIPs, folders, multiple files, remote URLs, cloud-drive sources, database dumps, attachments, record datasets, or source-product backups.
- Persisting source files, sanitized copies, server-side reports, analysis history, source construct catalogs, or credential findings outside the payload-free audit aggregate.
- Importing credentials, connection details, tokens, secrets, private keys, cookies, authorization headers, endpoints with query data, customer data, or source records.
- Automatically binding source names to current workspace objects, resolving users/roles/groups/departments, or interpreting source permissions as platform grants.
- AI/model-assisted source analysis, external scanners, network lookups, telemetry uploads, or third-party parsing services.
- Generating executable migration plans, code, platform DTOs, schemas, SQL, scripts, or API requests from source content.
- Adding a general-purpose file scanner, code viewer, syntax highlighter, migration framework, or arbitrary import engine.

## Tests

- Add backend validation tests for extension/content type, UTF-8 validity, binary detection, empty source, 1 MiB bytes, 50,000 lines, unknown multipart fields, single-file enforcement, cancellation, and generic errors.
- Add authorization tests for authentication, both required permissions, active workspace, inactive/suspended membership, bootstrap behavior, and API-key rejection.
- Add lexical/parser tests for comments, quoted strings, escaped delimiters, malformed/nested sections, unknown constructs, deterministic ordering/IDs, partial results, and analyzer-version stability.
- Add catalog tests for direct field candidates, manual lookup/multi-select/subform/picker mappings, list report candidates, unsupported report/page types, dependency findings, permission inventory, workflow/function/schedule inventory, and fixed reason codes.
- Add secret-safety tests with passwords, tokens, API keys, private keys, authorization headers, connection strings, URLs with queries, high-entropy literals, customer-like data, and credential-like construct names. Assert exact sensitive values never appear in DTOs, audit rows, application logs, error messages, snapshots, or frontend DOM.
- Add bounds/adversarial tests for maximum constructs/findings, long identifiers, long lines, control characters, catastrophic-regex candidates, deep delimiter nesting, repeated unknown sections, large comments/strings, and predictable memory/time behavior.
- Add audit tests proving one successful aggregate-only event, no filename/names/snippets/values, correct fingerprint/version/counts, and no audit entry for pre-analysis validation rejection.
- Add mutation tests proving table counts for forms, versions, records, reports, permissions, triggers, workflows, integrations, processing definitions/runs, attachments, and notifications remain unchanged after analysis.
- Add frontend API/page tests for multipart request construction, safe typed rendering, filters, truncation/incomplete warnings, client-side sanitized JSON download, reset/retry, permission failures, and absence of import controls/source preview/browser persistence.
- Exercise authenticated PostgreSQL/API and browser acceptance with a synthetic secret-bearing fixture created only under `/tmp`, then remove the fixture and scan repository/worktree, API output, audit metadata, application logs, and rendered DOM for seeded sentinel values.

## Migration Notes

- No migration is planned. Analysis source and reports remain ephemeral.
- Reuse the workspace-owned audit table for one aggregate-only completion event. Do not add a source-file, analysis-session, finding, artifact, credential-signal, or migration-plan table.
- If future product requirements need analysis history or import application, specify a separate task with explicit encrypted storage, retention, legal-hold, authorization, artifact, concurrency, and confirmation semantics.

## Review Decisions Proposed

- Task 010 is a compatibility analyzer, not an importer. Every result says `canImport: false`.
- A conservative bounded scanner is safer and more honest than claiming complete support for an evolving external grammar.
- Source files are hostile and may contain secrets; request-scoped memory-only handling is the default, not optional cleanup after persistence.
- Existing `forms.manage_all` plus `integrations.manage` represents the narrow available administration boundary without introducing a new permission solely for this first slice.
- Safe symbolic inventory is useful, but source code, expressions, payloads, permissions, and connections never become executable platform definitions here.
- A payload-free aggregate audit event satisfies sensitive-action accountability without retaining the source or sanitized report.
- Client-side download of the sanitized response avoids a new artifact/history model.
- Any future apply/import phase requires its own reviewed task, explicit preview/confirmation, durable idempotency, conflict handling, and module-specific backend authorization.

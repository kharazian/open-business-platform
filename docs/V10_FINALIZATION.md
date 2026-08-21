# V10 Finalization

V10 Operational App Modeling is complete for tasks 001 through 010 and is accepted as a project checkpoint.

## Delivered

1. Bounded structured address fields across schema, records, reports, CSV, and print.
2. PostgreSQL-allocated immutable autonumber fields with per-workspace/form/field sequences.
3. Workspace-owned protected file attachments with bounded inspection, atomic record claims, and audited downloads.
4. Canonical lookup relationship edges with legacy-reference protection and restrictive delete behavior.
5. Permission-safe one-hop related report fields for catalog, filters, search, sort, CSV, and print.
6. Read-only reverse related-record panels with canonical/legacy deduplication and independent pagination.
7. Saved typed report and row actions projected through current backend permissions.
8. Bounded CSV-import and protected-export processing definitions, schedules, fenced runs, and export-only retries.
9. Separate payload-safe processing diagnostics, bounded retention/health, and deduplicated terminal-failure notifications.
10. A versioned memory-only Creator export analyzer that returns secret-safe compatibility findings with `canImport: false`.

Dashboard publishing, stable slugs, directory/navigation projection, and legacy adapter compatibility were also accepted during the V10 work and remain separate from the ten-task operational modeling sequence.

## Boundaries Preserved

- PostgreSQL, active-workspace ownership, backend permissions, record scopes, field security, and deny-overrides policies remain authoritative.
- Published form versions remain immutable, and every record retains its submission form version.
- Address values and report/action configuration use existing bounded JSONB contracts; relational state was added only where authoritative concurrency, ownership, integrity, or operations history required it.
- Attachment bytes never enter record JSON or public URLs. Downloads reauthorize record and field access and are audited.
- Relationship APIs remain module-specific. V10 adds one forward report hop and read-only reverse panels, not a generic graph or mutation API.
- Typed report actions reuse existing destination endpoints; no generic action executor, arbitrary URL, expression, or script path was added.
- Processing supports only the two reviewed import/export kinds. CSV imports fail closed after uncertain interruption and cannot retry; exports remain bounded and are the only scheduled/retryable kind.
- Processing operational logs remain separate from audit, integration, trigger, notification, and run history.
- Creator export analysis is request-scoped, memory-only, non-executing, and non-importing. Source content and credential values are never persisted or returned.
- `/theme` remains a sample-data/design-system playground and does not own reusable application primitives.

## Database Checkpoint

V10-era authoritative migrations are:

1. `20260717191123_BackendGeneratedAutonumbers`
2. `20260717201359_ProtectedFileAttachments`
3. `20260717205617_LookupRelationshipIntegrity`
4. `20260818201241_DashboardPublishingAndNavigation`
5. `20260819201648_BoundedProcessingJobs`
6. `20260820172637_ProcessingOperationsAndFailureNotifications`

Tasks 001, 005, 006, 007, and 010 intentionally add no migration. Their additive value/configuration contracts stay in existing schema, record, report, or audit storage.

On 2026-08-21, an isolated PostgreSQL database accepted all 42 migrations from the initial foundation through `20260820172637_ProcessingOperationsAndFailureNotifications`. The V10 tables were present, and `dotnet ef migrations has-pending-model-changes` reported no model drift. The isolated database was removed after verification.

## Verification Gate

The final V10 state passed:

```bash
dotnet run --project src/api.Tests/OpenBusinessPlatform.Api.Tests.csproj --no-build
dotnet build src/api.Tests/OpenBusinessPlatform.Api.Tests.csproj --no-restore
cd src/app
npm test -- --run
npm run build
npm audit
git diff --check
```

Task-level authenticated PostgreSQL/API acceptance covered autonumber concurrency, attachment lifecycle and denials, relationship integrity, nested report permissions, related-record projections, typed actions, processing claims/recovery/deduplication, notification behavior, and fixture cleanup. Task 010 additionally passed authenticated API, payload-leak, mutation-count, sanitized-download, and headless Chromium acceptance with its synthetic source removed afterward.

The final security review pinned `react-router-dom` to `7.18.2` and Vite to `8.2.2`; `npm audit` then reported zero production or development vulnerabilities. The NuGet package scan reported no vulnerable direct or transitive backend packages from the configured source. This environment may still emit `NU1900` when the NuGet vulnerability service is intermittently unreachable; that warning does not represent a compile or test failure and should continue to be monitored in CI.

## Deferred Limits And Risks

- Attachment inspection is deterministic and replaceable but is not an external antivirus service. Binary storage remains PostgreSQL-backed for the bounded foundation, and automated pending-orphan cleanup is deferred.
- Reports support one forward lookup hop; related-record panels are read-only and not user-configurable.
- Processing has no scheduled imports, arbitrary transformations, remote file transport, connector execution, external alert delivery, or exactly-once distributed guarantee.
- Processing alert delivery is in-app only. Email/SMS/chat/on-call routing and incident workflows remain separate product work.
- Creator analysis is conservative rather than vendor-certified and has no apply/import phase, analysis history, source retention, or external model/scanner call.
- Production-specific OIDC, DNS/TLS/proxy, SMTP, object-storage/scanner, multi-instance worker, and production-scale retention checks remain deployment acceptance responsibilities.
- The backend test project remains a lightweight executable harness rather than a formal `dotnet test` project.

## Next

V10 is accepted and should remain stable. Define and review a separate V11 plan before adding product scope. In particular, Creator apply/import, deeper relationship traversal, richer processing kinds, and external alert delivery must not be inferred from this checkpoint.

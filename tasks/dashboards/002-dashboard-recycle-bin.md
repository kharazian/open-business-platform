# Dashboard Recycle Bin

Status: implemented and verified.

## Goal

Give dashboard managers a recoverable archive lifecycle without allowing archived or draft data to leak into live dashboard routes.

## Scope

- Searchable manager-only recycle-bin interface with archive actor/time and widget count.
- Safe restore to an unpublished, non-navigable draft with no slug.
- Permanent deletion only for archived dashboards after a configurable wait.
- Current concurrency stamp and exact case-sensitive dashboard-name confirmation.
- Preserve metadata-only audit history while deleting dashboard-owned revisions.
- No automatic purge and no database migration.

## Acceptance criteria

- Normal viewers receive `403` from all recycle-bin management endpoints.
- Archive clears published exposure, default status, and published snapshot.
- Restore preserves editable content and revisions but cannot recreate a live route.
- Production waits 30 days by default; development may use zero days for E2E tests.
- Permanent deletion is transactional and retains `dashboard_permanently_deleted` audit metadata.
- Frontend unit tests, backend harness, production build, bundle budget, real-browser lifecycle test, and pending-migration check pass.

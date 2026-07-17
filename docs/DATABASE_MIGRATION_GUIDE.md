# Database Migration Guide

## Database

PostgreSQL.

## Migration Tool

The backend uses EF Core migrations with the Npgsql PostgreSQL provider.

Migrations live under:

```txt
src/api/Infrastructure/Persistence/Migrations
```

If `dotnet ef` is not installed locally, install a compatible EF Core tool version:

```bash
dotnet tool install --global dotnet-ef --version 10.0.4
```

Generate a migration:

```bash
dotnet ef migrations add MigrationName \
  --project src/api/OpenBusinessPlatform.Api.csproj \
  --startup-project src/api/OpenBusinessPlatform.Api.csproj \
  --output-dir Infrastructure/Persistence/Migrations
```

Apply migrations locally after PostgreSQL is running:

```bash
docker compose up -d
dotnet ef database update \
  --project src/api/OpenBusinessPlatform.Api.csproj \
  --startup-project src/api/OpenBusinessPlatform.Api.csproj
```

Apply migrations in the deployment Compose stack with the one-shot migrator service:

```bash
docker compose \
  --profile migrate \
  --env-file deploy/env/stage.env \
  -f deploy/compose.yml \
  -f deploy/compose.stage.yml \
  -f deploy/compose.proxy.yml \
  run --rm migrator
```

Host-run local development uses PostgreSQL on `localhost:55432` by default so it does not collide with a machine-level PostgreSQL service on `5432`. If `database update` reports password authentication failures, first confirm the API and EF commands are using the project Compose port. If the port is correct, the existing Docker volume may have been initialized with older credentials. Either use the password that initialized the volume or intentionally recreate the local development volume after confirming no local data needs to be kept.

Check that the model matches the committed migration:

```bash
dotnet ef migrations has-pending-model-changes \
  --project src/api/OpenBusinessPlatform.Api.csproj \
  --startup-project src/api/OpenBusinessPlatform.Api.csproj
```

## Migration Rules

- Do not mutate published form versions.
- Add indexes for frequently queried columns.
- Document any JSONB structure changes.
- Use nullable columns carefully.
- For breaking changes, plan backfill scripts.

## Important Indexes

Records:

- form_id
- form_version_id
- status
- owner_id
- department_id
- created_by_id
- created_at

Reports:

- form_id
- type
- created_by_id

Audit logs:

- entity_type/entity_id
- user_id
- created_at

Password reset tokens:

- token_hash unique
- user_id
- expires_at

Triggers:

- form_id
- event_name
- is_enabled

Trigger event outbox:

- status + next_attempt_at
- locked_at
- form_id
- record_id

## V8 Production Hardening Migration

`20260715174713_V8ProductionHardening` adds nullable `trigger_definitions.schedule_locked_at` for atomic scheduler claims. It also records EF Core concurrency-token metadata for existing `concurrency_stamp` properties; that metadata changes update predicates but does not alter the PostgreSQL concurrency-stamp columns.

## Transactional Trigger Event Outbox Migration

`20260715180727_TransactionalTriggerEventOutbox` adds `trigger_event_outbox` for record events committed atomically with their source mutation. The table stores internal JSONB event payloads, delivery status, bounded retry metadata, and five-minute lease/fencing fields. It does not backfill historical record changes.

## Workspace Branding Migration

`20260717150535_WorkspaceBranding` adds one optional `workspace_branding` row per workspace. Existing workspaces require no backfill: APIs resolve deployment/workspace defaults until an authorized administrator saves branding. The unique workspace index, global workspace filter, write guard, and concurrency stamp provide isolation and optimistic concurrency.

## Localization Foundation Migration

`20260717151922_LocalizationFoundation` adds optional `workspace_localizations` defaults and `user_localization_preferences` overrides. Existing workspaces/users require no backfill because effective settings fall back to `en-CA` and `UTC`; unique workspace/user indexes and concurrency stamps protect later writes.

## Custom Domains Migration

`20260717152713_CustomDomains` adds `workspace_custom_domains` with globally unique normalized hostnames, verification lifecycle fields, workspace ownership, and optimistic concurrency. It has no backfill and does not change proxy or TLS configuration.

## Backend-Generated Autonumbers Migration

`20260717191123_BackendGeneratedAutonumbers` adds workspace-owned `form_autonumber_sequences`. A unique `(workspace_id, form_id, field_id)` index supports atomic per-field allocation, and restrictive workspace/form foreign keys preserve ownership. The migration does not backfill or modify existing schemas, form versions, or record JSON.

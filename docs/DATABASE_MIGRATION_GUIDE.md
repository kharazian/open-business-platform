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

Triggers:

- form_id
- event_name
- is_enabled

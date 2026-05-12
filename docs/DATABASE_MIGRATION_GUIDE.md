# Database Migration Guide

## Database

PostgreSQL.

## Preferred Migration Tool

The current backend skeleton does not yet include EF Core migrations.

When EF Core is added, use EF Core migrations unless the project deliberately chooses another migration tool.

If using EF Core:

```bash
dotnet ef migrations add MigrationName
dotnet ef database update
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
- created_by
- created_at

Reports:

- form_id

Audit logs:

- entity_type/entity_id
- user_id
- created_at

Triggers:

- form_id
- event_name
- is_enabled

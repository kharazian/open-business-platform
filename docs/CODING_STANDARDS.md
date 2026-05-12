# Coding Standards

## General

- Keep code modular.
- Prefer small files with clear responsibility.
- Avoid business logic inside UI components or API controllers.
- Use consistent naming.
- Add comments only when logic is not obvious.
- Avoid large dependencies unless justified.

## React

- Prefer TypeScript.
- Avoid `any`.
- Use reusable components.
- Put shared UI primitives in `src/app/src/components/ui`.
- Put shared shell/navigation components in `src/app/src/components/layout`.
- Keep `/theme` pages sample-data only.
- Do not duplicate reusable components under `/theme`.
- Use the same shared classes/components in the real app and `/theme`.
- Separate API calls from UI components.
- Separate builder state from renderer logic.
- Keep FormBuilder and FormRenderer separate.
- Keep ReportBuilder and ReportViewer separate.

## .NET Core

- Keep controllers thin.
- Use services for business logic.
- Use DTOs for API contracts.
- Validate inputs.
- Use async APIs for database operations.
- Use transactions for multi-step writes.
- Centralize permission checks.

## PostgreSQL / EF Core

- Use migrations for schema changes.
- Use JSONB for flexible schemas/configs.
- Use relational columns for frequently filtered data.
- Add indexes intentionally.
- Do not mutate published form versions.

## Documentation

Update docs when changing:

- Architecture
- Data model
- API contracts
- Permissions
- Trigger behavior
- Roadmap scope

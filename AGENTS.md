# AGENTS.md

## Project Goal

Build an open-source modular business platform for internal company systems.

## Architecture Rules

- Use modular monolith architecture.
- Use practical Clean Architecture.
- Keep modules independent.
- Do not use microservices in early versions.
- Do not add dynamic plugin loading yet.
- Do not overuse abstractions.
- Prefer readable code over clever code.

## Backend

- ASP.NET Core
- PostgreSQL
- EF Core
- OpenTelemetry later
- OpenIddict or external OIDC later

## Frontend

- React
- Vite
- TypeScript
- Simple dashboard first
- Native Federation later only when needed

## First Modules

- Dashboard
- Users
- Roles
- Permissions
- Audit Logs

## Coding Style

- Keep code simple.
- Avoid generic repositories unless necessary.
- Avoid empty layers.
- Add tests for important logic.
- Keep README updated when commands change.
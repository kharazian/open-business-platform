# Architecture

## Style

The project uses a Modular Monolith with practical Clean Architecture.

It is not a microservices system in the beginning.

## Backend Structure

src/
  ApiHost/
  BuildingBlocks/
  Modules/
    Users/
    AuditLogs/
    Dashboard/
    Forms/
    Workflows/

## Module Structure

Each module may contain:

- Domain
- Application
- Infrastructure
- Api

Example:

Modules/
  Users/
    Domain/
    Application/
    Infrastructure/
    Api/

## Rules

- Domain logic must not depend on database or web framework.
- Modules should not directly access each other’s tables.
- Cross-module communication should happen through contracts or events.
- Keep abstractions simple.
- Avoid unnecessary interfaces.
- Avoid dynamic plugin loading in version 0.1.
- Add advanced plugin loading later only when needed.

## First Backend Modules

- Users
- Roles
- Permissions
- Audit Logs
- Dashboard

## First Frontend App

React + Vite dashboard with:

- Login
- Sidebar
- Dashboard page
- Users page
- Roles page
- Permissions page
- Audit logs page

## Theme Playground

The theme playground is a sample-data-only admin UI used to preview reusable shell, navigation, and page patterns.

- Keep the theme shell shared with the main app shell where possible.
- Keep theme navigation generated from the theme page registry.
- Use parent-child navigation groups for larger theme menus.
- Keep settings controls in the settings popup interactive; route menus should close after route selection.
- Validate frontend shell changes with `npm run build` from `src/app`.

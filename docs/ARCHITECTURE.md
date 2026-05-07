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
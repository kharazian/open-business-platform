# Product Requirements Document

## Product Name

Low-Code Business Form Platform

## Overview

The platform allows organizations to build responsive forms, collect and manage records, define reports, print records and lists, control access, automate actions, and eventually define approval workflows.

The first version should focus on a strong foundation: form builder, responsive layout, records, basic permissions, basic printing, and audit logs.

## Current Repository State

The repository currently has the project skeleton plus the first V1 foundations:

- ASP.NET Core minimal API host
- React frontend shell
- Frontend module registry for permission-aware app routes/navigation
- Shared UI/layout component foundation
- Shared V1 form schema contracts and validation helpers in frontend and backend code
- API health, authenticated dashboard summary, auth, users, roles, role permissions, and forms list/create endpoints
- EF Core/Npgsql persistence foundation for users, roles, departments, forms, form versions, records, role/form permissions, and audit logs
- Cookie auth with bootstrap admin fallback and local PostgreSQL user login
- Users & Access workspace for local users, roles, menu permissions, and per-form role access
- Persistent Forms list/create page and backend-owned field-builder page
- Form draft persistence, responsive layout, preview, publishing, published-form submission, record list/detail, record edit/delete, browser print, basic record permissions, and core audit logs
- Real app appearance settings for palette, density, layout, radius, and shadow
- Lightweight frontend tests for module registry, form schema/record validation, forms API/list/builder helpers, auth, users, and shared UI helpers
- `/theme` playground with sample data
- Docker Compose for PostgreSQL and Redis

The product features in this PRD should be implemented task by task. Dashboard, reports, settings/profile, and `/theme` remain starter/sample surfaces until real modules are added. Forms, Users & Access, records, record-level permissions, browser print, and core audit logs now have V1 foundations. Seed/demo data is still pending.

## Problem

Many organizations need internal applications for collecting structured data, tracking records, generating reports, controlling access, and automating simple processes. Building each application manually is slow and expensive.

The platform solves this by allowing non-developers or power users to configure forms and record workflows with minimal custom development.

## Goals

- Build responsive forms without coding.
- Store submitted data as records.
- View, search, edit, and print records.
- Create reports over records.
- Control access by user, role, group, department, and ownership.
- Automate actions using triggers.
- Support approval workflows in later versions.
- Keep the system modular and maintainable.

## Non-Goals for V1

- Full visual workflow builder
- Full report designer
- Advanced dashboards
- Custom PDF designer
- Complex ABAC rules
- External integrations
- Multi-tenant enterprise management

## Users

### Admin

Manages system settings, users, forms, permissions, and data.

### Builder

Creates and manages forms, layouts, reports, and basic settings.

### User

Submits forms and views records they are allowed to access.

### Viewer

Views reports or records they are allowed to see.

### Manager / Approver Later

Reviews records and participates in workflow approval.

## Core User Stories

### Form Builder

- As a builder, I can create a new form.
- As a builder, I can add fields to a form.
- As a builder, I can set labels, placeholders, required state, and simple validation.
- As a builder, I can arrange fields in a responsive layout.
- As a builder, I can preview the form before publishing.
- As a builder, I can publish a form version.

### Records

- As a user, I can submit a published form.
- As a user, I can see records I am allowed to access.
- As a user, I can view record details.
- As an authorized user, I can edit or delete records.
- As an authorized user, I can print a record.

### Reports

- As a builder, I can define reports over records.
- As a user, I can view reports I have access to.
- As a user, I can print or export reports if allowed.

### Permissions

- As an admin, I can define who can submit forms.
- As an admin, I can define who can view records.
- As an admin, I can define who can edit records.
- As an admin, I can define who can print or export.

### Triggers Later

- As a builder, I can create a trigger that runs after a record is created or updated.
- As a builder, I can define conditions and actions.
- As an admin, I can see trigger execution logs.

## V1 Scope

- Form list
- Create/edit form
- Basic field builder
- Basic responsive layout
- Form preview
- Publish form version
- Record submission
- Record list
- Record detail
- Record edit/delete
- Basic browser printing
- Basic permissions
- Basic audit logging
- Seed/demo data

## Acceptance Criteria for V1

- A builder can create and publish a form.
- A user can submit a published form.
- Submitted values are stored in PostgreSQL.
- Records reference the correct form version.
- Authorized users can view records.
- Unauthorized users cannot access restricted records through the backend API.
- Records can be printed with browser print.
- Basic audit logs are written for create/update/delete actions.

# Seed Data Plan

## Purpose

Seed data helps developers test forms, records, permissions, and reports quickly.

Status: implemented for local development. The API runs an idempotent development startup seeder that creates demo users, roles, departments, published sample forms, permissions, records, and the saved Business Performance Sample dashboard when PostgreSQL is available and migrations have been applied. `/theme` demo data remains separate.

The seeded local demo password is:

```text
DemoUser!2026
```

## V1 Seed Data

### Users

- Demo Admin: `admin.demo@company.test`
- Demo Builder: `builder.demo@company.test`
- Demo User: `user.demo@company.test`
- Demo Viewer: `viewer.demo@company.test`

### Roles

- Admin
- Builder
- User
- Viewer

### Departments

- HR
- Finance
- Operations

### Sample Form

Employee Information Form, published as version 1 on first seed.

Fields:

- First Name
- Last Name
- Email
- Phone
- Department
- Start Date
- Employment Type
- Notes

### Sample Records

Creates 10 sample employee records across departments. The seeder uses stable record IDs and will not duplicate them when the API restarts.

## Business Performance Dashboard Sample

Development seeding also creates:

- Published `Business Performance Sample Data` form with title, category, region, priority, amount, event date, and owner name fields.
- 48 deterministic records across all 12 months of 2025, three categories, four regions, three priorities, and four operational statuses.
- Expected analytics fixtures: 48 records, total amount `207000`, average amount `4312.5`, 12 records per status, 12 records per region, January amount `6250`, and December amount `28250`.
- Published workspace-visible `Business Performance Sample` saved dashboard with four sections and ten normal analytics widgets; slug `business-performance-sample`; not shown in navigation and not the workspace default.

All IDs are deterministic. Each entity is created only when its deterministic ID does not exist. Restarts do not duplicate records, overwrite edits, republish an unpublished dashboard, or re-enable navigation. `DemoDataSeeder.SeedDevelopmentAsync` is invoked only by the Development startup path. No production data or credentials are used.

## Later Seed Data

- Sample reports
- Sample permission rules
- Sample triggers
- Sample workflow

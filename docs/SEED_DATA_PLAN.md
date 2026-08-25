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
- Published `Business Performance Sample Data` (48 records), `Operational Performance Sample Data` (72 records), and `HSE Incident Sample Data` (36 records) forms with deterministic, permissioned dashboard facts.
- Published workspace-visible `Business Performance Sample` saved dashboard with 11 sections, standard analytics widgets, bounded sample-adapter visualizations, eight filters, and template provenance version 2; slug `business-performance-sample`; not shown in navigation and not the workspace default.
- Seeding is additive and idempotent. Existing forms, records, and the fixed dashboard identifier are never overwritten; an existing version-1 dashboard remains, while users can instantiate version 2 from the gallery.

All IDs are deterministic. Each entity is created only when its deterministic ID does not exist. Restarts do not duplicate records, overwrite edits, republish an unpublished dashboard, or re-enable navigation. `DemoDataSeeder.SeedDevelopmentAsync` is invoked only by the Development startup path. No production data or credentials are used.

### Operations Performance Dashboard Sample

Development seeding also creates the focused Operations sample:

- Published workspace-visible `Operations Performance Sample` saved dashboard with fixed slug `operations-performance-sample`.
- The dashboard is seeded from the environment-neutral `operations-performance` template, template provenance version 2, with seven sections, 24 widgets, five targeted filters, and one permissioned, report-capable Operations source slot. Eleven module analytics widgets carry fixed `module` filters for Loss, Production, Engineering, Supply Chain, and QA/QC; Overview, Trends, and Records remain broad or shared-filter driven.
- The dashboard is published with an immutable snapshot. Record-backed widgets use the existing permission-filtered analytics engine; illustrative adapter values are labeled as illustrative rather than live calculations.
- Seeding is additive and deterministic. The fixed dashboard ID is created only when absent and never overwrites existing edits, publication state, or archive state.

## Later Seed Data

- Sample reports
- Sample permission rules
- Sample triggers
- Sample workflow

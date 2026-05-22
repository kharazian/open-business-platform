# Seed Data Plan

## Purpose

Seed data helps developers test forms, records, permissions, and reports quickly.

Status: implemented for V1 local development. The API runs an idempotent development startup seeder that creates demo users, roles, departments, a published Employee Information Form, form permissions, and sample records when PostgreSQL is available and migrations have been applied. Frontend sample data for dashboard, reports, and `/theme` demos still lives in `src/app/src/lib/sampleData.ts` and `src/app/src/theme/mockData.ts`.

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

## Later Seed Data

- Sample reports
- Sample permission rules
- Sample triggers
- Sample workflow

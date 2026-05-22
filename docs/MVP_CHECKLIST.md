# MVP Checklist

Use this checklist to track V1 delivery. Current skeleton work now includes setup, database/auth foundations, Users & Access, persisted form list/create, backend-owned field-builder UI, form publishing, authenticated record submission, record list/detail, record edit/delete, V1 record permissions, browser print, and audit logs for core form/record actions.

## V1 Must Have

### Setup

- [x] Project inventory complete
- [x] Real frontend/backend commands documented
- [x] Database approach confirmed
- [x] Auth approach confirmed

### Forms

- [x] Core V1 form schema/types shared between frontend and backend
- [x] Form schema and record value validation helpers
- [x] Form list
- [x] Create form
- [ ] Edit form draft
- [x] Add/edit/delete fields in backend-owned draft builder
- [x] Basic field settings in backend-owned draft builder
- [x] Responsive layout settings
- [x] Form preview
- [x] Publish form version

### Records

- [x] Submit published form
- [x] Validate submitted values
- [x] Store record with form version
- [x] Record list
- [x] Record detail
- [x] Edit record
- [x] Delete/soft delete record

### Permissions

- [x] Basic roles
- [x] Submit permission
- [x] View records permission
- [x] Edit records permission
- [x] Backend permission checks for auth, users, roles, dashboard, and forms list/create
- [x] Backend permission checks for record submit
- [x] Backend permission checks for record view/edit/delete

### Printing

- [x] Print record detail
- [x] Print record list
- [x] Basic print CSS

### Audit

- [x] Record created log
- [x] Record updated log
- [x] Record deleted log
- [x] Form published log

### Quality

- [x] Backend build passes
- [x] Frontend build passes
- [x] Core tests pass
- [ ] Seed data exists

Note: the current code declares Node.js `>=20.19.0`. `npm test`, `npm run build`, the backend harness, and `dotnet build` passed with Node.js `v24.14.1` and .NET SDK `10.0.107`.

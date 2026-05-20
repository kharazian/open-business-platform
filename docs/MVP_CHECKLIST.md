# MVP Checklist

Use this checklist to track V1 delivery. Current skeleton work now includes setup, database/auth foundations, Users & Access, persisted form list/create, and a local field-builder UI. Record workflows are still pending.

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
- [x] Add/edit/delete fields in local frontend draft builder
- [x] Basic field settings in local frontend draft builder
- [ ] Responsive layout settings
- [ ] Form preview
- [ ] Publish form version

### Records

- [ ] Submit published form
- [ ] Validate submitted values
- [ ] Store record with form version
- [ ] Record list
- [ ] Record detail
- [ ] Edit record
- [ ] Delete/soft delete record

### Permissions

- [x] Basic roles
- [ ] Submit permission
- [ ] View records permission
- [ ] Edit records permission
- [x] Backend permission checks for auth, users, roles, dashboard, and forms list/create
- [ ] Backend permission checks for record submit/view/edit/delete

### Printing

- [ ] Print record detail
- [ ] Print record list
- [ ] Basic print CSS

### Audit

- [ ] Record created log
- [ ] Record updated log
- [ ] Record deleted log
- [ ] Form published log

### Quality

- [x] Backend build passes
- [x] Frontend build passes
- [x] Core tests pass
- [ ] Seed data exists

Note: the current code declares Node.js `>=20.19.0`. `npm test`, `npm run build`, the backend harness, and `dotnet build` passed with Node.js `v24.14.1` and .NET SDK `10.0.107`.

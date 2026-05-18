# MVP Checklist

Use this checklist to track V1 delivery. Current skeleton work counts only for setup and UI foundation, not for real form/record product features.

## V1 Must Have

### Setup

- [x] Project inventory complete
- [x] Real frontend/backend commands documented
- [x] Database approach confirmed
- [ ] Auth approach confirmed

### Forms

- [x] Core V1 form schema/types shared between frontend and backend
- [x] Form schema and record value validation helpers
- [ ] Form list
- [ ] Create form
- [ ] Edit form draft
- [ ] Add/edit/delete fields
- [ ] Basic field settings
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

- [ ] Basic roles
- [ ] Submit permission
- [ ] View records permission
- [ ] Edit records permission
- [ ] Backend permission checks

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

Note: the current code declares Node.js `>=20.19.0`. `npm test`, `npm run build`, and `dotnet build` passed with Node.js `v22.22.2` and .NET SDK `10.0.107`.

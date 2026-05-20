# Seed Data Plan

## Purpose

Seed data helps developers test forms, records, permissions, and reports quickly.

Status: planned. The current skeleton has frontend sample data for dashboard, reports, and `/theme` demos in `src/app/src/lib/sampleData.ts` and `src/app/src/theme/mockData.ts`. Forms and Users & Access now use API-backed flows where implemented, but backend/database seed data has not been implemented yet.

## V1 Seed Data

### Users

- Admin User
- Builder User
- Normal User
- Viewer User

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

Employee Information Form

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

Create at least 10 sample employee records across departments.

## Later Seed Data

- Sample reports
- Sample permission rules
- Sample triggers
- Sample workflow

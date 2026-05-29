# Product Brief

## Product

Open Business Platform is a modular low-code platform for building internal business applications.

The first product focus is a business form platform.

## Problem

Organizations need internal tools for forms, approvals, records, reports, printing, permissions, notifications, and audit logs. Building each tool from scratch repeats the same foundation and creates inconsistent systems.

## Solution

Provide one modular platform where teams can:

- Build responsive forms
- Publish immutable form versions
- Collect and manage records
- View records and reports
- Control access by user, role, group, department, ownership, and later custom rules
- Print records and lists
- Audit sensitive actions
- Add triggers and workflows in later versions

## Engine North Star

The platform should grow into cooperating engines, not one giant builder:

1. The form engine defines fields, layout, validation, draft editing, published versions, and the screens used to open, edit, and view form details.
2. The record engine creates, opens, edits, displays, and prints records while preserving the exact form version used when each record was submitted.
3. The report engine shows form records in table format, with saved columns, filters, search, sorting, pagination, export, and permissions.
4. The print engine supports clean single-record printing and report table printing first, then PDF and custom templates later.
5. The validation/rule engine starts with field validation and later supports conditional business rules.
6. The trigger engine starts work from events such as record creation or record changes, and later from schedules or incoming webhooks.
7. The workflow engine coordinates multi-step processes, approvals, transitions, assignments, and history.
8. The action engine gives triggers and workflows safe, auditable actions such as creating/updating records, sending email, calling APIs/webhooks, generating documents later, and starting workflows.

Reachability matters: build the data spine first, then rules, then automation. The near-term path is reports and printing over reliable form/record data; validation rules, event triggers, workflows, scheduled triggers, and broader integrations should come after that foundation is stable.

## V1 Outcome

V1 proves the core loop:

1. Create a form.
2. Add fields and responsive layout.
3. Publish a form version.
4. Submit records.
5. View, edit, delete, and print records with basic permissions.
6. Write audit logs.

Status: complete for the current repository. V1 is now the baseline for future V2+ work.

## Non-Goals For V1

- Microservices
- Dynamic plugin loading
- Native Federation
- Full workflow builder
- Full report designer
- PDF template designer
- Advanced dashboards
- XYFlow-based form layout

## Product Principle

Start simple, keep the architecture modular, and add advanced platform capabilities only when the foundation is stable.

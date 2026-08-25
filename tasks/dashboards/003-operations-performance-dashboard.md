# Operations Performance Dashboard

## Goal

Provide a focused reusable Operations template and a separate published development sample using the existing Operations form and dashboard platform.

## Acceptance

- [x] The gallery exposes the environment-neutral `operations-performance` version-1 template.
- [x] The template contains seven sections, 24 widgets, five targeted filters, and one report-capable Operations source slot.
- [x] Record-backed widgets use the existing analytics engine and illustrative adapters are labeled.
- [x] Development seeding publishes the workspace-visible `operations-performance-sample` dashboard with an immutable snapshot.
- [x] The fixed dashboard ID is additive and never overwrites edits, publication state, or archive state.
- [x] Existing dashboard permissions, backend validation, hidden-field protection, and builder lifecycle remain unchanged.
- [x] Focused frontend/backend tests and documentation are updated.

## Boundaries

No Operations module, cross-form join, static per-widget query filter, database migration, or chart dependency is introduced.

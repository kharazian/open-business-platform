# Multi-Value Fixed Widget Filters

## Goal

Let dashboard authors constrain one analytics widget to several declared choice values without creating duplicate filters for the same field.

## Acceptance

- [x] Declared choice options render as accessible checkbox controls in the widget properties drawer.
- [x] Authors can select or clear several values without mutating the saved widget until Apply is used.
- [x] At least one value is required before Apply is enabled.
- [x] Selected values use OR semantics within one field; separate fixed-filter fields retain AND semantics.
- [x] The existing 20-value request bound, declared-option validation, fixed-field precedence, and permission-scoped execution remain enforced.
- [x] Focused frontend and backend tests cover immutable toggling, validation, and aggregate results.

## Boundaries

Options come only from permitted form schema metadata. The editor does not discover record values, add arbitrary predicates, alter dashboard-wide filters, or weaken backend authorization and hidden-field checks.

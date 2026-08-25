# KPI Conditional Formatting

## Goal

Let dashboard authors communicate KPI health with bounded, value-driven semantic colors while keeping analytics and permissions unchanged.

## Acceptance

- [x] KPI appearance stores an optional enabled state and at most five ordered rules.
- [x] Rules support `>`, `>=`, `<`, `<=`, and `=` comparisons over bounded numeric thresholds.
- [x] Each rule selects an allowlisted semantic accent; the first match wins and the static card accent is the fallback.
- [x] The properties drawer provides enable, add, edit, reorder, and remove controls with useful starter rules.
- [x] Live preview and published viewer evaluate the same primary summary value.
- [x] Changing away from KPI disables hidden conditional formatting.
- [x] Widget, section, and template cloning deep-copy nested rules.
- [x] Backend validation rejects invalid IDs, duplicates, operators, colors, bounds, excessive rules, empty enabled rules, and non-KPI use.
- [x] Focused frontend/backend tests and browser verification cover evaluation and authoring.

## Boundaries

Conditional formatting is presentation-only. It does not change analytics requests, execute formulas, mutate records, bypass permissions, or apply to adapter and non-KPI widgets.

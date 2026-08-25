# KPI Targets and Variance

## Goal

Let dashboard authors compare a KPI's live actual value with a saved target without changing analytics execution.

## Acceptance

- [x] KPI appearance stores an optional bounded numeric target and a required 1-80 character label.
- [x] The properties drawer provides target enable, value, and label controls.
- [x] The live preview and published viewer show the target using the KPI's localized number format.
- [x] Nonzero targets show a one-decimal percentage above, below, or on-target result.
- [x] Zero targets use an absolute formatted difference instead of dividing by zero.
- [x] Changing away from KPI disables hidden target configuration.
- [x] Appearance cloning deep-copies target configuration.
- [x] Backend validation rejects out-of-range values, blank or oversized labels, and non-KPI use.
- [x] Focused tests and browser verification cover authoring, calculation, validation, and rendering.

## Boundaries

Targets and variance are presentation-only. They do not change analytics requests, conditional-rule order, records, permissions, or source values.

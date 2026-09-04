# Dashboard Display Units

## Goal

Let dashboard authors present large values compactly without changing the permission-filtered analytics result or storing formatter code.

## Acceptance

- [x] Appearance supports bounded `none`, `auto`, `thousands`, `millions`, and `billions` display units.
- [x] Existing widgets default to unscaled values.
- [x] Automatic units use locale-aware compact notation; fixed units use readable K/M/B suffixes.
- [x] Number, currency, percent, and count values share display-unit behavior.
- [x] Decimal precision remains bounded from zero through four places.
- [x] KPI values, targets, comparisons, chart axes, labels, legends, and tooltips reuse the shared formatter.
- [x] The editor explains that scaling is presentation-only.
- [x] Backend validation rejects unknown unit settings.
- [x] Focused frontend/backend tests, production builds, bundle checks, and browser verification cover the feature.

## Boundaries

Display units scale only rendered text. They do not change stored records, analytics requests or responses, sorting, thresholds, reference values, permissions, or database schema. Arbitrary formatter functions remain unsupported; bounded text affixes are defined separately by Task 023.

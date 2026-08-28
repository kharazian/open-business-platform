# Chart Reference and Target Lines

## Goal

Let dashboard authors add clear goal, standard, warning, or limit lines to data-driven Bar, Line, and Area charts without changing analytics execution.

## Acceptance

- [x] A cartesian chart supports up to four reference lines.
- [x] Each line has a required label, nonnegative numeric value, semantic color, solid/dashed/dotted style, and left/right axis.
- [x] Frontend and backend validation reject excessive, duplicate, malformed, unsupported, KPI, table, pie, and donut configurations.
- [x] Reference values participate in their selected axis maximum so lines above current data remain visible.
- [x] Visible labels and native SVG titles use the widget's localized number formatting.
- [x] The properties editor provides add, edit, remove, reset, validation, and live preview behavior.
- [x] Switching to an incompatible visualization removes reference-line metadata rather than saving an invisible setting.
- [x] Published viewers reuse the same renderer as the editor preview.
- [x] Focused frontend/backend tests, production builds, bundle checks, and browser verification cover the feature.

## Boundaries

Reference lines are bounded presentation metadata stored in the existing dashboard JSON. They do not change queries, calculate formulas, grant permissions, write target records, introduce cross-form joins, add a chart dependency, or require a database migration.

# Horizontal Bar Charts

## Goal

Let dashboard authors turn category Bar charts horizontally for clearer comparisons and longer category labels without rebuilding metrics or adding another visualization engine.

## Acceptance

- [x] Category breakdown Bar charts offer Vertical and Horizontal directions.
- [x] Horizontal direction supports grouped, stacked, and 100% stacked layouts.
- [x] Horizontal charts require one or more Bar series on a shared axis.
- [x] Trend, KPI, table, circular, mixed-display, and mixed-axis configurations remain vertical and are rejected by frontend/backend validation if malformed.
- [x] Category labels, localized values, legends, data labels, reference lines, tooltips, selection, and keyboard drill-through work in the horizontal renderer.
- [x] Existing and missing appearance metadata default to Vertical for backward compatibility.
- [x] Editor previews and published viewers use the same renderer.
- [x] Focused frontend/backend tests, production builds, bundle checks, and browser verification cover the feature.

## Boundaries

Bar direction is bounded presentation metadata in the existing dashboard JSON. It does not transpose analytics queries, change source records or permissions, introduce cross-form joins, require a database migration, or add a chart dependency.

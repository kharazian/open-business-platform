# Stacked Bar Charts

## Goal

Let dashboard authors compare cumulative totals or percentage composition with stacked Bar series while preserving grouped bars as the default.

## Acceptance

- [x] Breakdown and trend charts offer Grouped, Stacked, and 100% stacked Bar layouts.
- [x] Stacking requires two to four Bar series on one shared axis; incompatible edits return the chart to Grouped.
- [x] Normal stacked scaling uses the largest category/date total, while 100% stacked scaling is fixed at 100%.
- [x] 100% stacked labels show percentage composition and SVG tooltips retain both actual values and percentages.
- [x] Negative source values produce a clear unavailable state instead of a misleading stack.
- [x] 100% stacked charts reject and remove reference lines because reference values use an absolute scale.
- [x] Frontend and backend validation reject unsupported modes, widget types, series counts, mixed display types, and mixed axes.
- [x] Editor previews and published viewers use the same chart renderer.
- [x] Focused frontend/backend tests, production builds, bundle checks, and browser verification cover the feature.

## Boundaries

Bar layout is bounded presentation metadata stored in the existing dashboard JSON. It does not change analytics queries, source records, permissions, series metrics, or database schema, and it adds no chart dependency.

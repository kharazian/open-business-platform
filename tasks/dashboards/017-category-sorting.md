# Dashboard Category Sorting

## Goal

Let dashboard authors order breakdown categories by their returned value or readable label without changing permission-filtered analytics execution.

## Acceptance

- [x] Breakdown charts offer Source order, Value highest/lowest first, and Label A-Z/Z-A.
- [x] Multi-series value sorting uses the combined returned value for each category.
- [x] Vertical, horizontal, grouped, stacked, 100% stacked, pie, and donut renderers use the same deterministic order.
- [x] Date trends preserve chronological source order; KPI and table widgets reject non-default category sorting.
- [x] Existing and missing appearance metadata default to Source order.
- [x] Switching to an incompatible visualization resets category ordering instead of saving an invisible setting.
- [x] Frontend and backend validation reject unsupported values and widget types.
- [x] Editor previews and published viewers share the ordered renderers.
- [x] Focused frontend/backend tests, production builds, bundle checks, and browser verification cover the feature.

## Boundaries

Category order is bounded presentation metadata stored in the existing dashboard JSON. Sorting applies only to the already-returned, bounded category set. It does not change analytics queries, record permissions, source report ordering, limits, stored records, or database schema and adds no dependency.

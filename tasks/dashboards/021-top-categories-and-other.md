# Dashboard Top Categories and Other Grouping

## Goal

Let dashboard authors focus a breakdown on its most useful returned categories while optionally preserving the remainder as an honest aggregate.

## Acceptance

- [x] Breakdown charts support an optional visible-category limit from 1 through 12.
- [x] Authors can optionally sum remaining returned categories into a final `Other` point.
- [x] `Other` grouping is restricted to additive count/sum series; average series retain Top-N without a mathematically invalid combined average.
- [x] Multi-series charts compute the shared Top-N order from combined series values and aggregate each series independently.
- [x] Vertical, horizontal, stacked, line/area, pie, and donut presentations share the reduced category set.
- [x] Axis scaling, percentages, labels, legends, tooltips, and chart geometry use the displayed Top-N plus Other values.
- [x] Existing widgets retain the prior maximum of 12 displayed categories with no Other point.
- [x] Trend, KPI, and table widgets reject or normalize invisible category-limit settings.
- [x] `Other` is marked as an aggregate and never creates a false single-category drill-through filter.
- [x] The editor explains that Top-N and Other operate within the permission-filtered result limit returned by the API.
- [x] Focused frontend/backend tests, production builds, bundle checks, and browser verification cover the feature.

## Boundaries

Top-N is bounded presentation over the already-authorized analytics response. `Other` sums only additive count/sum categories present in that bounded response, never hidden or unreturned source groups; it is unavailable when any series is an average. The feature does not change analytics execution, permissions, source records, or database schema and adds no chart dependency.

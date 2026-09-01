# Dashboard Data Label Properties

## Goal

Let dashboard authors choose useful label content and placement without introducing misleading percentages or unreadable circular-chart collisions.

## Acceptance

- [x] Breakdown and trend charts can show automatic or explicit value labels.
- [x] Pie, donut, and 100% stacked charts additionally support percentage or combined value-and-percentage labels.
- [x] Cartesian labels support automatic, inside, and outside placement.
- [x] Automatic placement preserves the prior behavior: grouped Bars and line/area points label outside, stacked segments label inside, circular charts show percentages in their label list.
- [x] Pie and donut labels remain in the collision-safe label list and reject unsupported inside/outside placement.
- [x] Vertical and horizontal grouped/stacked renderers share the same label content contract and localized value formatting.
- [x] Incompatible content or placement resets safely when the visualization, display style, or Bar layout changes.
- [x] Existing and missing appearance metadata resolve to automatic content and placement.
- [x] Backend validation rejects unsupported values, misleading percentage use, and invisible custom settings on non-chart widgets.
- [x] Focused frontend/backend tests, production builds, bundle checks, and browser verification cover the feature.

## Boundaries

Data-label settings are bounded presentation metadata inside existing dashboard JSON. Percentages are derived only from already-returned circular proportions or 100% stack totals. The feature does not change analytics execution, permissions, source records, or database schema and adds no chart dependency.

# Dashboard Legend Properties

## Goal

Let dashboard authors place chart legends where they best support comparison while preserving readable, responsive widgets.

## Acceptance

- [x] Breakdown and trend charts support top, bottom, left, and right legend positions.
- [x] Top remains the backward-compatible default for existing and missing appearance metadata.
- [x] The same placement contract applies to cartesian, pie, and donut chart legends.
- [x] Side legends use a bounded scroll area and truncate long labels while exposing the full label as native hover text.
- [x] Left and right legends stack above the chart on narrow screens.
- [x] Turning off the legend removes cartesian legend content without changing its saved position.
- [x] Non-chart widgets reject or normalize invisible non-default legend positions.
- [x] The editor preview and published viewer share the same responsive renderer.
- [x] Focused frontend/backend tests, production builds, bundle checks, and browser verification cover the feature.

## Boundaries

Legend position is bounded presentation metadata inside existing dashboard JSON. It does not change analytics execution, returned values, permissions, source records, or database schema and adds no chart dependency.

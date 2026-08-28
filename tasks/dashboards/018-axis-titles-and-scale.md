# Chart Axis Titles and Scale

## Goal

Let dashboard authors explain chart units and optionally focus an axis with a bounded manual maximum while keeping Bar chart baselines honest.

## Acceptance

- [x] Cartesian charts support optional left and active right axis titles up to 80 characters.
- [x] Each active axis supports automatic scaling or a positive bounded manual maximum.
- [x] Bar charts retain a zero baseline; the feature does not introduce misleading truncated Bar baselines.
- [x] Manual maxima clamp SVG geometry and show a readable warning when returned values, stacked totals, or reference lines exceed the configured scale.
- [x] 100% stacked charts retain their fixed 100% maximum and remove incompatible manual maxima.
- [x] Inactive axes and KPI, table, pie, and donut widgets reject or remove invisible axis settings.
- [x] Existing and missing appearance metadata use empty titles and automatic scaling.
- [x] Vertical, horizontal, editor-preview, and published-viewer charts use the same settings.
- [x] Focused frontend/backend tests, production builds, bundle checks, and browser verification cover the feature.

## Boundaries

Axis settings are bounded presentation metadata in existing dashboard JSON. They do not change analytics execution, returned values, permissions, source records, or database schema and add no chart dependency. Manual maxima may hide magnitude by design, so the renderer always exposes clipping explicitly.

# V7 Task Index

V7 deepens the V2 dashboard foundation into richer analytics while keeping dashboards separate from reports, records, triggers, workflows, and print/PDF modules.

## Start Packet

Before implementing V7, read `docs/V7_START_HERE.md`, then follow the design and implementation plan linked from V7 task 001.

## Recommended Execution Order

1. `001-advanced-dashboard-analytics-foundation.md` - backend analytics contracts and permission-checked summary/trend/breakdown primitives for dashboard widgets.
2. `002-dashboard-widget-builder-upgrade.md` - frontend controls for richer analytics widgets over the new backend contracts.
3. `003-dashboard-viewer-refresh.md` - dashboard viewer improvements for dense scanning, refreshed widgets, empty states, and layout polish.
4. `004-dashboard-sharing-and-defaults.md` - saved dashboard defaults and sharing/visibility rules, after workspace ownership decisions are ready.

## Scope Rules

- Keep dashboards as their own module.
- Reuse existing form/report permission checks and field-hidden rules.
- Do not expose hidden field values through chart, table, or analytics widgets.
- Keep reports and dashboards separate: reports define reusable tabular views; dashboards compose analytics widgets.
- Avoid adding a large charting dependency until the lightweight renderers cannot meet the task requirements.
- Defer workspace ownership and cross-workspace sharing until the workspace module exists.
- Defer custom BI expressions, cross-form joins, and arbitrary SQL.

# Dashboard Tooltip Properties

## Goal

Let dashboard authors control native value hints without weakening accessible interaction labels or accepting arbitrary tooltip markup.

## Acceptance

- [x] Appearance can show or hide native value tooltips and defaults to showing them.
- [x] Tooltip content is bounded to automatic, value-only, category-and-value, or series-category-and-value modes.
- [x] Automatic mode preserves the prior contextual tooltip wording for existing widgets.
- [x] Bar, stacked, horizontal, line, area, pie, donut, reference-line, legacy-series, and KPI presentations use the shared tooltip settings.
- [x] Line and area points expose hover targets even when no drill-through action is configured.
- [x] Percentage charts retain their derived percentage alongside the formatted raw value.
- [x] Hiding tooltips does not remove visible values or full keyboard/screen-reader interaction labels.
- [x] Backend and frontend validation reject unknown content modes.
- [x] Focused tests, production builds, bundle checks, and browser verification cover the feature.

## Boundaries

Tooltips use native escaped text only. The settings do not alter analytics, records, filters, permissions, selection, or drill-through. HTML, Markdown, images, arbitrary fields, expressions, formatter functions, and record-level hidden data remain unsupported.

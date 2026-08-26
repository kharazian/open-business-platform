# Data-Driven Pie and Donut Charts

## Goal

Let dashboard authors present a permission-filtered category breakdown as a Pie or Donut chart through the normal analytics, preview, save, publish, and viewer paths.

## Acceptance

- [x] Pie and Donut are available in the series Display property for a compatible category breakdown.
- [x] Circular displays require exactly one breakdown series in frontend and backend validation.
- [x] Changing away from a breakdown safely resets an incompatible circular display to Bar.
- [x] Slice colors follow the bounded chart palette and selected starting color.
- [x] Legend values and optional percentage labels use localized widget formatting.
- [x] Slices and legend rows expose tooltips, keyboard focus, selection, and existing typed drill-through.
- [x] Donut charts show the formatted aggregate total.
- [x] Empty, zero-only, and negative results produce an explanatory empty state instead of misleading proportions.
- [x] Focused frontend/backend tests, production build, and live browser verification cover the feature.

## Boundaries

This is a presentation extension over the existing permission-scoped breakdown response. It adds no query engine, cross-form join, arbitrary formula, chart dependency, or database migration. Circular charts do not support trends or multiple series.

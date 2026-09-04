# Dashboard Value Prefix and Suffix

## Goal

Let dashboard authors add a short, safe qualifier or unit around formatted numeric values without introducing formatter code.

## Acceptance

- [x] Appearance supports an optional prefix of at most 12 printable characters.
- [x] Appearance supports an optional suffix of at most 24 printable characters.
- [x] Existing widgets default to empty affixes and preserve their current rendering.
- [x] Prefixes wrap the localized number before currency/display-unit text and suffixes follow the complete formatted value.
- [x] KPI values, targets, comparisons, count metrics, chart axes, labels, legends, and tooltips share the same affix behavior.
- [x] The editor provides examples and explains how to include spacing before a unit.
- [x] Backend and frontend validation reject oversized values and control characters.
- [x] Focused tests, production builds, bundle checks, and browser verification cover the feature.

## Boundaries

Affixes are escaped React text and presentation-only. They do not alter analytics, thresholds, sorting, stored record values, permissions, or database schema. HTML, CSS, JavaScript, arbitrary formatter functions, and per-series affixes remain unsupported.

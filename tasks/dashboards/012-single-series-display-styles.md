# Single-Series Display Styles

## Goal

Make the existing Bar, Line, and Area series property visibly affect charts when an analytics widget has only one configured series.

## Acceptance

- [x] A configured single breakdown or trend series uses the shared SVG chart renderer.
- [x] Bar, line, and area selections produce their corresponding marks in live preview and published viewer.
- [x] Series color, axis, legend, gridline, data-label, interaction, and localized formatting behavior remains shared with multi-series charts.
- [x] KPI summaries and tables retain their specialized renderers.
- [x] Empty configured series retain the existing empty state.
- [x] Legacy preview payloads without `dataSeries` retain the horizontal-bar fallback.
- [x] Focused tests and browser verification cover renderer routing and author-visible style changes.

## Boundaries

This task changes frontend rendering only. It does not alter analytics queries, saved data, permissions, series validation, or API response contracts.

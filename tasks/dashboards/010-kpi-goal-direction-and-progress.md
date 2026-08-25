# KPI Goal Direction and Progress

## Goal

Make KPI target results meaningful for both growth metrics and reduction metrics, with a readable outcome and bounded progress indicator.

## Acceptance

- [x] KPI target settings store `higher_is_better` or `lower_is_better`, defaulting legacy targets to higher-is-better.
- [x] The target editor provides a clear goal-direction control.
- [x] Target evaluation returns favorable, on-target, or needs-attention independently from above/below variance.
- [x] The preview and viewer display the outcome as text, not color alone.
- [x] Nonnegative actuals and targets show accessible progress bounded to 0-100.
- [x] Negative-value cases omit progress instead of presenting a misleading ratio.
- [x] Backend validation rejects unsupported directions.
- [x] Focused tests and browser verification cover higher-is-better, lower-is-better, zero-target, and rendering behavior.

## Boundaries

Goal direction, outcome, and progress are presentation-only. They do not change source values, analytics requests, conditional rules, records, or permissions.

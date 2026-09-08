# Dashboard Trend Date Granularity

## Goal

Let dashboard authors group date trends into explicit calendar periods instead of always grouping by an exact date.

## Acceptance

- [x] Trend widgets save a `day`, `week`, `month`, `quarter`, or `year` granularity.
- [x] Existing widgets without the setting retain daily grouping.
- [x] The widget properties drawer provides a clear trend-only granularity selector.
- [x] Datetime values are converted to the workspace timezone before bucketing.
- [x] Date-only values retain their declared calendar date.
- [x] Weekly buckets honor the workspace first-day-of-week setting.
- [x] Bucket keys remain stable and labels identify their calendar period.
- [x] Limits apply after records are grouped into buckets, retaining the most recent periods.
- [x] Invalid granularities are rejected by frontend and backend validation.
- [x] Dashboard templates use monthly grouping for monthly sample trends.
- [x] Focused tests, production builds, bundle checks, and browser verification cover the feature.

## Boundaries

This task changes trend aggregation only. It does not fill missing calendar periods, define null-value behavior, localize server-generated bucket labels, or expand drill-through query semantics for aggregated date ranges; those remain separate follow-up tasks.

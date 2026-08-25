# KPI Period Comparison

## Goal

Let KPI authors compare the current bounded date window with the immediately preceding equal window without exposing or calculating record data in the browser.

## Acceptance

- [x] KPI config stores an optional reportable date field and a 7, 30, or 90-day period preset.
- [x] The properties drawer provides enable, date-field, and period controls.
- [x] Backend validation rejects non-KPI use, non-date fields, hidden fields, and unsupported periods.
- [x] The backend evaluates current and previous UTC windows over the same permission-scoped records and all other filters.
- [x] Comparison owns only its configured date-field filter and does not weaken saved-report or authorization constraints.
- [x] The response returns aggregate comparison metadata without source records.
- [x] Preview and viewer show readable up/down/unchanged change plus the localized previous value.
- [x] Zero previous values avoid division by zero and use direction-only text.
- [x] Widget, section, and template cloning deep-copy comparison settings; source changes clear stale settings.
- [x] Automated and browser verification cover validation, authoring, aggregation, and rendering.

## Boundaries

Comparison is an analytics query option, not a browser-side calculation. It does not expose records, bypass filters or permissions, join forms, or predict future values.

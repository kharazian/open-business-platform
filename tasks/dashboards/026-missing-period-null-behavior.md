# Dashboard Missing-Period and Null Behavior

## Goal

Make no-data handling explicit so dashboard readers can distinguish missing measurements from real zero values.

## Acceptance

- [x] Trend widgets can omit empty periods, show them as zero, or retain them as visual gaps.
- [x] Existing widgets default to omitting empty periods.
- [x] Filled period ranges are bounded by the configured result limit.
- [x] Gap points carry explicit `isMissing` metadata and are not rendered or selectable as measurements.
- [x] All-missing chart series show an informative no-data state.
- [x] Numeric metrics can ignore null inputs or treat them as zero.
- [x] Ignored nulls do not enter sum or average calculations; zero-valued nulls enter average denominators.
- [x] Record count remains independent of numeric-null settings.
- [x] KPI cards display `No data` instead of a misleading zero when no usable numeric value exists.
- [x] Unknown behaviors are rejected in frontend and backend validation.
- [x] Tests and documentation cover sparse periods, gap metadata, null averages, and safe defaults.

## Boundaries

The engine does not interpolate values, invent periods beyond the first/latest dated source bucket, or allow gap points to trigger drill-through. Missing points retain a numeric transport value of zero for backward compatibility, but `isMissing: true` is authoritative.

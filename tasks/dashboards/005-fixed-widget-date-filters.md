# Fixed Widget Date Filters

## Goal

Complete fixed-filter authoring for reportable date and datetime fields with clear range semantics and matching frontend/backend validation.

## Acceptance

- [x] The widget properties drawer lists permitted date and datetime fields alongside declared choice fields.
- [x] Date filters expose optional inclusive-start and exclusive-end controls.
- [x] At least one date bound is required and an entered end must be after an entered start.
- [x] Date filters reject scalar `values`; choice filters reject date bounds.
- [x] Changing a fixed-filter field resets incompatible value/date state.
- [x] Backend validation independently enforces the saved date-filter contract.
- [x] Focused frontend and backend tests cover valid and reversed ranges.

## Boundaries

The editor does not discover record values, interpret relative phrases, evaluate formulas, or persist viewer-local dates. Existing source permissions, hidden-field checks, record scopes, and inclusive-start/exclusive-end analytics execution remain authoritative.

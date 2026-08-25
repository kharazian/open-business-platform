# Fixed Widget Analytics Filters

## Goal

Allow a dashboard author to permanently constrain an individual analytics widget without changing the shared dashboard filters or bypassing source permissions.

## Acceptance

- [x] Analytics widget configs persist up to eight fixed field/value or date filters.
- [x] The properties drawer provides choice-field authoring and clears fixed filters when the source changes.
- [x] The frontend request builder and backend aggregation engine merge fixed and runtime filters with fixed same-field precedence.
- [x] Backend validation rejects unknown or non-filterable fields, duplicate fields, missing criteria, invalid choice options, and excessive bounds.
- [x] Widget/template cloning deep-copies filter values.
- [x] Operations module widgets are scoped to their matching `module` value while broad sections retain normal dashboard filters.
- [x] Frontend and backend tests cover validation, precedence, cloning, seed output, and template output.

## Boundaries

Fixed filters do not grant access, discover record values, add arbitrary expressions, execute SQL, or join forms. Normal form/report permission checks, hidden-field checks, record scopes, and audit-safe dashboard lifecycle remain authoritative.

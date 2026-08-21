# Comprehensive Sample Dashboard Foundation

## Goal

Add a reusable dashboard-template contract and a comprehensive Business Performance Sample dashboard using the existing saved-dashboard, analytics, permission, and publishing paths plus a bounded sample visualization adapter.

## Scope

- Typed source-slot templates, catalog registration, capability checks, safe instantiation, generated runtime IDs, and informational provenance.
- Explicit Blank versus template creation workflow in the real dashboard builder.
- Eleven-section Business Performance Sample covering executive, financial, loss, production, engineering, supply chain, QAQC, HSE, trends/targets, records, and data-health patterns.
- Three independently permissioned form bindings, deterministic development sources, eight filters, accessible tabs, focus/refresh actions, and bounded adapter visualizations.
- Implementation and deferment evidence is maintained in `docs/COMPREHENSIVE_DASHBOARD_FEATURE_MATRIX.md`.
- Deterministic, development-only sample form, 48 records, permissions, and published saved dashboard.
- Preserve legacy saved dashboards and adapter widgets.

## Acceptance

- [x] Templates contain no environment-specific source IDs.
- [x] Instantiation validates structure/bindings and returns independent draft objects.
- [x] Template selection requires an explicit create action and permitted form binding.
- [x] The seeded sample renders through the normal saved-dashboard viewer and analytics API.
- [x] Development seeding is deterministic, additive, and never overwrites the sample after creation.
- [x] Template provenance is optional and backward compatible.
- [x] Shared date/status/category/region filters are bounded, backend validated, and applied only to compatible widgets.
- [x] Focused frontend/backend tests and documentation are updated.
- [x] The builder exposes a responsive visual layout canvas with drag handles and cross-section widget drop zones while retaining accessible fallback controls.
- [x] Existing analytics and adapter widgets can be edited through a responsive properties drawer with permitted source metadata, live preview, dirty-state handling, and explicit Apply/Cancel controls.

## Deferred follow-up

Permission-safe drill-through remains follow-up work because the current record-list route does not yet accept the dashboard filter contract. Operations and Finance template conversions remain separate tasks.

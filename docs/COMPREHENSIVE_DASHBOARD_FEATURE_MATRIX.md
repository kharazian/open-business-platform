# Comprehensive Dashboard Feature Matrix

Status: implemented and verified on 2026-08-21 unless marked deferred.

| Area | Capability | Status | Evidence / boundary |
|---|---|---|---|
| Template | Reusable template and independent draft instances | Verified | Tests cover cloning, provenance, bindings, and adapter availability. |
| Sources | Business, Operations, and HSE form bindings with optional reports | Verified | Each permitted source is loaded and validated separately; no cross-form join. |
| Navigation | 11 ordered sections with allowlisted icons and keyboard tabs | Verified | Arrow, Home, End, overflow, and responsive behavior are shared. |
| Executive | Counts, value KPIs, status mix, target attainment | Verified | Standard analytics plus bounded adapter. |
| Financial | Category/trend, delta, waterfall, heatmap | Verified | Illustrative values are labeled; no Finance module was invented. |
| Operations | Loss, Production, Engineering, Supply Chain, QAQC | Verified | Deterministic Operations form plus analytics/adapter patterns. |
| HSE | Incident count/cost/hours, location, trend, donut | Verified | Dedicated HSE sample form; no HSE domain module was invented. |
| Trends | Business/operational trends, budget comparison, diagnostics | Verified | Standard analytics and bounded visuals. |
| Records | Permission-filtered tables and accessible detail dialog | Verified | Dialog demonstrates detail UX without claiming cross-source drill-through. |
| Data health | Row counts, schema/permission state, source labels | Verified | Adapter exposes configured scalar values only. |
| Filters | Eight filters, chips, apply/reset per active tab | Verified | Filters apply only to widgets with the matching source form. |
| Refresh | Widget/tab refresh, concurrency 3, stale-response protection | Verified | Viewer bounds requests and ignores older responses. |
| Presentation | Focus mode, copy link, state handling, responsive cards | Verified | Escape exits focus mode. |
| Authoring | Section CRUD/order/icons; widget add/duplicate/drag/move/remove/resection/resize | Verified | Visual canvas provides section/widget drag handles, drop zones, and button/select fallbacks. |
| Publishing | Draft/publish/unpublish, slug, navigation, permission | Verified | Existing audited lifecycle reused. |
| Safety | 16 sections, 48 widgets, 16/section, 8 filters | Verified | Server validator and tests enforce limits. |
| Localization | Locale-aware number/date formatting | Implemented | Translation keys for sample prose remain a localization backlog item. |
| Drill-through | Module/report routes with serialized filters | Deferred | Operations/HSE are sample forms. Add typed route adapters when real modules exist. |
| URL filter state | Shareable query parameters | Deferred | Copy-link currently shares the route only; add versioned serialization first. |
| Full widget editor | Edit analytics/adapter definitions after creation | Verified | Responsive drawer edits content, layout, source/report, analytics properties, table columns, and adapter settings with capability validation and live preview. |
| Soft delete | Recoverable delete/restore | Deferred | No delete API exists; draft/publish/unpublish remain supported. |
| Server adapter registry | Per-adapter setting allowlists | Deferred | API bounds scalar settings; shared registry is required before third-party adapters. |
| Cross-source calculations | Joins/formulas across sources | Deferred | Excluded to preserve permissions and avoid arbitrary SQL. |

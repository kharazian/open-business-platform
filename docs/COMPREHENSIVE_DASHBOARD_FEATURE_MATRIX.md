# Comprehensive Dashboard Feature Matrix

Status: implemented and verified through 2026-08-24 unless marked deferred.

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
| Filters | Visual add/edit/reorder, defaults, required state, targeting, chips, apply/reset per active tab | Verified | Options come from permitted form schema or bounded author input; filters target only widgets with the matching source form. |
| Refresh | Widget/tab refresh, concurrency 3, stale-response protection | Verified | Viewer bounds requests and ignores older responses. |
| Presentation | Focus mode, copy link, state handling, responsive cards | Verified | Escape exits focus mode. |
| Authoring | Section CRUD/order/icons; widget add/duplicate/drag/move/remove/resection/resize | Verified | Visual canvas provides section/widget drag handles, drop zones, and button/select fallbacks. |
| Publishing | Draft/publish/unpublish, slug, navigation, permission | Verified | Existing audited lifecycle reused. |
| Safety | 16 sections, 48 widgets, 16/section, 8 filters | Verified | Server validator and tests enforce limits. |
| Localization | Locale-aware number/date formatting | Implemented | Translation keys for sample prose remain a localization backlog item. |
| Drill-through | Typed source-record and saved-report destinations with serialized scalar filters | Verified | Destination APIs recheck current form/report, record-scope, and field permissions; arbitrary URLs and cross-source mappings are not supported. |
| URL filter state | Shareable query parameters | Verified | Versioned `dv=1` state preserves an allowlisted active section and bounded schema-defined filter values; malformed sections, dates, values, and unsupported options are ignored. |
| Full widget editor | Edit analytics/adapter definitions after creation | Verified | Responsive drawer edits content, layout, source/report, analytics properties, table columns, and adapter settings with capability validation and live preview. |
| Multi-series charts | Add, remove, reorder, label, style, color, and axis for up to four metrics | Verified | Every metric is validated and executed against the same permission-scoped source rows; combo charts use independent left/right scales. |
| Chart appearance | Palette, legend, labels, gridlines, card accent, and localized number formats | Verified | Bounded theme/cool/warm/monochrome presets and reset-to-theme controls are saved per analytics widget; presentation settings never alter source data. |
| Add-widget wizard | Source-first guided creation, searchable gallery, recommendations, preview, and recent choices | Verified | Recommendations use already-loaded reportable field capabilities; the final sample preview and add action reuse the permission-checked analytics endpoint. |
| Canvas productivity | Undo/redo, multi-select, bulk actions, section duplication/collapse, density, and zoom | Verified | Draft-only history is bounded to 30 snapshots; bulk and duplicate operations preserve the 16-section/48-widget/16-per-section server limits. |
| Filter authoring | Source/field/control editor, option preview, default values, required/optional state, and per-widget targeting | Verified | Backend revalidates filter ids, field compatibility, defaults, options, and same-source targets before persistence. |
| Chart interactions | Keyboard/click selection, native SVG tooltips, table-row selection, optional new-tab navigation | Verified | Point filters derive only from the configured grouping/date field; table rows use typed record-detail routes. |
| Dashboard library | Search, lifecycle badges, duplication, and archive | Verified | Published directory search is client-bounded; duplication creates an independent draft; archive uses permission-protected audited soft deletion and immediately removes live exposure. |
| Sharing | Workspace, specific user/role/group audience, private, and unlisted links | Verified | Subject IDs are manager-only metadata; published snapshots enforce OR membership checks and unauthorized reads return 404. |
| Server adapter registry | Built-in adapter visualization and setting allowlists | Verified | `sample-dashboard` IDs/keys are backend allowlisted; unknown built-in config is rejected while safe scalar legacy adapter definitions remain backward compatible. |
| Archive restore | User-facing recovery of archived dashboards | Deferred | Archive is a confirmed audited soft delete; a workspace-wide recycle-bin policy is required before exposing restore and retention semantics. |
| Cross-source calculations | Joins/formulas across sources | Deferred | Excluded to preserve permissions and avoid arbitrary SQL. |

# Dashboard Builder UX Roadmap

Implement and verify these increments in order. Each increment must preserve backend permissions, saved-definition bounds, keyboard fallbacks, and legacy dashboards.

1. **Visual layout canvas — implemented**
   - Drag handles for sections and widgets
   - Reorder widgets and move them between section drop zones
   - Drop-target highlighting, widget counts, save notice, arrow/select fallback
   - Responsive canvas with no page-level mobile overflow
2. **Widget properties drawer — implemented**
   - Select a card to edit title, subtitle, section, width, source, report, type, aggregation, grouping, date field, row limit, and table columns
   - Live preview, Apply/Cancel, dirty-state warning, validation beside the affected control
3. **Multi-series analytics contract — implemented**
   - Bounded series collection with label, metric, aggregation, field, color, axis, and display type
   - Backend validation/execution with field permissions checked for every series
   - Bar, line, area, and combo rendering with independent left/right scales, without arbitrary formulas or SQL
4. **Appearance and color controls — implemented**
   - Theme-safe palette presets, per-series semantic colors, legend, labels, gridlines, card accent, number/currency/percent formatting
   - Bounded accessible color presets and reset-to-theme behavior
5. **Improved add-widget experience — implemented**
   - Searchable visualization gallery, recommended chart hints, source-first wizard, sample preview, recent choices
   - Four guided steps with field-capability validation and responsive controls
6. **Canvas productivity — implemented**
   - Undo/redo, multi-select, bulk move/resize/delete, duplicate section, collapse sections, zoom/density controls
   - Thirty-step draft history with existing dashboard bounds enforced before bulk or duplicate operations
7. **Filter authoring — implemented**
   - Add/edit/reorder filters, widget targeting, defaults, required/optional state, dependency-safe option previews
   - Eight-filter bound, compatible source/field controls, schema-only option discovery, and viewer enforcement of required values
8. **Interaction and drill-through — implemented**
   - Typed record/report destinations, permission-safe filter mapping, tooltip and selection behavior
   - Keyboard-selectable points/rows, same-source scalar filter transfer, destination authorization, and optional new-tab behavior
9. **Publishing workflow — implemented**
   - Draft/published comparison, preview mode, unsaved-change guard, revision history, restore, safer slug/navigation changes
   - Draft saves no longer mutate the live snapshot; publishing explicitly replaces live, and restore always creates a new draft revision
10. **Accessibility and quality — implemented**
    - Keyboard drag alternative, screen-reader move announcements, touch reorder controls, responsive/browser regression coverage, performance budgets
    - Reorder handles use Space/Enter pickup, arrow-key movement, and Escape/release semantics; labeled 44px touch controls move widgets within and between sections
    - Builder previews run at a maximum concurrency of four, existing 16-section/48-widget/30-history bounds are centralized, and the dashboard-builder route remains below 32 kB gzip

## Release hardening — implemented

- A real Chromium/API/PostgreSQL lifecycle suite covers preview, save, first publish, isolated post-publish draft edits, normal-viewer published-snapshot reads, manager-only revision access, republish, restore-without-live-change, unpublish, and cleanup.
- Dashboard list and ID reads project the immutable published snapshot for normal viewers; only managers receive editable draft state.
- Permission-protected audited soft deletion provides deterministic E2E cleanup without altering seeded sample dashboards.

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
3. **Multi-series analytics contract**
   - Bounded series collection with label, metric, aggregation, field, color, axis, and display type
   - Backend validation/execution with field permissions checked for every series
   - Bar, line, area, and combo rendering without arbitrary formulas or SQL
4. **Appearance and color controls**
   - Theme-safe palette presets, per-series semantic colors, legend, labels, gridlines, card accent, number/currency/percent formatting
   - Contrast checks and reset-to-theme behavior
5. **Improved add-widget experience**
   - Searchable visualization gallery, recommended chart hints, source-first wizard, sample preview, recent choices
6. **Canvas productivity**
   - Undo/redo, multi-select, bulk move/resize/delete, duplicate section, collapse sections, zoom/density controls
7. **Filter authoring**
   - Add/edit/reorder filters, widget targeting, defaults, required/optional state, dependency-safe option previews
8. **Interaction and drill-through**
   - Typed record/report destinations, permission-safe filter mapping, tooltip and selection behavior
9. **Publishing workflow**
   - Draft/published comparison, preview mode, unsaved-change guard, revision history, restore, safer slug/navigation changes
10. **Accessibility and quality**
    - Keyboard drag alternative, screen-reader move announcements, touch reorder controls, responsive/browser regression coverage, performance budgets

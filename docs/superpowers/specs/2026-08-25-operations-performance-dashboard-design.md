# Operations Performance Dashboard Design

## Context

The dashboard platform already provides:

- A typed frontend template engine and gallery.
- Saved dashboard drafts, publication snapshots, revisions, archive/recovery, and permission-aware viewing.
- Backend analytics for summary, breakdown, trend, and table widgets.
- A bounded `sample-dashboard` adapter for visualization shapes not represented by analytics widgets.
- A deterministic development form named `Operational Performance Sample Data` with 72 records.
- Existing Operations examples inside the broader Business Performance Sample dashboard.

The next slice should make Operations useful as a focused dashboard without creating a separate Operations module or a second visualization system.

## Goal

Add a reusable **Operations Performance** gallery template and a separate, deterministic, published **Operations Performance Sample** dashboard. Both use the existing Operations form, analytics engine, adapter registry, permissions, saved-dashboard APIs, and publishing lifecycle.

## Product Boundary

The approved approach is one focused template plus one independently stored published sample.

The alternatives were:

1. A gallery template only. This has less seed code but gives a new installation no ready-to-view Operations dashboard.
2. A gallery template plus published sample. This provides immediate demonstration and a reusable starting point, so it is the selected approach.
3. A custom Operations page or backend analytics subsystem. This would duplicate established dashboard behavior and is rejected.

No new database table, migration, chart dependency, route family, or Operations module is required.

## Dashboard Information Architecture

The template uses seven ordered sections:

1. **Operations Overview** — high-level actual, target, variance, and module context.
2. **Loss** — loss totals, metric composition, period movement, and standard comparison.
3. **Production** — product performance, production movement, and composition.
4. **Engineering** — equipment performance, engineering movement, and target comparison.
5. **Supply Chain** — inventory and service-level indicators.
6. **QA/QC** — release performance, quality metrics, and supporting details.
7. **Trends & Records** — cross-period trends, budget comparison, operational facts, and record detail.

The initial definition should contain approximately 22 to 24 widgets. Exact count may change during implementation when needed for a balanced responsive layout, but it must stay comfortably within the existing limits of 16 sections, 48 total widgets, and 16 widgets per section.

### Widget Strategy

Use analytics widgets whenever values can be calculated from permitted records:

- Count or sum KPI cards.
- Actual, target, and budget summaries.
- Breakdown by metric, product, equipment, or module.
- Trends by `period_date`.
- Operational record tables.

Use `sample-dashboard` adapters only for bounded visual treatments that the analytics contract cannot currently express cleanly:

- Actual-versus-target or actual-versus-budget combinations.
- Stacked composition.
- Target lines and attainment displays.
- Status panels.
- Detail-popup demonstrations.

Adapter settings are illustrative rather than calculated from live record rows. Every adapter must include the existing Operations source label and must use wording that does not imply the displayed adapter values were queried from the database.

## Source Contract

The template has one required source slot:

```txt
operations
```

The slot:

- Accepts a form binding.
- Allows an optional compatible saved report binding.
- Requires the fields used by the template to be present and reportable.
- Contains no environment-specific form or report identifier in frontend template code.

Required field capabilities are based on the existing Operations schema:

- `module`
- `metric_key`
- `fiscal_year`
- `period_type`
- `period_label`
- `period_number`
- `period_date`
- `product`
- `equipment`
- `actual_value`
- `target_value`
- `budget_value`
- `numerator`
- `denominator`
- `unit`

The deterministic published sample binds the source slot conceptually to the existing fixed Operations form ID in backend development seeding. The reusable frontend template remains environment-neutral.

## Filters

Provide five filters:

1. **Fiscal year** — applies to all compatible analytics widgets.
2. **Period type** — applies to all compatible analytics widgets.
3. **Product** — targets Production, Supply Chain, QA/QC, and record widgets.
4. **Equipment** — targets Engineering and record widgets.
5. **Module** — targets Overview, cross-module trend, and record widgets.

Filters use the existing `applyToWidgetKeys` template contract and become runtime `applyToWidgetIds` during instantiation. Targeting must prevent a contextual filter from unexpectedly emptying unrelated sections. Adapter widgets do not claim live filtering unless their existing adapter contract explicitly supports that behavior.

## Template Lifecycle

Register a new template with a stable identifier:

```txt
operations-performance
```

Template creation follows the current builder workflow:

1. A user with dashboard-management access selects the Operations Performance template.
2. The user binds the `operations` source slot to a permitted form and optional report.
3. Client capability validation checks required fields and adapter availability.
4. Instantiation generates new section, widget, layout, and filter IDs.
5. The result is an independent draft saved through the normal dashboard API.
6. Later template changes do not mutate existing dashboards.

Provenance is informational and records the stable template ID, template version, and instantiation timestamp.

## Published Sample Lifecycle

Development seeding creates a separate dashboard with:

- A deterministic dashboard ID.
- Name `Operations Performance Sample`.
- Slug `operations-performance-sample`.
- Published status and a valid published snapshot.
- Workspace-visible audience.
- `showInNavigation` set to `false`, keeping the sample discoverable in the dashboard directory without permanently adding demo navigation.
- Template provenance using `operations-performance` version 1.

Seeding is additive and idempotent:

- Create the sample only when its deterministic ID is absent.
- Never overwrite edits, publication state, revisions, or archive state for an existing row.
- Never silently recreate the sample under a second ID or restore it from the recycle bin.
- Skip creation safely if the deterministic Operations form or required schema is unavailable.

The published sample is independent of drafts users later create from the gallery template.

## Template And Seed Parity

The frontend template and backend seed cannot share TypeScript/C# objects directly. They must share a documented semantic contract:

- Stable section keys and ordering.
- Stable widget keys and intended source type.
- Stable filter keys and widget targeting.
- The same template ID and version.
- Equivalent appearance and publication defaults where applicable.

Focused tests should assert these stable identifiers and expected counts on both sides. The tests do not need to require byte-identical JSON because runtime IDs and persisted publication data legitimately differ.

## Permissions And Security

No permission bypass is introduced for demo content.

- Creating, editing, publishing, archiving, or restoring dashboards uses existing dashboard-management permissions.
- Viewing the published sample requires normal dashboard/menu access and any configured publication permission.
- Analytics execution requires permission to view the bound form and applies record-scope rules.
- Optional saved report sources require report view access.
- Hidden fields remain rejected or excluded by backend analytics validation.
- The published definition must not embed record values that are protected by source permissions.
- Existing audit behavior covers dashboard creation, publication, archive, restore, and applicable viewing/export actions.

## Validation And Error Handling

Template authoring and instantiation should fail with the existing structured template errors when:

- The `operations` source is missing or malformed.
- Required fields are absent or not reportable.
- A requested adapter is unavailable.
- A filter targets a missing widget or a widget with a different source.
- Template bounds or unique-key rules are violated.

Persisted dashboards pass the normal backend definition validator. Runtime analytics errors remain isolated to the affected widget so one invalid or forbidden query does not make the whole dashboard unusable.

The UI should provide an actionable error when the selected source lacks required capabilities. It must not auto-select an unauthorized form or silently discard incompatible filters.

## Responsive And Accessible Behavior

The template uses existing responsive dashboard size presets and layout rules:

- KPI cards remain readable at narrow widths.
- Wide trends and tables span the available row where appropriate.
- Tabs remain keyboard accessible and expose the active section correctly.
- Filters retain labels and usable controls at mobile widths.
- Widget loading, empty, forbidden, and failed states retain accessible text.
- Drag-and-drop and property editing remain available through the current builder, including accessible fallback controls.

## Implementation Surface

Expected frontend changes:

- Add `operationsPerformance.ts` under the dashboard templates folder.
- Register it in the template catalog.
- Extend template engine/catalog tests for validation, capability checks, targeted filters, independent instantiation, and registration order.

Expected backend changes:

- Add the deterministic Operations sample dashboard definition to `DemoDataSeeder` using existing saved-dashboard contracts.
- Reuse existing adapter allowlists and analytics validation.
- Extend the lightweight backend test harness for deterministic IDs, seed idempotency, definition validity, and expected Operations fixtures.

Expected documentation changes:

- Add a dashboard task describing implementation acceptance.
- Update the seed-data plan and dashboard feature documentation with the new sample/template.
- Update API or architecture documentation only if implementation changes a documented contract; none is expected by design.

## Testing And Verification

### Frontend automated checks

- The template validates without structural errors.
- The catalog contains one Operations template with a unique stable ID.
- Missing, malformed, or incompatible source bindings return actionable errors.
- The five filters resolve to only their intended runtime widget IDs.
- Two instantiations produce independent IDs and objects.
- Adapter absence is reported before draft creation.
- Existing Business Performance template tests continue to pass.

### Backend automated checks

- The Operations form retains the required schema and deterministic 72-record fixture.
- The seeded dashboard has deterministic identity, slug, template provenance, sections, filters, and valid widget definitions.
- Seeding twice does not duplicate or overwrite the sample.
- Archived or edited seeded dashboards are preserved.
- Analytics field and adapter settings pass the existing backend validator.
- Existing seed and dashboard tests continue to pass.

### Browser verification

- The gallery displays the Operations Performance template.
- Binding the existing Operations form and creating a draft succeeds.
- The separately seeded sample appears in the dashboard directory and opens through its published slug.
- All seven tabs, widget states, and targeted filters work at desktop and mobile widths.
- Builder drag/drop and properties editing work on an instantiated draft.
- The browser console has no new errors.

### Build verification

- Frontend tests and production build pass under the required Node.js version.
- Backend lightweight test harness and API build pass under .NET 10.

## Acceptance Criteria

- [ ] A reusable Operations Performance template is available in the gallery.
- [ ] The template contains one environment-neutral Operations source slot.
- [ ] A permitted compatible source can instantiate an independent dashboard draft.
- [ ] Seven focused sections provide Operations overview, module analysis, trends, and records.
- [ ] Five filters use bounded, compatible widget targeting.
- [ ] Record-backed metrics use the analytics engine; illustrative adapter values are clearly identified.
- [ ] A separate deterministic sample is published at `operations-performance-sample` in development data.
- [ ] Seeding is idempotent and never overwrites edits or archive state.
- [ ] Existing backend permissions and hidden-field enforcement apply unchanged.
- [ ] No database migration or new visualization dependency is introduced.
- [ ] Focused automated tests, builds, and browser checks pass.
- [ ] User-facing seed/task documentation is updated.

## Out Of Scope

- A separate Operations application module or custom Operations page.
- New form fields or additional Operations records.
- Cross-form joins.
- Formulas, arbitrary SQL, forecasting, anomaly detection, or write-back actions.
- Live calculation of adapter-only illustrative charts.
- Automatic upgrade of dashboards previously instantiated from the template.
- New charting or drag-and-drop libraries.
- Database schema changes.

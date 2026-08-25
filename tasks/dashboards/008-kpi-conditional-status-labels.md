# KPI Conditional Status Labels

## Goal

Make KPI conditional formatting understandable without relying on color by pairing every enabled rule with author-defined status text.

## Acceptance

- [x] Each conditional rule may store a bounded status label, and enabled rules require a nonblank label.
- [x] New starter rules use `On target`, `Watch`, and `Below target` labels.
- [x] The properties drawer lets authors edit each label beside its threshold and color.
- [x] First-match evaluation returns the rule ID, semantic accent, and trimmed status label together.
- [x] The live preview and published viewer display the matching status as a readable badge.
- [x] Nested appearance cloning preserves status labels.
- [x] Frontend and backend validation reject blank enabled labels and labels longer than 80 characters.
- [x] Focused tests and browser verification cover evaluation, validation, authoring, and rendering.

## Boundaries

Status labels are bounded presentation metadata. They do not alter analytics requests, records, permissions, or the ordered first-match behavior.

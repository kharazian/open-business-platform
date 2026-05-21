# Form Renderer and Preview Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a reusable V1 form renderer and a builder preview modal that validates draft record values locally.

**Architecture:** Keep schema editing in the existing builder page and move runtime form rendering into focused forms feature files. Use pure helper functions for default values, value coercion, validation error mapping, and forced preview layout classes so the risky behavior is covered by lightweight tests. The preview modal renders the current local draft through the same `FormRenderer` component that later submission and record detail pages can reuse.

**Tech Stack:** React, TypeScript, Tailwind CSS classes, lucide-react, existing local Node test harness with `npx tsc`.

**Commit Policy:** The user requested no commits until all work is finished, so this plan omits intermediate commit steps.

---

### Task 1: Renderer Helpers

**Files:**
- Create: `src/app/src/features/forms/renderer.ts`
- Create: `src/app/src/features/forms/formRenderer.test.mjs`
- Modify: `src/app/package.json`

- [ ] **Step 1: Write the failing helper test**

Create `src/app/src/features/forms/formRenderer.test.mjs` with tests that compile `types.ts` and `renderer.ts`, then verify:

```js
import assert from "node:assert/strict";
import { execFileSync } from "node:child_process";
import { existsSync, rmSync } from "node:fs";
import { createRequire } from "node:module";

const outDir = "/tmp/obp-form-renderer-test";

rmSync(outDir, { recursive: true, force: true });
execFileSync(
  "npx",
  [
    "tsc",
    "src/features/forms/types.ts",
    "src/features/forms/renderer.ts",
    "--ignoreConfig",
    "--target",
    "ES2022",
    "--module",
    "CommonJS",
    "--moduleResolution",
    "Node",
    "--ignoreDeprecations",
    "6.0",
    "--outDir",
    outDir,
    "--skipLibCheck",
    "--strict"
  ],
  { stdio: "inherit" }
);

const emittedRendererPath = existsSync(`${outDir}/features/forms/renderer.js`) ? `${outDir}/features/forms/renderer.js` : `${outDir}/renderer.js`;
const require = createRequire(import.meta.url);
const {
  coerceFieldInputValue,
  createInitialRecordValues,
  getColumnSpanClass,
  getFieldErrorsById,
  getLayoutFields
} = require(emittedRendererPath);

const schema = {
  schemaVersion: 1,
  fields: [
    { id: "name", type: "text", label: "Name", defaultValue: "Ada" },
    { id: "amount", type: "number", label: "Amount", defaultValue: 25 },
    { id: "approved", type: "checkbox", label: "Approved", defaultValue: true },
    {
      id: "priority",
      type: "radio",
      label: "Priority",
      options: [
        { id: "priority_low", label: "Low", value: "low" },
        { id: "priority_high", label: "High", value: "high" }
      ],
      defaultValue: "high"
    }
  ],
  layout: {
    pages: [
      {
        id: "page_1",
        sections: [
          {
            id: "section_1",
            rows: [
              {
                id: "row_1",
                columns: [{ id: "column_1", span: { mobile: 12, tablet: 6, desktop: 4 }, fields: ["name", "missing", "amount"] }]
              }
            ]
          }
        ]
      }
    ]
  }
};

assert.deepEqual(createInitialRecordValues(schema), {
  name: "Ada",
  amount: 25,
  approved: true,
  priority: "high"
});

assert.equal(coerceFieldInputValue(schema.fields[1], "42"), 42);
assert.equal(coerceFieldInputValue(schema.fields[1], ""), null);
assert.equal(coerceFieldInputValue(schema.fields[2], false), false);
assert.equal(coerceFieldInputValue(schema.fields[0], "Grace"), "Grace");

assert.deepEqual(
  getFieldErrorsById([
    { path: "values.name", code: "record.required", message: "Name is required." },
    { path: "values.amount", code: "record.type", message: "Amount must be numeric." },
    { path: "layout", code: "layout.field_missing", message: "Layout issue." }
  ]),
  {
    name: ["Name is required."],
    amount: ["Amount must be numeric."]
  }
);

assert.deepEqual(
  getLayoutFields(schema.layout.pages[0].sections[0].rows[0].columns[0], new Map(schema.fields.map((field) => [field.id, field]))).map(
    (field) => field.id
  ),
  ["name", "amount"]
);

assert.equal(getColumnSpanClass(schema.layout.pages[0].sections[0].rows[0].columns[0], "mobile"), "col-span-12");
assert.equal(getColumnSpanClass(schema.layout.pages[0].sections[0].rows[0].columns[0], "tablet"), "col-span-6");
assert.equal(getColumnSpanClass(schema.layout.pages[0].sections[0].rows[0].columns[0], "desktop"), "col-span-4");
assert.equal(getColumnSpanClass(schema.layout.pages[0].sections[0].rows[0].columns[0], "responsive"), "col-span-12 md:col-span-6 xl:col-span-4");
```

- [ ] **Step 2: Run the focused test to verify it fails**

Run: `cd src/app && node src/features/forms/formRenderer.test.mjs`

Expected: fail because `src/features/forms/renderer.ts` does not exist.

- [ ] **Step 3: Implement renderer helpers**

Create `src/app/src/features/forms/renderer.ts` exporting:

```ts
import type { FormField, FormLayoutColumn, FormRecordValue, FormRecordValues, FormSchema, ValidationError } from "./types";

export type FormPreviewSize = "responsive" | "mobile" | "tablet" | "desktop";

export function createInitialRecordValues(schema: FormSchema): FormRecordValues {
  return Object.fromEntries(schema.fields.map((field) => [field.id, normalizeInitialFieldValue(field)]));
}

export function coerceFieldInputValue(field: FormField, value: FormRecordValue | string | boolean): FormRecordValue {
  if (field.type === "checkbox") return Boolean(value);
  if (value === "") return null;
  if (field.type === "number") {
    const numericValue = typeof value === "number" ? value : Number(value);
    return Number.isFinite(numericValue) ? numericValue : null;
  }
  if (typeof value === "boolean") return value;
  return String(value);
}

export function getFieldErrorsById(errors: ValidationError[] = []): Record<string, string[]> {
  return errors.reduce<Record<string, string[]>>((result, error) => {
    const match = /^values\.([^.[\]]+)$/.exec(error.path);
    if (!match) return result;
    result[match[1]] = [...(result[match[1]] ?? []), error.message];
    return result;
  }, {});
}

export function getLayoutFields(column: FormLayoutColumn, fieldsById: Map<string, FormField>): FormField[] {
  return column.fields.map((fieldId) => fieldsById.get(fieldId)).filter((field): field is FormField => Boolean(field));
}

export function getColumnSpanClass(column: FormLayoutColumn, previewSize: FormPreviewSize = "responsive"): string {
  if (previewSize === "mobile") return `col-span-${normalizeSpan(column.span.mobile)}`;
  if (previewSize === "tablet") return `col-span-${normalizeSpan(column.span.tablet)}`;
  if (previewSize === "desktop") return `col-span-${normalizeSpan(column.span.desktop)}`;
  return `col-span-${normalizeSpan(column.span.mobile)} md:col-span-${normalizeSpan(column.span.tablet)} xl:col-span-${normalizeSpan(column.span.desktop)}`;
}
```

Implement the private `normalizeInitialFieldValue()` and `normalizeSpan()` helpers in the same file.

- [ ] **Step 4: Run the focused test to verify it passes**

Run: `cd src/app && node src/features/forms/formRenderer.test.mjs`

Expected: pass.

- [ ] **Step 5: Add the helper test to the frontend test script**

Modify `src/app/package.json` and insert:

```json
node src/features/forms/formRenderer.test.mjs
```

after `node src/features/forms/formBuilder.test.mjs`.

### Task 2: FormRenderer Component

**Files:**
- Create: `src/app/src/features/forms/components/FormRenderer.tsx`
- Modify: `src/app/src/features/forms/index.ts`

- [ ] **Step 1: Implement the reusable component**

Create `FormRenderer.tsx` that:

- Imports shared UI controls from `components/ui`.
- Accepts `schema`, `values`, `errors`, `mode`, `previewSize`, `submitLabel`, `onChange`, and `onSubmit`.
- Builds `fieldsById` and `errorsById`.
- Renders pages, sections, rows, columns, and fields.
- Uses `getColumnSpanClass`, `getLayoutFields`, and `coerceFieldInputValue`.
- Shows an `EmptyState` when `schema.fields.length === 0`.
- Renders a submit button only when `onSubmit` exists and `mode !== "readonly"`.

- [ ] **Step 2: Export the component**

Modify `src/app/src/features/forms/index.ts`:

```ts
export * from "./components/FormRenderer";
```

### Task 3: Builder Preview Modal

**Files:**
- Modify: `src/app/src/features/forms/pages/FormBuilderPage.tsx`

- [ ] **Step 1: Add preview state and handlers**

Add imports for `Eye`, `Monitor`, `Smartphone`, `Tablet`, `Modal`, `FormRenderer`, renderer helpers, `validateRecordValues`, and `ValidationError`.

Add state:

```ts
const [previewOpen, setPreviewOpen] = useState(false);
const [previewSize, setPreviewSize] = useState<FormPreviewSize>("desktop");
const [previewValues, setPreviewValues] = useState<FormRecordValues>(() => createInitialRecordValues(schema));
const [previewErrors, setPreviewErrors] = useState<ValidationError[]>([]);
const [previewNotice, setPreviewNotice] = useState<string | null>(null);
```

Add handlers to open preview, change field values, and validate preview with `validateRecordValues(schema, previewValues)`.

- [ ] **Step 2: Add the toolbar action**

Add a `Preview` button near `Save draft`:

```tsx
<Button onClick={handleOpenPreview} variant="outline">
  <Eye className="size-4" />
  Preview
</Button>
```

- [ ] **Step 3: Render the preview modal**

Render a large `Modal` with `FormPreviewSizeSelector`, success/error notice, and:

```tsx
<FormRenderer
  errors={previewErrors}
  onChange={handlePreviewValueChange}
  onSubmit={handleValidatePreview}
  previewSize={previewSize}
  schema={schema}
  submitLabel="Validate preview"
  values={previewValues}
/>
```

### Task 4: Documentation and Verification

**Files:**
- Modify: `tasks/v1/007-form-renderer-and-preview.md`
- Modify: `docs/MVP_CHECKLIST.md`

- [ ] **Step 1: Update task status**

Mark all acceptance criteria in `tasks/v1/007-form-renderer-and-preview.md` complete and add a short `Current Status` section describing the reusable renderer and local preview modal.

- [ ] **Step 2: Update MVP checklist**

Mark `Form preview` complete in `docs/MVP_CHECKLIST.md`.

- [ ] **Step 3: Run frontend tests**

Run: `cd src/app && npm test`

Expected: all frontend tests pass.

- [ ] **Step 4: Run frontend build**

Run: `cd src/app && npm run build`

Expected: TypeScript and Vite build complete successfully.

# Forms List Create Frontend Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the V1 `/forms` frontend page where builders can list form summaries, filter them, and create an in-memory draft form.

**Architecture:** Keep pure form list/create logic in `src/app/src/features/forms/drafts.ts` so it can be tested without React. Render the product page in `src/app/src/features/forms/pages/FormsListPage.tsx`, then register it through a new platform module at `src/app/src/modules/forms/module.tsx`.

**Tech Stack:** React, React Router, TypeScript, Vite, Tailwind CSS, lucide-react, Node-based test harness with `tsc`.

---

## File Structure

- Create `src/app/src/features/forms/drafts.ts`: form summary statuses, list/create types, sample summaries, draft creation validation, filtering helpers, and label helpers.
- Create `src/app/src/features/forms/formDrafts.test.mjs`: pure helper tests that compile and run `drafts.ts`.
- Modify `src/app/src/features/forms/index.ts`: export the new draft helpers.
- Create `src/app/src/features/forms/pages/FormsListPage.tsx`: `/forms` page UI with filters, table, mobile cards, empty state, and create modal.
- Create `src/app/src/modules/forms/module.tsx`: route and navigation registration for Forms.
- Modify `src/app/src/modules/index.ts`: include `formsModule` in the platform module list.
- Modify `src/app/package.json`: add `node src/features/forms/formDrafts.test.mjs` to the existing `npm test` chain while preserving the existing users test entry.

---

### Task 1: Add Form Draft Helper Red Test

**Files:**
- Create: `src/app/src/features/forms/formDrafts.test.mjs`
- Modify: `src/app/package.json`

- [ ] **Step 1: Create the failing helper test**

Create `src/app/src/features/forms/formDrafts.test.mjs`:

```js
import assert from "node:assert/strict";
import { execFileSync } from "node:child_process";
import { existsSync, rmSync } from "node:fs";
import { createRequire } from "node:module";

const outDir = "/tmp/obp-form-drafts-test";

rmSync(outDir, { recursive: true, force: true });
execFileSync(
  "npx",
  [
    "tsc",
    "src/features/forms/types.ts",
    "src/features/forms/drafts.ts",
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

const emittedDraftsPath = existsSync(`${outDir}/features/forms/drafts.js`)
  ? `${outDir}/features/forms/drafts.js`
  : `${outDir}/drafts.js`;
const require = createRequire(import.meta.url);
const {
  createFormDraftSummary,
  filterFormSummaries,
  formStatuses,
  getFormStatusLabel,
  sampleFormSummaries,
  validateCreateFormDraftInput
} = require(emittedDraftsPath);

assert.deepEqual(formStatuses, ["draft", "published", "archived"]);

const draft = createFormDraftSummary({
  id: "form_test",
  name: "  Safety inspection  ",
  description: "  Used before opening a site  ",
  now: "2026-05-19T12:00:00.000Z"
});

assert.deepEqual(draft, {
  id: "form_test",
  name: "Safety inspection",
  description: "Used before opening a site",
  status: "draft",
  fieldCount: 0,
  currentVersionId: null,
  createdAt: "2026-05-19T12:00:00.000Z",
  updatedAt: "2026-05-19T12:00:00.000Z"
});

assert.deepEqual(validateCreateFormDraftInput({ name: "   ", description: " keep me " }), {
  valid: false,
  error: "Form name is required.",
  value: { name: "", description: "keep me" }
});

assert.equal(filterFormSummaries(sampleFormSummaries, { query: "expense", status: "all" }).length, 1);
assert.equal(filterFormSummaries(sampleFormSummaries, { query: "", status: "draft" }).every((form) => form.status === "draft"), true);
assert.equal(filterFormSummaries(sampleFormSummaries, { query: "does-not-exist", status: "all" }).length, 0);
assert.equal(getFormStatusLabel("published"), "Published");
```

- [ ] **Step 2: Add the test to the package script**

Modify the `test` script in `src/app/package.json` to preserve the existing users test and add the form drafts test:

```json
"test": "node src/platform/moduleRegistry.test.mjs && node src/features/forms/formSchema.test.mjs && node src/features/forms/formDrafts.test.mjs && node src/features/auth/authClient.test.mjs && node src/features/users/userTypes.test.mjs"
```

- [ ] **Step 3: Run the red test**

Run:

```bash
cd src/app && npm test
```

Expected: FAIL because `src/features/forms/drafts.ts` does not exist yet.

---

### Task 2: Implement Form Draft Helpers

**Files:**
- Create: `src/app/src/features/forms/drafts.ts`
- Modify: `src/app/src/features/forms/index.ts`
- Test: `src/app/src/features/forms/formDrafts.test.mjs`

- [ ] **Step 1: Create the draft helpers**

Create `src/app/src/features/forms/drafts.ts`:

```ts
export const formStatuses = ["draft", "published", "archived"] as const;

export type FormStatus = (typeof formStatuses)[number];
export type FormStatusFilter = "all" | FormStatus;

export type FormSummary = {
  id: string;
  name: string;
  description?: string;
  status: FormStatus;
  fieldCount: number;
  currentVersionId?: string | null;
  createdAt: string;
  updatedAt: string;
};

export type CreateFormDraftInput = {
  id?: string;
  name: string;
  description?: string;
  now?: string;
};

export type CreateFormDraftValidation =
  | {
      valid: true;
      value: {
        name: string;
        description?: string;
      };
    }
  | {
      valid: false;
      error: string;
      value: {
        name: string;
        description?: string;
      };
    };

export type FormSummaryFilter = {
  query: string;
  status: FormStatusFilter;
};

export const sampleFormSummaries: FormSummary[] = [
  {
    id: "form_employee_onboarding",
    name: "Employee onboarding",
    description: "Collect new hire details before HR review.",
    status: "published",
    fieldCount: 7,
    currentVersionId: "version_employee_onboarding_1",
    createdAt: "2026-05-10T14:00:00.000Z",
    updatedAt: "2026-05-15T17:30:00.000Z"
  },
  {
    id: "form_expense_request",
    name: "Expense request",
    description: "Draft intake for employee reimbursements.",
    status: "draft",
    fieldCount: 5,
    currentVersionId: null,
    createdAt: "2026-05-12T10:15:00.000Z",
    updatedAt: "2026-05-18T19:20:00.000Z"
  },
  {
    id: "form_vendor_access",
    name: "Vendor access",
    description: "Archived access request form for legacy vendor onboarding.",
    status: "archived",
    fieldCount: 6,
    currentVersionId: "version_vendor_access_2",
    createdAt: "2026-04-20T09:00:00.000Z",
    updatedAt: "2026-05-02T13:45:00.000Z"
  }
];

export function validateCreateFormDraftInput(input: Pick<CreateFormDraftInput, "name" | "description">): CreateFormDraftValidation {
  const name = input.name.trim();
  const description = normalizeOptionalText(input.description);
  const value = description ? { name, description } : { name };

  if (!name) {
    return {
      valid: false,
      error: "Form name is required.",
      value
    };
  }

  return {
    valid: true,
    value
  };
}

export function createFormDraftSummary(input: CreateFormDraftInput): FormSummary {
  const validation = validateCreateFormDraftInput(input);

  if (!validation.valid) {
    throw new Error(validation.error);
  }

  const timestamp = input.now ?? new Date().toISOString();

  return {
    id: input.id ?? createFormId(),
    name: validation.value.name,
    description: validation.value.description,
    status: "draft",
    fieldCount: 0,
    currentVersionId: null,
    createdAt: timestamp,
    updatedAt: timestamp
  };
}

export function filterFormSummaries(forms: FormSummary[], filter: FormSummaryFilter): FormSummary[] {
  const query = filter.query.trim().toLowerCase();

  return forms.filter((form) => {
    const matchesStatus = filter.status === "all" || form.status === filter.status;

    if (!matchesStatus) {
      return false;
    }

    if (!query) {
      return true;
    }

    return `${form.name} ${form.description ?? ""}`.toLowerCase().includes(query);
  });
}

export function getFormStatusLabel(status: FormStatus): string {
  return status.charAt(0).toUpperCase() + status.slice(1);
}

function normalizeOptionalText(value: string | undefined): string | undefined {
  const normalized = value?.trim();
  return normalized ? normalized : undefined;
}

function createFormId(): string {
  if (globalThis.crypto?.randomUUID) {
    return `form_${globalThis.crypto.randomUUID()}`;
  }

  return `form_${Date.now().toString(36)}`;
}
```

- [ ] **Step 2: Export the helpers**

Modify `src/app/src/features/forms/index.ts`:

```ts
export * from "./types";
export * from "./validation";
export * from "./drafts";
```

- [ ] **Step 3: Run the helper test**

Run:

```bash
cd src/app && node src/features/forms/formDrafts.test.mjs
```

Expected: PASS.

- [ ] **Step 4: Run the frontend test suite**

Run:

```bash
cd src/app && npm test
```

Expected: PASS.

---

### Task 3: Add Forms Page UI

**Files:**
- Create: `src/app/src/features/forms/pages/FormsListPage.tsx`
- Test manually through `/forms` after module registration in Task 4.

- [ ] **Step 1: Create the Forms list page**

Create `src/app/src/features/forms/pages/FormsListPage.tsx`:

```tsx
import { type FormEvent, useMemo, useState } from "react";
import { FileText, Plus, Search } from "lucide-react";
import { Badge } from "../../../components/ui/Badge";
import { Button } from "../../../components/ui/Button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "../../../components/ui/Card";
import { EmptyState } from "../../../components/ui/EmptyState";
import { Input } from "../../../components/ui/Input";
import { Modal } from "../../../components/ui/Modal";
import { PageHeader } from "../../../components/ui/PageHeader";
import { Select } from "../../../components/ui/Select";
import { Table, type TableColumn } from "../../../components/ui/Table";
import { Textarea } from "../../../components/ui/Textarea";
import {
  createFormDraftSummary,
  filterFormSummaries,
  formStatuses,
  getFormStatusLabel,
  sampleFormSummaries,
  validateCreateFormDraftInput,
  type FormStatus,
  type FormStatusFilter,
  type FormSummary
} from "../drafts";

const statusOptions: Array<{ label: string; value: FormStatusFilter }> = [
  { label: "All statuses", value: "all" },
  ...formStatuses.map((status) => ({ label: getFormStatusLabel(status), value: status }))
];

const statusBadgeVariant: Record<FormStatus, "default" | "success" | "warning"> = {
  draft: "warning",
  published: "success",
  archived: "default"
};

const formColumns: Array<TableColumn<FormSummary>> = [
  {
    header: "Form",
    render: (form) => (
      <div>
        <p className="font-bold text-foreground">{form.name}</p>
        {form.description ? <p className="mt-1 max-w-xl text-sm leading-5 text-muted-foreground">{form.description}</p> : null}
      </div>
    )
  },
  {
    header: "Status",
    render: (form) => <Badge variant={statusBadgeVariant[form.status]}>{getFormStatusLabel(form.status)}</Badge>
  },
  { header: "Fields", accessor: "fieldCount" },
  {
    header: "Current version",
    render: (form) => (form.currentVersionId ? <span className="font-semibold">{form.currentVersionId}</span> : <span className="text-muted-foreground">Draft only</span>)
  },
  {
    header: "Updated",
    render: (form) => formatDate(form.updatedAt)
  }
];

export function FormsListPage() {
  const [forms, setForms] = useState<FormSummary[]>(sampleFormSummaries);
  const [query, setQuery] = useState("");
  const [status, setStatus] = useState<FormStatusFilter>("all");
  const [createOpen, setCreateOpen] = useState(false);
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [nameError, setNameError] = useState<string | undefined>();

  const filteredForms = useMemo(() => filterFormSummaries(forms, { query, status }), [forms, query, status]);
  const draftCount = forms.filter((form) => form.status === "draft").length;
  const publishedCount = forms.filter((form) => form.status === "published").length;
  const archivedCount = forms.filter((form) => form.status === "archived").length;

  function openCreateModal() {
    setCreateOpen(true);
    setNameError(undefined);
  }

  function closeCreateModal() {
    setCreateOpen(false);
    setName("");
    setDescription("");
    setNameError(undefined);
  }

  function handleCreateSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const validation = validateCreateFormDraftInput({ name, description });

    if (!validation.valid) {
      setNameError(validation.error);
      return;
    }

    setForms((currentForms) => [createFormDraftSummary({ name, description }), ...currentForms]);
    closeCreateModal();
  }

  function clearFilters() {
    setQuery("");
    setStatus("all");
  }

  return (
    <div className="grid gap-6">
      <PageHeader
        eyebrow="Forms"
        title="Form drafts"
        description="Create and manage the forms that will collect records across the workspace."
        actions={
          <Button onClick={openCreateModal}>
            <Plus className="size-4" />
            Create form
          </Button>
        }
      />

      <section className="grid gap-4 md:grid-cols-3">
        <SummaryTile label="Drafts" value={draftCount} />
        <SummaryTile label="Published" value={publishedCount} />
        <SummaryTile label="Archived" value={archivedCount} />
      </section>

      <Card>
        <CardHeader>
          <CardTitle>Forms</CardTitle>
          <CardDescription>Filter draft, published, and archived form summaries.</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="mb-4 grid gap-3 md:grid-cols-[minmax(0,1fr)_14rem]">
            <Input
              aria-label="Search forms"
              icon={<Search className="size-4" />}
              onChange={(event) => setQuery(event.target.value)}
              placeholder="Search by name or description"
              value={query}
            />
            <Select
              aria-label="Filter forms by status"
              onChange={(event) => setStatus(event.target.value as FormStatusFilter)}
              options={statusOptions}
              value={status}
            />
          </div>

          {filteredForms.length > 0 ? (
            <>
              <div className="hidden md:block">
                <Table columns={formColumns} rows={filteredForms} />
              </div>
              <div className="grid gap-3 md:hidden">
                {filteredForms.map((form) => (
                  <MobileFormSummary key={form.id} form={form} />
                ))}
              </div>
            </>
          ) : (
            <EmptyState
              title="No forms found"
              description="No form summaries match the current search and status filters."
              action={
                <Button onClick={clearFilters} variant="outline">
                  Clear filters
                </Button>
              }
            />
          )}
        </CardContent>
      </Card>

      <Modal
        open={createOpen}
        onClose={closeCreateModal}
        title="Create form"
        description="Start a draft form with a name and optional description."
        footer={
          <>
            <Button onClick={closeCreateModal} variant="outline">
              Cancel
            </Button>
            <Button form="create-form" type="submit">
              Create draft
            </Button>
          </>
        }
      >
        <form className="grid gap-4" id="create-form" onSubmit={handleCreateSubmit}>
          <Input
            autoFocus
            error={nameError}
            label="Form name"
            onChange={(event) => {
              setName(event.target.value);
              if (nameError) setNameError(undefined);
            }}
            placeholder="Expense request"
            value={name}
          />
          <Textarea
            label="Description"
            onChange={(event) => setDescription(event.target.value)}
            placeholder="What this form is used for"
            value={description}
          />
        </form>
      </Modal>
    </div>
  );
}

function SummaryTile({ label, value }: { label: string; value: number }) {
  return (
    <Card className="p-5">
      <p className="text-sm font-bold text-muted-foreground">{label}</p>
      <p className="mt-3 text-3xl font-bold text-foreground">{value}</p>
    </Card>
  );
}

function MobileFormSummary({ form }: { form: FormSummary }) {
  return (
    <div className="rounded-xl border border-border bg-card/80 p-4">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <div className="flex items-center gap-2">
            <FileText className="size-4 shrink-0 text-muted-foreground" />
            <p className="truncate font-bold text-foreground">{form.name}</p>
          </div>
          {form.description ? <p className="mt-2 text-sm leading-5 text-muted-foreground">{form.description}</p> : null}
        </div>
        <Badge className="shrink-0" variant={statusBadgeVariant[form.status]}>
          {getFormStatusLabel(form.status)}
        </Badge>
      </div>
      <dl className="mt-4 grid grid-cols-2 gap-3 text-sm">
        <div>
          <dt className="font-bold text-muted-foreground">Fields</dt>
          <dd className="mt-1 text-foreground">{form.fieldCount}</dd>
        </div>
        <div>
          <dt className="font-bold text-muted-foreground">Updated</dt>
          <dd className="mt-1 text-foreground">{formatDate(form.updatedAt)}</dd>
        </div>
      </dl>
    </div>
  );
}

function formatDate(value: string): string {
  return new Intl.DateTimeFormat("en", {
    month: "short",
    day: "numeric",
    year: "numeric"
  }).format(new Date(value));
}
```

- [ ] **Step 2: Run TypeScript build**

Run:

```bash
cd src/app && npm run build
```

Expected: PASS. If the build fails on type-only issues, fix the exact file named by TypeScript and rerun this command.

---

### Task 4: Register Forms Module

**Files:**
- Create: `src/app/src/modules/forms/module.tsx`
- Modify: `src/app/src/modules/index.ts`

- [ ] **Step 1: Create the module file**

Create `src/app/src/modules/forms/module.tsx`:

```tsx
import { ClipboardList } from "lucide-react";
import { FormsListPage } from "../../features/forms/pages/FormsListPage";
import type { PlatformModule } from "../../platform/moduleRegistry";

export const formsModule: PlatformModule = {
  id: "app.forms",
  name: "Forms",
  owner: "app",
  order: 20,
  routes: [{ path: "/forms", element: <FormsListPage /> }],
  navigation: [{ label: "Forms", path: "/forms", icon: ClipboardList, order: 30 }]
};
```

- [ ] **Step 2: Register the module**

Modify `src/app/src/modules/index.ts`:

```ts
import { dashboardModule } from "./dashboard/module";
import { formsModule } from "./forms/module";
import { profileModule } from "./profile/module";
import { reportsModule } from "./reports/module";
import { settingsModule } from "./settings/module";
import { usersModule } from "./users/module";

export const platformModules = [
  dashboardModule,
  formsModule,
  usersModule,
  reportsModule,
  settingsModule,
  profileModule
];
```

- [ ] **Step 3: Run tests**

Run:

```bash
cd src/app && npm test
```

Expected: PASS.

---

### Task 5: Verify Build And Browser UI

**Files:**
- Verify only unless the commands expose a defect.

- [ ] **Step 1: Run final frontend test suite**

Run:

```bash
cd src/app && npm test
```

Expected: PASS.

- [ ] **Step 2: Run production build**

Run:

```bash
cd src/app && npm run build
```

Expected: PASS.

- [ ] **Step 3: Start the dev server**

Run:

```bash
cd src/app && npm run dev -- --host 127.0.0.1 --port 5174
```

Expected: Vite serves the app at `http://127.0.0.1:5174/`.

- [ ] **Step 4: Browser smoke test**

Open `http://127.0.0.1:5174/forms`.

Expected:

- The app shows the Forms navigation item.
- The `/forms` page renders the header, summary tiles, filters, and table or mobile cards.
- Search filters by name or description.
- Status filter narrows the list.
- Create form modal rejects an empty name.
- Creating a named form adds a new draft summary to the top of the list.

---

## Self-Review

- Spec coverage: This plan covers route registration, navigation, list page, search/status filtering, create modal, local state, helper tests, and no backend persistence.
- Ambiguity scan: Every code-producing task names exact files and complete snippets.
- Type consistency: `FormStatus`, `FormStatusFilter`, `FormSummary`, `CreateFormDraftInput`, `validateCreateFormDraftInput`, `createFormDraftSummary`, and `filterFormSummaries` are introduced in Task 2 before being used by the page in Task 3.

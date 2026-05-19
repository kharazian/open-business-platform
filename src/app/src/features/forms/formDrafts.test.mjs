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

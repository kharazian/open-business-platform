import assert from "node:assert/strict";
import { execFileSync } from "node:child_process";
import { existsSync, rmSync } from "node:fs";
import { createRequire } from "node:module";

const outDir = "/tmp/obp-form-submission-test";

rmSync(outDir, { recursive: true, force: true });
execFileSync(
  "npx",
  [
    "tsc",
    "src/features/forms/types.ts",
    "src/features/forms/renderer.ts",
    "src/features/forms/api.ts",
    "src/features/forms/submission.ts",
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

const emittedSubmissionPath = existsSync(`${outDir}/features/forms/submission.js`)
  ? `${outDir}/features/forms/submission.js`
  : `${outDir}/submission.js`;
const require = createRequire(import.meta.url);
const submission = require(emittedSubmissionPath);

const form = {
  id: "form-2",
  name: "Safety inspection",
  description: null,
  currentVersionId: "version-1",
  currentVersionNumber: 1,
  schema: {
    schemaVersion: 1,
    fields: [{ id: "site_name", type: "text", label: "Site name", required: true }],
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
                  columns: [{ id: "col_1", span: { mobile: 12, tablet: 12, desktop: 12 }, fields: ["site_name"] }]
                }
              ]
            }
          ]
        }
      ]
    }
  }
};

const record = {
  id: "record-1",
  formId: "form-2",
  formVersionId: "version-1",
  status: "active",
  values: { site_name: "North plant" },
  concurrencyStamp: "record-stamp",
  createdAt: "2026-05-19T13:20:00.000Z",
  createdById: null
};

const errors = [{ path: "values.site_name", code: "record.required", message: "'Site name' is required." }];

assert.deepEqual(submission.createPublishedFormSubmissionValues(form), { site_name: "" });
assert.deepEqual(submission.getSubmissionSuccessLinks(record), {
  recordPath: "/records/record-1",
  recordsPath: "/forms/form-2/records",
  formsPath: "/forms"
});
assert.deepEqual(submission.clearSubmissionFieldErrors(errors, "site_name"), []);

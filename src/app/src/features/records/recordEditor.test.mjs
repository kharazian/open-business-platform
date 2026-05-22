import assert from "node:assert/strict";
import { execFileSync } from "node:child_process";
import { existsSync, rmSync } from "node:fs";
import { createRequire } from "node:module";

const outDir = "/tmp/obp-record-editor-test";

rmSync(outDir, { recursive: true, force: true });
execFileSync(
  "npx",
  [
    "tsc",
    "src/features/forms/types.ts",
    "src/features/forms/api.ts",
    "src/features/records/recordEditor.ts",
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

const emittedPath = existsSync(`${outDir}/features/records/recordEditor.js`)
  ? `${outDir}/features/records/recordEditor.js`
  : existsSync(`${outDir}/records/recordEditor.js`)
    ? `${outDir}/records/recordEditor.js`
    : `${outDir}/recordEditor.js`;
const require = createRequire(import.meta.url);
const editor = require(emittedPath);

const record = {
  id: "record-1",
  formId: "form-1",
  formVersionId: "version-1",
  status: "active",
  values: { site_name: "North plant" },
  schema: { schemaVersion: 1, fields: [], layout: { pages: [] } },
  concurrencyStamp: "record-stamp",
  createdAt: "2026-05-19T13:20:00.000Z",
  createdById: null,
  updatedAt: null,
  updatedById: null
};

const draftValues = editor.createRecordEditDraft(record);
draftValues.site_name = "South plant";

assert.deepEqual(record.values, { site_name: "North plant" });
assert.deepEqual(draftValues, { site_name: "South plant" });
assert.deepEqual(editor.createUpdateRecordRequest(record, draftValues), {
  values: { site_name: "South plant" },
  concurrencyStamp: "record-stamp"
});
assert.equal(editor.getRecordListPath(record), "/forms/form-1/records");

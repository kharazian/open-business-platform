import assert from "node:assert/strict";
import { execFileSync } from "node:child_process";
import { existsSync, rmSync } from "node:fs";
import { createRequire } from "node:module";

const outDir = "/tmp/obp-forms-api-test";

rmSync(outDir, { recursive: true, force: true });
execFileSync(
  "npx",
  [
    "tsc",
    "src/features/forms/types.ts",
    "src/features/forms/drafts.ts",
    "src/features/forms/api.ts",
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

const emittedApiPath = existsSync(`${outDir}/features/forms/api.js`) ? `${outDir}/features/forms/api.js` : `${outDir}/api.js`;
const require = createRequire(import.meta.url);
const api = require(emittedApiPath);

const calls = [];
const fetcher = async (input, init = {}) => {
  calls.push({ input, init });

  if (input === "/api/forms" && init.method === "GET") {
    return {
      ok: true,
      json: async () => ({
        items: [
          {
            id: "form-1",
            name: "Expense request",
            description: "Employee reimbursement intake.",
            status: "draft",
            fieldCount: 0,
            currentVersionId: null,
            concurrencyStamp: "stamp",
            createdAt: "2026-05-19T12:00:00.000Z",
            createdById: null,
            updatedAt: null,
            updatedById: null
          }
        ]
      })
    };
  }

  if (input === "/api/forms" && init.method === "POST") {
    return {
      ok: true,
      json: async () => ({
        id: "form-2",
        name: "Safety inspection",
        description: null,
        status: "draft",
        fieldCount: 0,
        currentVersionId: null,
        concurrencyStamp: "stamp-2",
        createdAt: "2026-05-19T13:00:00.000Z",
        createdById: null,
        updatedAt: null,
        updatedById: null
      })
    };
  }

  return { ok: false, json: async () => ({ message: "Unexpected request." }) };
};

const forms = await api.listForms(fetcher);
const created = await api.createForm({ name: "Safety inspection", description: "" }, fetcher);

assert.equal(forms[0].name, "Expense request");
assert.equal(forms[0].fieldCount, 0);
assert.equal(created.status, "draft");
assert.equal(calls[0].input, "/api/forms");
assert.equal(calls[0].init.method, "GET");
assert.equal(calls[0].init.credentials, "include");
assert.equal(calls[1].input, "/api/forms");
assert.equal(calls[1].init.method, "POST");
assert.equal(calls[1].init.credentials, "include");
assert.equal(calls[1].init.headers["Content-Type"], "application/json");
assert.equal(JSON.parse(calls[1].init.body).name, "Safety inspection");

await assert.rejects(
  () => api.listForms(async () => ({ ok: true, json: async () => ({}) })),
  /API response did not include an items collection/
);

await assert.rejects(
  () => api.createForm({ name: "", description: "" }, async () => ({ ok: false, json: async () => ({ message: "Form name is required." }) })),
  /Form name is required/
);

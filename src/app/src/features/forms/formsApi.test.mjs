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

  if (input === "/api/forms/form-2" && init.method === "GET") {
    return {
      ok: true,
      json: async () => ({
        id: "form-2",
        name: "Safety inspection",
        description: null,
        status: "draft",
        fieldCount: 1,
        currentVersionId: null,
        draftSchema: {
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
        },
        concurrencyStamp: "stamp-3",
        createdAt: "2026-05-19T13:00:00.000Z",
        createdById: null,
        updatedAt: "2026-05-19T13:05:00.000Z",
        updatedById: null
      })
    };
  }

  if (input === "/api/forms/form-2" && init.method === "PUT") {
    return {
      ok: true,
      json: async () => ({
        id: "form-2",
        name: "Safety inspection",
        description: null,
        status: "draft",
        fieldCount: JSON.parse(init.body).schema.fields.length,
        currentVersionId: null,
        draftSchema: JSON.parse(init.body).schema,
        concurrencyStamp: "stamp-4",
        createdAt: "2026-05-19T13:00:00.000Z",
        createdById: null,
        updatedAt: "2026-05-19T13:10:00.000Z",
        updatedById: null
      })
    };
  }

  if (input === "/api/forms/form-2/publish" && init.method === "POST") {
    return {
      ok: true,
      json: async () => ({
        form: {
          id: "form-2",
          name: "Safety inspection",
          description: null,
          status: "published",
          fieldCount: 1,
          currentVersionId: "version-1",
          draftSchema: JSON.parse(calls[3].init.body).schema,
          concurrencyStamp: "stamp-5",
          createdAt: "2026-05-19T13:00:00.000Z",
          createdById: null,
          updatedAt: "2026-05-19T13:15:00.000Z",
          updatedById: null
        },
        version: {
          id: "version-1",
          formId: "form-2",
          versionNumber: 1,
          schema: JSON.parse(calls[3].init.body).schema,
          publishedById: null,
          publishedAt: "2026-05-19T13:15:00.000Z"
        }
      })
    };
  }

  return { ok: false, json: async () => ({ message: "Unexpected request." }) };
};

const forms = await api.listForms(fetcher);
const created = await api.createForm({ name: "Safety inspection", description: "" }, fetcher);
const formDetail = await api.getForm("form-2", fetcher);
const updatedDraft = await api.updateFormDraft("form-2", formDetail.draftSchema, fetcher);
const published = await api.publishForm("form-2", fetcher);

assert.equal(forms[0].name, "Expense request");
assert.equal(forms[0].fieldCount, 0);
assert.equal(created.status, "draft");
assert.equal(formDetail.draftSchema.fields[0].id, "site_name");
assert.equal(updatedDraft.fieldCount, 1);
assert.equal(published.form.status, "published");
assert.equal(published.version.versionNumber, 1);
assert.equal(calls[0].input, "/api/forms");
assert.equal(calls[0].init.method, "GET");
assert.equal(calls[0].init.credentials, "include");
assert.equal(calls[1].input, "/api/forms");
assert.equal(calls[1].init.method, "POST");
assert.equal(calls[1].init.credentials, "include");
assert.equal(calls[1].init.headers["Content-Type"], "application/json");
assert.equal(JSON.parse(calls[1].init.body).name, "Safety inspection");
assert.equal(calls[2].input, "/api/forms/form-2");
assert.equal(calls[2].init.method, "GET");
assert.equal(calls[2].init.credentials, "include");
assert.equal(calls[3].input, "/api/forms/form-2");
assert.equal(calls[3].init.method, "PUT");
assert.equal(calls[3].init.credentials, "include");
assert.equal(calls[3].init.headers["Content-Type"], "application/json");
assert.equal(JSON.parse(calls[3].init.body).schema.fields[0].id, "site_name");
assert.equal(calls[4].input, "/api/forms/form-2/publish");
assert.equal(calls[4].init.method, "POST");
assert.equal(calls[4].init.credentials, "include");
assert.equal(calls[4].init.body, undefined);

await assert.rejects(
  () => api.listForms(async () => ({ ok: true, json: async () => ({}) })),
  /API response did not include an items collection/
);

await assert.rejects(
  () => api.createForm({ name: "", description: "" }, async () => ({ ok: false, json: async () => ({ message: "Form name is required." }) })),
  /Form name is required/
);

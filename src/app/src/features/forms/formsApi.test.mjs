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

  if (input === "/api/forms/form-2/published" && init.method === "GET") {
    return {
      ok: true,
      json: async () => ({
        id: "form-2",
        name: "Safety inspection",
        description: null,
        currentVersionId: "version-1",
        currentVersionNumber: 1,
        schema: JSON.parse(calls[3].init.body).schema
      })
    };
  }

  if (input === "/api/forms/form-2/records" && init.method === "POST") {
    return {
      ok: true,
      json: async () => ({
        id: "record-1",
        formId: "form-2",
        formVersionId: "version-1",
        status: "active",
        values: JSON.parse(init.body).values,
        concurrencyStamp: "record-stamp",
        createdAt: "2026-05-19T13:20:00.000Z",
        createdById: null
      })
    };
  }

  if (input === "/api/forms/form-2/records?page=2&pageSize=10&search=North+plant" && init.method === "GET") {
    return {
      ok: true,
      json: async () => ({
        totalCount: 1,
        items: [
          {
            id: "record-1",
            formId: "form-2",
            formVersionId: "version-1",
            status: "active",
            values: { site_name: "North plant" },
            createdAt: "2026-05-19T13:20:00.000Z",
            createdById: null
          }
        ]
      })
    };
  }

  if (input === "/api/records/record-1" && init.method === "GET") {
    return {
      ok: true,
      json: async () => ({
        id: "record-1",
        formId: "form-2",
        formVersionId: "version-1",
        status: "active",
        values: { site_name: "North plant" },
        schema: JSON.parse(calls[3].init.body).schema,
        concurrencyStamp: "record-stamp",
        createdAt: "2026-05-19T13:20:00.000Z",
        createdById: null,
        updatedAt: null,
        updatedById: null
      })
    };
  }

  if (input === "/api/records/record-1" && init.method === "PUT") {
    return {
      ok: true,
      json: async () => ({
        id: "record-1",
        formId: "form-2",
        formVersionId: "version-1",
        status: "active",
        values: JSON.parse(init.body).values,
        schema: JSON.parse(calls[3].init.body).schema,
        concurrencyStamp: "record-stamp-2",
        createdAt: "2026-05-19T13:20:00.000Z",
        createdById: null,
        updatedAt: "2026-05-19T13:25:00.000Z",
        updatedById: null
      })
    };
  }

  if (input === "/api/records/record-1" && init.method === "DELETE") {
    return { ok: true, json: async () => null };
  }

  return { ok: false, json: async () => ({ message: "Unexpected request." }) };
};

const forms = await api.listForms(fetcher);
const created = await api.createForm({ name: "Safety inspection", description: "" }, fetcher);
const formDetail = await api.getForm("form-2", fetcher);
const updatedDraft = await api.updateFormDraft("form-2", formDetail.draftSchema, fetcher);
const published = await api.publishForm("form-2", fetcher);
const publishedSubmissionForm = await api.getPublishedFormForSubmission("form-2", fetcher);
const submittedRecord = await api.submitRecord("form-2", { values: { site_name: "North plant" } }, fetcher);
const listedRecords = await api.listRecords("form-2", { page: 2, pageSize: 10, search: "North plant" }, fetcher);
const recordDetail = await api.getRecord("record-1", fetcher);
const updatedRecord = await api.updateRecord(
  "record-1",
  { values: { site_name: "South plant" }, concurrencyStamp: "record-stamp" },
  fetcher
);
await api.deleteRecord("record-1", fetcher);

assert.equal(forms[0].name, "Expense request");
assert.equal(forms[0].fieldCount, 0);
assert.equal(created.status, "draft");
assert.equal(formDetail.draftSchema.fields[0].id, "site_name");
assert.equal(updatedDraft.fieldCount, 1);
assert.equal(published.form.status, "published");
assert.equal(published.version.versionNumber, 1);
assert.equal(publishedSubmissionForm.currentVersionId, "version-1");
assert.equal(publishedSubmissionForm.schema.fields[0].id, "site_name");
assert.equal(submittedRecord.formVersionId, "version-1");
assert.equal(submittedRecord.values.site_name, "North plant");
assert.equal(listedRecords.totalCount, 1);
assert.equal(listedRecords.items[0].formVersionId, "version-1");
assert.equal(recordDetail.schema.fields[0].id, "site_name");
assert.equal(updatedRecord.values.site_name, "South plant");
assert.equal(updatedRecord.concurrencyStamp, "record-stamp-2");
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
assert.equal(calls[5].input, "/api/forms/form-2/published");
assert.equal(calls[5].init.method, "GET");
assert.equal(calls[5].init.credentials, "include");
assert.equal(calls[6].input, "/api/forms/form-2/records");
assert.equal(calls[6].init.method, "POST");
assert.equal(calls[6].init.credentials, "include");
assert.equal(calls[6].init.headers["Content-Type"], "application/json");
assert.deepEqual(JSON.parse(calls[6].init.body), { values: { site_name: "North plant" } });
assert.equal(calls[7].input, "/api/forms/form-2/records?page=2&pageSize=10&search=North+plant");
assert.equal(calls[7].init.method, "GET");
assert.equal(calls[7].init.credentials, "include");
assert.equal(calls[8].input, "/api/records/record-1");
assert.equal(calls[8].init.method, "GET");
assert.equal(calls[8].init.credentials, "include");
assert.equal(calls[9].input, "/api/records/record-1");
assert.equal(calls[9].init.method, "PUT");
assert.equal(calls[9].init.credentials, "include");
assert.equal(calls[9].init.headers["Content-Type"], "application/json");
assert.deepEqual(JSON.parse(calls[9].init.body), { values: { site_name: "South plant" }, concurrencyStamp: "record-stamp" });
assert.equal(calls[10].input, "/api/records/record-1");
assert.equal(calls[10].init.method, "DELETE");
assert.equal(calls[10].init.credentials, "include");
assert.equal(calls[10].init.body, undefined);

await assert.rejects(
  () => api.listForms(async () => ({ ok: true, json: async () => ({}) })),
  /API response did not include an items collection/
);

await assert.rejects(
  () => api.createForm({ name: "", description: "" }, async () => ({ ok: false, json: async () => ({ message: "Form name is required." }) })),
  /Form name is required/
);

await assert.rejects(
  () =>
    api.submitRecord(
      "form-2",
      { values: {} },
      async () => ({
        ok: false,
        json: async () => ({
          message: "Record values are invalid.",
          errors: [{ path: "values.site_name", code: "record.required", message: "'Site name' is required." }]
        })
      })
    ),
  (error) => {
    assert.equal(error.name, "FormsApiError");
    assert.equal(error.message, "Record values are invalid.");
    assert.equal(error.errors[0].path, "values.site_name");
    return true;
  }
);

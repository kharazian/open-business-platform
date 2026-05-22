import assert from "node:assert/strict";
import { test } from "vitest";
import {
  clearSubmissionFieldErrors,
  createPublishedFormSubmissionValues,
  getSubmissionSuccessLinks
} from "./submission.ts";

test("published form submission helpers create values and success links", () => {
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

  assert.deepEqual(createPublishedFormSubmissionValues(form), { site_name: "" });
  assert.deepEqual(getSubmissionSuccessLinks(record), {
    recordPath: "/records/record-1",
    recordsPath: "/forms/form-2/records",
    formsPath: "/forms"
  });
  assert.deepEqual(clearSubmissionFieldErrors(errors, "site_name"), []);
});

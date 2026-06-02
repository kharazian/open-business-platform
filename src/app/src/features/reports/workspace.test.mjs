import assert from "node:assert/strict";
import { test } from "vitest";
import { loadReportWorkspace } from "./workspace.ts";

test("report workspace keeps saved reports when form detail is forbidden", async () => {
  const result = await loadReportWorkspace("form-1", {
    getForm: async () => {
      throw new Error("Forbidden");
    },
    listReports: async () => [
      {
        id: "report-1",
        formId: "form-1",
        formName: "Employee information",
        name: "Directory",
        type: "list",
        columnCount: 2,
        filterCount: 0,
        sortCount: 1,
        concurrencyStamp: "report-stamp",
        createdAt: "2026-06-02T12:00:00.000Z",
        createdById: null,
        updatedAt: null,
        updatedById: null
      }
    ]
  });

  assert.equal(result.formDetail, null);
  assert.equal(result.reports[0].name, "Directory");
});

test("report workspace still fails when saved reports cannot be listed", async () => {
  await assert.rejects(
    () =>
      loadReportWorkspace("form-1", {
        getForm: async () => ({
          id: "form-1",
          name: "Employee information",
          description: null,
          status: "published",
          fieldCount: 2,
          currentVersionId: "version-1",
          draftSchema: null,
          concurrencyStamp: "form-stamp",
          createdAt: "2026-06-02T12:00:00.000Z",
          createdById: null,
          updatedAt: null,
          updatedById: null
        }),
        listReports: async () => {
          throw new Error("Reports unavailable");
        }
      }),
    /Reports unavailable/
  );
});

import assert from "node:assert/strict";
import { test } from "vitest";
import { searchComplianceAudit } from "./api.ts";

test("compliance audit search encodes bounded filters and paging", async () => {
  let requested = ""; await searchComplianceAudit({ entityType: "Custom Domain", page: 2, pageSize: 25 }, async (input) => { requested = input; return new Response(JSON.stringify({ items: [], page: 2, pageSize: 25, total: 0 }), { status: 200 }); });
  assert.equal(requested, "/api/compliance/audit?entityType=Custom+Domain&page=2&pageSize=25");
});

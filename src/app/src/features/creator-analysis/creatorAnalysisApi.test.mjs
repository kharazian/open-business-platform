import assert from "node:assert/strict";
import { test } from "vitest";
import { analyzeCreatorExport } from "./api.ts";

test("Creator analysis submits one renamed plain-text source without JSON persistence", async () => {
  const calls = [];
  const fetcher = async (input, init = {}) => {
    calls.push({ input, init });
    return { ok: true, json: async () => ({ analyzerVersion: "creator-analysis-v1", canImport: false, constructs: [], findings: [] }) };
  };
  const file = new File(["form Orders"], "customer-secret-name.ds", { type: "application/octet-stream" });
  const report = await analyzeCreatorExport(file, fetcher);
  assert.equal(report.canImport, false);
  assert.equal(calls[0].input, "/api/creator-analysis");
  assert.equal(calls[0].init.method, "POST");
  assert.equal(calls[0].init.credentials, "include");
  assert.equal(calls[0].init.headers, undefined);
  assert.equal(calls[0].init.body instanceof FormData, true);
  assert.equal(calls[0].init.body.get("source").name, "creator-export.ds");
  assert.equal(calls[0].init.body.get("source").type, "text/plain");
});

test("Creator analysis uses a platform-authored fallback for empty authorization responses", async () => {
  const file = new File(["form Orders"], "source.ds", { type: "application/octet-stream" });
  const fetcher = async () => ({ ok: false, status: 401, json: async () => { throw new SyntaxError("Unexpected end of JSON input"); } });
  await assert.rejects(() => analyzeCreatorExport(file, fetcher), { message: "Creator export analysis failed." });
});

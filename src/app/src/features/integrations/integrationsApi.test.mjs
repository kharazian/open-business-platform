import assert from "node:assert/strict";
import { test } from "vitest";
import * as api from "./api.ts";
import { filterIntegrationLogs, isIntegrationLogRetryEligible } from "./operations.ts";

test("integrations API client manages API keys and retry requests", async () => {
  const calls = [];
  const fetcher = async (input, init = {}) => {
    calls.push({ input, init });

    if (input === "/api/integrations/api-keys" && init.method === "GET") {
      return { ok: true, json: async () => ({ items: [{ id: "key-1", name: "Payroll", integrationKey: "payroll", scopes: [] }] }) };
    }

    if (input === "/api/integrations/api-keys/key-1/revoke") return { ok: true, json: async () => ({ id: "key-1", revokedAt: "2026-06-10T12:00:00Z" }) };
    if (input === "/api/integrations/api-keys/key-1/rotate") return { ok: true, json: async () => ({ apiKey: { id: "key-1" }, rawKey: "obp_sk_new.secret" }) };
    if (input === "/api/integrations/logs/log-1/retry") return { ok: true, json: async () => ({ id: "log-1", retryRequestedAt: "2026-06-10T12:00:00Z" }) };

    return { ok: true, json: async () => ({ apiKey: { id: "key-new", keyPrefix: "obp_sk_prefix" }, rawKey: "obp_sk_prefix.secret" }) };
  };

  const keys = await api.listIntegrationApiKeys(fetcher);
  const created = await api.createIntegrationApiKey({ name: "Warehouse", integrationKey: "warehouse", scopes: ["integrations.records.read"], isActive: true }, fetcher);
  const revoked = await api.revokeIntegrationApiKey("key-1", { reason: "retired", concurrencyStamp: "stamp" }, fetcher);
  const rotated = await api.rotateIntegrationApiKey("key-1", { concurrencyStamp: "stamp" }, fetcher);
  const retried = await api.requestIntegrationLogRetry("log-1", { reason: "manual retry" }, fetcher);

  assert.equal(keys[0].name, "Payroll");
  assert.equal(created.rawKey, "obp_sk_prefix.secret");
  assert.equal(revoked.id, "key-1");
  assert.equal(rotated.rawKey, "obp_sk_new.secret");
  assert.equal(retried.id, "log-1");
  assert.equal(calls[1].input, "/api/integrations/api-keys");
  assert.equal(calls[1].init.method, "POST");
  assert.equal(calls[2].input, "/api/integrations/api-keys/key-1/revoke");
  assert.equal(calls[3].input, "/api/integrations/api-keys/key-1/rotate");
  assert.equal(calls[4].input, "/api/integrations/logs/log-1/retry");
});

test("integration operations filter logs and identify retryable failures", () => {
  const logs = [
    { id: "1", direction: "inbound", integrationType: "api", status: "succeeded", sourceType: "PublicRecordApi", isRetryable: false },
    { id: "2", direction: "outbound", integrationType: "webhook", status: "failed", sourceType: "Trigger", isRetryable: true },
    { id: "3", direction: "outbound", integrationType: "export", status: "failed", sourceType: "ExternalExportJob", isRetryable: false }
  ];

  assert.deepEqual(filterIntegrationLogs(logs, { direction: "outbound", status: "failed", type: "webhook", source: "trigger" }).map((log) => log.id), ["2"]);
  assert.equal(isIntegrationLogRetryEligible(logs[1]), true);
  assert.equal(isIntegrationLogRetryEligible(logs[2]), false);
});

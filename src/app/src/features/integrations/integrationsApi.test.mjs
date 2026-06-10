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
    if (input === "/api/integrations/logs/log-1/retry-request") return { ok: true, json: async () => ({ id: "log-1", retryRequestedAt: "2026-06-10T12:00:00Z" }) };

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
  assert.equal(calls[4].input, "/api/integrations/logs/log-1/retry-request");
});

test("integrations API client manages webhook listeners, imports, and exports", async () => {
  const calls = [];
  const fetcher = async (input, init = {}) => {
    calls.push({ input, init });

    if (input === "/api/integrations/webhooks" && init.method === "GET") {
      return { ok: true, json: async () => ({ items: [{ id: "listener-1", listenerKey: "employee-created" }] }) };
    }

    if (input === "/api/integrations/webhooks" && init.method === "POST") {
      return { ok: true, json: async () => ({ listener: { id: "listener-2", listenerKey: "employee-upsert" }, rawSecret: "obp_wh_secret" }) };
    }

    if (input === "/api/integrations/webhooks/listener-1" && init.method === "PUT") {
      return { ok: true, json: async () => ({ id: "listener-1", isActive: false }) };
    }

    if (input === "/api/integrations/webhooks/listener-1/rotate-secret" && init.method === "POST") {
      return { ok: true, json: async () => ({ listener: { id: "listener-1" }, rawSecret: "obp_wh_new" }) };
    }

    if (input === "/api/integrations/imports" && init.method === "GET") {
      return { ok: true, json: async () => ({ items: [{ id: "import-1", status: "succeeded" }] }) };
    }

    if (input === "/api/integrations/imports" && init.method === "POST") {
      return { ok: true, json: async () => ({ id: "import-2", status: "succeeded" }) };
    }

    if (input === "/api/integrations/exports" && init.method === "GET") {
      return { ok: true, json: async () => ({ items: [{ id: "export-1", status: "succeeded" }] }) };
    }

    if (input === "/api/integrations/exports" && init.method === "POST") {
      return { ok: true, json: async () => ({ id: "export-2", status: "succeeded" }) };
    }

    return { ok: false, json: async () => ({ message: `Unexpected ${init.method} ${input}` }) };
  };

  const listeners = await api.listIncomingWebhookListeners(fetcher);
  const createdListener = await api.createIncomingWebhookListener({
    name: "Employee upsert",
    listenerKey: "employee-upsert",
    targetFormId: "form-1",
    action: "create",
    authMode: "listener_secret",
    mapping: { fieldMappings: [] },
    isActive: true
  }, fetcher);
  const updatedListener = await api.updateIncomingWebhookListener("listener-1", {
    name: "Employee created",
    listenerKey: "employee-created",
    targetFormId: "form-1",
    action: "create",
    authMode: "api_key",
    mapping: { fieldMappings: [] },
    isActive: false
  }, fetcher);
  const rotatedSecret = await api.rotateIncomingWebhookListenerSecret("listener-1", fetcher);
  const imports = await api.listRecordImportJobs(fetcher);
  const createdImport = await api.createRecordImportJob({
    formId: "form-1",
    integrationKey: "import-test",
    fileName: "records.csv",
    csvContent: "email\njane@example.test",
    mapping: { fieldMappings: [{ csvHeader: "email", targetFieldId: "email" }] }
  }, fetcher);
  const exports = await api.listExternalExportJobs(fetcher);
  const createdExport = await api.createExternalExportJob({
    sourceType: "form_records",
    format: "json",
    integrationKey: "export-test",
    formId: "form-1"
  }, fetcher);

  assert.equal(listeners[0].listenerKey, "employee-created");
  assert.equal(createdListener.rawSecret, "obp_wh_secret");
  assert.equal(updatedListener.isActive, false);
  assert.equal(rotatedSecret.rawSecret, "obp_wh_new");
  assert.equal(imports[0].id, "import-1");
  assert.equal(createdImport.id, "import-2");
  assert.equal(exports[0].id, "export-1");
  assert.equal(createdExport.id, "export-2");
  assert.deepEqual(calls.map((call) => `${call.init.method} ${call.input}`), [
    "GET /api/integrations/webhooks",
    "POST /api/integrations/webhooks",
    "PUT /api/integrations/webhooks/listener-1",
    "POST /api/integrations/webhooks/listener-1/rotate-secret",
    "GET /api/integrations/imports",
    "POST /api/integrations/imports",
    "GET /api/integrations/exports",
    "POST /api/integrations/exports"
  ]);
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

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

test("integrations API client manages secret-safe connector configs", async () => {
  const calls = [];
  const fetcher = async (input, init = {}) => {
    calls.push({ input, init });

    if (input === "/api/integrations/connectors" && init.method === "GET") {
      return { ok: true, json: async () => ({ items: [{ id: "connector-1", connectorKey: "warehouse-sftp", configuredSecretNames: ["password"] }] }) };
    }

    if (input === "/api/integrations/connectors" && init.method === "POST") {
      return { ok: true, json: async () => ({ id: "connector-2", connectorKey: "erp-api", configuredSecretNames: ["apiToken"] }) };
    }

    if (input === "/api/integrations/connectors/connector-1" && init.method === "PUT") {
      return { ok: true, json: async () => ({ id: "connector-1", isActive: false, configuredSecretNames: ["password"] }) };
    }

    return { ok: false, json: async () => ({ message: `Unexpected ${init.method} ${input}` }) };
  };

  const connectors = await api.listIntegrationConnectors(fetcher);
  const created = await api.createIntegrationConnector({
    name: "ERP API",
    connectorKey: "erp-api",
    type: "vendor_api",
    config: { baseUrl: "https://api.example.test", apiToken: "raw-token" },
    secrets: { apiToken: "raw-token" },
    isActive: true
  }, fetcher);
  const updated = await api.updateIntegrationConnector("connector-1", {
    name: "Warehouse SFTP",
    connectorKey: "warehouse-sftp",
    type: "sftp",
    config: { host: "sftp.example.test" },
    secrets: null,
    isActive: false,
    concurrencyStamp: "stamp"
  }, fetcher);

  assert.equal(connectors[0].connectorKey, "warehouse-sftp");
  assert.deepEqual(created.configuredSecretNames, ["apiToken"]);
  assert.equal(updated.isActive, false);
  assert.deepEqual(calls.map((call) => `${call.init.method} ${call.input}`), [
    "GET /api/integrations/connectors",
    "POST /api/integrations/connectors",
    "PUT /api/integrations/connectors/connector-1"
  ]);
  assert.equal(JSON.parse(calls[1].init.body).secrets.apiToken, "raw-token");
});

test("integrations API client downloads protected export artifacts", async () => {
  const calls = [];
  const blob = new Blob(["email\njane@example.test"], { type: "text/csv" });
  const fetcher = async (input, init = {}) => {
    calls.push({ input, init });
    return { ok: true, blob: async () => blob };
  };

  const downloaded = await api.downloadExternalExportArtifact("export-1", fetcher);

  assert.equal(downloaded, blob);
  assert.deepEqual(calls.map((call) => `${call.init.method} ${call.input}`), [
    "GET /api/integrations/exports/export-1/artifact"
  ]);
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

test("processing job API client preserves bounded typed requests and run ancestry", async () => {
  const calls = [];
  const fetcher = async (input, init = {}) => {
    calls.push({ input, init });
    if (input.startsWith("/api/processing-jobs?page=")) return { ok: true, json: async () => ({ items: [{ id: "job-1", name: "Nightly export" }], page: 1, pageSize: 25, totalCount: 1 }) };
    if (input === "/api/processing-jobs" && init.method === "POST") return { ok: true, json: async () => ({ id: "job-1", name: "Nightly export" }) };
    if (input === "/api/processing-jobs/job-1" && init.method === "GET") return { ok: true, json: async () => ({ id: "job-1", name: "Nightly export", concurrencyStamp: "stamp-1" }) };
    if (input === "/api/processing-jobs/job-1" && init.method === "PUT") return { ok: true, json: async () => ({ id: "job-1", name: "Updated export", concurrencyStamp: "stamp-2" }) };
    if (input.includes("/runs?page=")) return { ok: true, json: async () => ({ items: [{ id: "run-1", retrySourceRunId: null }], page: 1, pageSize: 25, totalCount: 1 }) };
    if (input.endsWith("/runs") && init.method === "POST") return { ok: true, json: async () => ({ id: "run-2", status: "pending" }) };
    if (input.endsWith("/retry")) return { ok: true, json: async () => ({ id: "run-3", retrySourceRunId: "run-1", attempt: 2 }) };
    return { ok: false, json: async () => ({ message: "Unexpected call" }) };
  };

  const page = await api.listProcessingJobs(1, 25, fetcher);
  await api.createProcessingJob({
    name: "Nightly export", kind: "record_export", isEnabled: true,
    config: { formId: "form-1", integrationKey: "nightly", sourceType: "form_records", format: "csv", maxRows: 5000 },
    schedule: { kind: "daily", timeZone: "UTC", startAt: "2026-08-20T00:00:00Z", interval: 1 },
    retryPolicy: { isEnabled: true, maxAttempts: 3, delaySeconds: 300 }
  }, fetcher);
  const detail = await api.getProcessingJob("job-1", fetcher);
  const updated = await api.updateProcessingJob("job-1", {
    name: "Updated export",
    config: { formId: "form-1", integrationKey: "nightly", sourceType: "form_records", format: "csv", maxRows: 5000 },
    schedule: null,
    retryPolicy: { isEnabled: false, maxAttempts: 1, delaySeconds: 300 },
    concurrencyStamp: detail.concurrencyStamp
  }, fetcher);
  const runs = await api.listProcessingJobRuns("job-1", 1, 25, fetcher);
  await api.queueProcessingJob("job-1", null, null, fetcher);
  const retry = await api.retryProcessingJobRun("job-1", "run-1", fetcher);

  assert.equal(page.totalCount, 1);
  assert.equal(updated.name, "Updated export");
  assert.equal(runs.items[0].id, "run-1");
  assert.equal(retry.retrySourceRunId, "run-1");
  assert.equal(JSON.parse(calls[1].init.body).config.maxRows, 5000);
  assert.deepEqual(calls.map((call) => `${call.init.method} ${call.input}`), [
    "GET /api/processing-jobs?page=1&pageSize=25",
    "POST /api/processing-jobs",
    "GET /api/processing-jobs/job-1",
    "PUT /api/processing-jobs/job-1",
    "GET /api/processing-jobs/job-1/runs?page=1&pageSize=25",
    "POST /api/processing-jobs/job-1/runs",
    "POST /api/processing-jobs/job-1/runs/run-1/retry"
  ]);
});

import assert from "node:assert/strict";
import { test } from "vitest";
import * as api from "./api.ts";

test("trigger API client maps management requests, logs, and validation errors", async () => {
  const calls = [];
  const createRequest = {
    name: "Route HR submissions",
    description: "Move HR submissions to review.",
    eventName: "record.created",
    conditions: {
      mode: "all",
      conditions: [{ type: "field_equals", fieldId: "department", value: "HR" }]
    },
    actions: [
      { id: "audit-1", type: "write_audit_entry", message: "HR trigger matched." },
      { id: "status-1", type: "change_status", status: "in_review" }
    ],
    isEnabled: true
  };
  const fetcher = async (input, init = {}) => {
    calls.push({ input, init });

    if (input === "/api/forms/form-1/triggers" && init.method === "GET") {
      return {
        ok: true,
        json: async () => ({
          items: [
            {
              id: "trigger-1",
              formId: "form-1",
              name: "Route HR submissions",
              description: "Move HR submissions to review.",
              eventName: "record.created",
              isEnabled: true,
              conditionCount: 1,
              actionCount: 2,
              concurrencyStamp: "trigger-stamp-1",
              createdAt: "2026-06-02T12:00:00.000Z",
              createdById: null,
              updatedAt: null,
              updatedById: null
            }
          ]
        })
      };
    }

    if (input === "/api/forms/form-1/triggers" && init.method === "POST") {
      const body = JSON.parse(init.body);

      return {
        ok: true,
        json: async () => ({
          id: "trigger-2",
          formId: "form-1",
          concurrencyStamp: "trigger-stamp-2",
          createdAt: "2026-06-02T12:05:00.000Z",
          createdById: null,
          updatedAt: null,
          updatedById: null,
          ...body
        })
      };
    }

    if (input === "/api/triggers/trigger-2" && init.method === "GET") {
      return {
        ok: true,
        json: async () => ({
          id: "trigger-2",
          formId: "form-1",
          concurrencyStamp: "trigger-stamp-2",
          createdAt: "2026-06-02T12:05:00.000Z",
          createdById: null,
          updatedAt: null,
          updatedById: null,
          ...createRequest
        })
      };
    }

    if (input === "/api/triggers/trigger-2" && init.method === "PUT") {
      const body = JSON.parse(init.body);

      return {
        ok: true,
        json: async () => ({
          id: "trigger-2",
          formId: "form-1",
          ...body,
          concurrencyStamp: "trigger-stamp-3",
          createdAt: "2026-06-02T12:05:00.000Z",
          createdById: null,
          updatedAt: "2026-06-02T12:10:00.000Z",
          updatedById: null
        })
      };
    }

    if (input === "/api/triggers/trigger-2/logs" && init.method === "GET") {
      return {
        ok: true,
        json: async () => ({
          items: [
            {
              id: "log-1",
              triggerId: "trigger-2",
              formId: "form-1",
              eventName: "record.created",
              entityType: "Record",
              entityId: "record-1",
              status: "success",
              input: { recordId: "record-1" },
              result: { actions: [{ actionId: "audit-1", type: "write_audit_entry" }] },
              errorMessage: null,
              startedAt: "2026-06-02T12:20:00.000Z",
              completedAt: "2026-06-02T12:20:01.000Z",
              createdAt: "2026-06-02T12:20:00.000Z"
            }
          ]
        })
      };
    }

    if (input === "/api/triggers/trigger-2/logs/log-1/retry" && init.method === "POST") {
      return {
        ok: true,
        json: async () => ({
          id: "log-2",
          triggerId: "trigger-2",
          formId: "form-1",
          eventName: "record.created",
          entityType: "Record",
          entityId: "record-1",
          status: "success",
          retryOfLogId: "log-1",
          input: {
            recordId: "record-1",
            retry: { sourceLogId: "log-1" }
          },
          result: {
            retry: { sourceLogId: "log-1" },
            actions: [{ actionId: "audit-1", type: "write_audit_entry" }]
          },
          errorMessage: null,
          startedAt: "2026-06-02T12:21:00.000Z",
          completedAt: "2026-06-02T12:21:01.000Z",
          createdAt: "2026-06-02T12:21:00.000Z"
        })
      };
    }

    return { ok: false, json: async () => ({ message: "Unexpected request." }) };
  };

  const summaries = await api.listTriggers("form-1", fetcher);
  const created = await api.createTrigger("form-1", createRequest, fetcher);
  const detail = await api.getTrigger("trigger-2", fetcher);
  const updated = await api.updateTrigger("trigger-2", { ...createRequest, isEnabled: false, concurrencyStamp: "trigger-stamp-2" }, fetcher);
  const logs = await api.listTriggerLogs("trigger-2", fetcher);
  const retriedLog = await api.retryTriggerLog("trigger-2", "log-1", fetcher);

  assert.equal(summaries[0].name, "Route HR submissions");
  assert.equal(summaries[0].conditionCount, 1);
  assert.equal(created.actions[1].status, "in_review");
  assert.equal(detail.conditions.conditions[0].fieldId, "department");
  assert.equal(updated.isEnabled, false);
  assert.equal(updated.concurrencyStamp, "trigger-stamp-3");
  assert.equal(logs[0].result.actions[0].type, "write_audit_entry");
  assert.equal(retriedLog.retryOfLogId, "log-1");
  assert.equal(retriedLog.result.retry.sourceLogId, "log-1");
  assert.equal(calls[0].input, "/api/forms/form-1/triggers");
  assert.equal(calls[0].init.method, "GET");
  assert.equal(calls[0].init.credentials, "include");
  assert.equal(calls[1].input, "/api/forms/form-1/triggers");
  assert.equal(calls[1].init.method, "POST");
  assert.equal(calls[1].init.credentials, "include");
  assert.equal(calls[1].init.headers["Content-Type"], "application/json");
  assert.deepEqual(JSON.parse(calls[1].init.body), createRequest);
  assert.equal(calls[2].input, "/api/triggers/trigger-2");
  assert.equal(calls[2].init.method, "GET");
  assert.equal(calls[2].init.credentials, "include");
  assert.equal(calls[3].input, "/api/triggers/trigger-2");
  assert.equal(calls[3].init.method, "PUT");
  assert.equal(calls[3].init.credentials, "include");
  assert.equal(calls[3].init.headers["Content-Type"], "application/json");
  assert.equal(JSON.parse(calls[3].init.body).concurrencyStamp, "trigger-stamp-2");
  assert.equal(calls[4].input, "/api/triggers/trigger-2/logs");
  assert.equal(calls[4].init.method, "GET");
  assert.equal(calls[4].init.credentials, "include");
  assert.equal(calls[5].input, "/api/triggers/trigger-2/logs/log-1/retry");
  assert.equal(calls[5].init.method, "POST");
  assert.equal(calls[5].init.credentials, "include");

  await assert.rejects(
    () => api.listTriggers("form-1", async () => ({ ok: true, json: async () => ({}) })),
    /API response did not include an items collection/
  );

  await assert.rejects(
    () =>
      api.createTrigger("form-1", createRequest, async () => ({
        ok: false,
        json: async () => ({
          message: "Trigger definition is invalid.",
          errors: [{ path: "actions[0].message", code: "trigger.action.message_required", message: "Audit action message is required." }]
        })
      })),
    (error) => {
      assert.equal(error.name, "TriggersApiError");
      assert.equal(error.message, "Trigger definition is invalid.");
      assert.equal(error.errors[0].path, "actions[0].message");
      return true;
    }
  );
});

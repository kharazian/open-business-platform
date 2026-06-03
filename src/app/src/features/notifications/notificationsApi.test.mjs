import assert from "node:assert/strict";
import { test } from "vitest";
import * as api from "./api.ts";

test("notification API client maps inbox and read-state endpoints", async () => {
  const calls = [];
  const fetcher = async (input, init = {}) => {
    calls.push({ input, init });

    if (input === "/api/notifications" && init.method === "GET") {
      return {
        ok: true,
        json: async () => ({
          items: [
            {
              id: "notification-1",
              title: "Record needs review",
              body: "Open the record.",
              sourceType: "Record",
              sourceId: "record-1",
              triggerId: "trigger-1",
              triggerLogId: "log-1",
              actionId: "notify-1",
              metadata: { recordId: "record-1" },
              readAt: null,
              createdAt: "2026-06-02T17:00:00.000Z"
            }
          ]
        })
      };
    }

    if (input === "/api/notifications/unread-count" && init.method === "GET") {
      return { ok: true, json: async () => ({ unreadCount: 1 }) };
    }

    if (input === "/api/notifications/notification-1/read" && init.method === "POST") {
      return {
        ok: true,
        json: async () => ({
          id: "notification-1",
          title: "Record needs review",
          body: "Open the record.",
          sourceType: "Record",
          sourceId: "record-1",
          triggerId: "trigger-1",
          triggerLogId: "log-1",
          actionId: "notify-1",
          metadata: { recordId: "record-1" },
          readAt: "2026-06-02T17:05:00.000Z",
          createdAt: "2026-06-02T17:00:00.000Z"
        })
      };
    }

    if (input === "/api/notifications/read-all" && init.method === "POST") {
      return { ok: true, json: async () => ({ unreadCount: 0 }) };
    }

    if (input === "/api/notifications/preferences" && init.method === "GET") {
      return { ok: true, json: async () => ({ inAppEnabled: true, showUnreadBadge: true, updatedAt: null }) };
    }

    if (input === "/api/notifications/preferences" && init.method === "PUT") {
      return { ok: true, json: async () => ({ inAppEnabled: false, showUnreadBadge: true, updatedAt: "2026-06-03T15:00:00.000Z" }) };
    }

    return { ok: false, json: async () => ({ message: "Unexpected request." }) };
  };

  const notifications = await api.listNotifications(fetcher);
  const unreadCount = await api.getUnreadNotificationCount(fetcher);
  const readNotification = await api.markNotificationRead("notification-1", fetcher);
  const readAllCount = await api.markAllNotificationsRead(fetcher);
  const preferences = await api.getNotificationPreferences(fetcher);
  const updatedPreferences = await api.updateNotificationPreferences({ inAppEnabled: false, showUnreadBadge: true }, fetcher);

  assert.equal(notifications[0].id, "notification-1");
  assert.equal(notifications[0].readAt, null);
  assert.equal(notifications[0].metadata.recordId, "record-1");
  assert.equal(unreadCount.unreadCount, 1);
  assert.equal(readNotification.readAt, "2026-06-02T17:05:00.000Z");
  assert.equal(readAllCount.unreadCount, 0);
  assert.equal(preferences.inAppEnabled, true);
  assert.equal(preferences.showUnreadBadge, true);
  assert.equal(updatedPreferences.inAppEnabled, false);
  assert.equal(updatedPreferences.updatedAt, "2026-06-03T15:00:00.000Z");
  assert.equal(calls[0].input, "/api/notifications");
  assert.equal(calls[0].init.method, "GET");
  assert.equal(calls[0].init.credentials, "include");
  assert.equal(calls[1].input, "/api/notifications/unread-count");
  assert.equal(calls[1].init.method, "GET");
  assert.equal(calls[1].init.credentials, "include");
  assert.equal(calls[2].input, "/api/notifications/notification-1/read");
  assert.equal(calls[2].init.method, "POST");
  assert.equal(calls[2].init.credentials, "include");
  assert.equal(calls[3].input, "/api/notifications/read-all");
  assert.equal(calls[3].init.method, "POST");
  assert.equal(calls[3].init.credentials, "include");
  assert.equal(calls[4].input, "/api/notifications/preferences");
  assert.equal(calls[4].init.method, "GET");
  assert.equal(calls[4].init.credentials, "include");
  assert.equal(calls[5].input, "/api/notifications/preferences");
  assert.equal(calls[5].init.method, "PUT");
  assert.equal(calls[5].init.credentials, "include");
  assert.equal(calls[5].init.headers["Content-Type"], "application/json");
  assert.equal(calls[5].init.body, JSON.stringify({ inAppEnabled: false, showUnreadBadge: true }));

  await assert.rejects(
    () => api.listNotifications(async () => ({ ok: true, json: async () => ({}) })),
    /API response did not include an items collection/
  );

  await assert.rejects(
    () => api.markNotificationRead("notification-2", async () => ({ ok: false, json: async () => ({ message: "Notification was not found." }) })),
    (error) => {
      assert.equal(error.name, "NotificationsApiError");
      assert.equal(error.message, "Notification was not found.");
      return true;
    }
  );
});

import type {
  AppNotification,
  NotificationPreferences,
  NotificationUnreadCount,
  UpdateNotificationPreferencesRequest
} from "./types";

type ApiFetchResponse = {
  ok: boolean;
  status?: number;
  json: () => Promise<unknown>;
};

export type NotificationsFetcher = (input: string, init?: RequestInit) => Promise<ApiFetchResponse>;

export class NotificationsApiError extends Error {
  constructor(message: string) {
    super(message);
    this.name = "NotificationsApiError";
  }
}

const defaultFetcher: NotificationsFetcher = (input, init) => fetch(input, init);

export async function listNotifications(fetcher: NotificationsFetcher = defaultFetcher): Promise<AppNotification[]> {
  return requestItems<AppNotification>("/api/notifications", { method: "GET", credentials: "include" }, fetcher);
}

export async function getUnreadNotificationCount(fetcher: NotificationsFetcher = defaultFetcher): Promise<NotificationUnreadCount> {
  return requestJson<NotificationUnreadCount>("/api/notifications/unread-count", { method: "GET", credentials: "include" }, fetcher);
}

export async function getNotificationPreferences(fetcher: NotificationsFetcher = defaultFetcher): Promise<NotificationPreferences> {
  return requestJson<NotificationPreferences>("/api/notifications/preferences", { method: "GET", credentials: "include" }, fetcher);
}

export async function updateNotificationPreferences(
  request: UpdateNotificationPreferencesRequest,
  fetcher: NotificationsFetcher = defaultFetcher
): Promise<NotificationPreferences> {
  return requestJson<NotificationPreferences>(
    "/api/notifications/preferences",
    {
      method: "PUT",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request)
    },
    fetcher
  );
}

export async function markNotificationRead(
  notificationId: string,
  fetcher: NotificationsFetcher = defaultFetcher
): Promise<AppNotification> {
  return requestJson<AppNotification>(
    `/api/notifications/${encodeURIComponent(notificationId)}/read`,
    { method: "POST", credentials: "include" },
    fetcher
  );
}

export async function markAllNotificationsRead(fetcher: NotificationsFetcher = defaultFetcher): Promise<NotificationUnreadCount> {
  return requestJson<NotificationUnreadCount>("/api/notifications/read-all", { method: "POST", credentials: "include" }, fetcher);
}

async function requestItems<T>(input: string, init: RequestInit, fetcher: NotificationsFetcher): Promise<T[]> {
  const body = await requestJson<unknown>(input, init, fetcher);

  if (!isRecord(body) || !Array.isArray(body.items)) {
    throw new NotificationsApiError("API response did not include an items collection.");
  }

  return body.items as T[];
}

async function requestJson<T>(input: string, init: RequestInit, fetcher: NotificationsFetcher): Promise<T> {
  const response = await fetcher(input, init);
  const body = await readJson(response);

  if (!response.ok) {
    throw new NotificationsApiError(getErrorMessageFromBody(body));
  }

  return body as T;
}

async function readJson(response: ApiFetchResponse): Promise<unknown> {
  try {
    return await response.json();
  } catch {
    return null;
  }
}

function getErrorMessageFromBody(body: unknown): string {
  if (isRecord(body) && typeof body.message === "string" && body.message.trim().length > 0) {
    return body.message;
  }

  return "Notifications API request failed.";
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

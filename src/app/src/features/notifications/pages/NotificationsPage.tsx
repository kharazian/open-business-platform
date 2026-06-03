import { useEffect, useMemo, useState } from "react";
import { Bell, Check, CheckCheck, RefreshCw } from "lucide-react";
import { Alert } from "../../../components/ui/Alert";
import { Badge } from "../../../components/ui/Badge";
import { Button } from "../../../components/ui/Button";
import { Card, CardContent } from "../../../components/ui/Card";
import { EmptyState } from "../../../components/ui/EmptyState";
import { PageHeader } from "../../../components/ui/PageHeader";
import { Switch } from "../../../components/ui/Switch";
import {
  getNotificationPreferences,
  getUnreadNotificationCount,
  listNotifications,
  markAllNotificationsRead,
  markNotificationRead,
  updateNotificationPreferences
} from "../api";
import { dispatchNotificationsChanged } from "../events";
import type { AppNotification, NotificationPreferences } from "../types";

const defaultNotificationPreferences: NotificationPreferences = {
  inAppEnabled: true,
  showUnreadBadge: true,
  updatedAt: null
};

export function NotificationsPage() {
  const [notifications, setNotifications] = useState<AppNotification[]>([]);
  const [preferences, setPreferences] = useState<NotificationPreferences | null>(null);
  const [unreadCount, setUnreadCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [markingId, setMarkingId] = useState<string | null>(null);
  const [markingAll, setMarkingAll] = useState(false);
  const [savingPreferences, setSavingPreferences] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [preferenceError, setPreferenceError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  useEffect(() => {
    void loadInbox();
  }, []);

  const unreadNotifications = useMemo(() => notifications.filter((notification) => !notification.readAt), [notifications]);

  async function loadInbox() {
    setLoading(true);
    setError(null);

    try {
      const [items, count, loadedPreferences] = await Promise.all([
        listNotifications(),
        getUnreadNotificationCount(),
        getNotificationPreferences()
      ]);
      setNotifications(items);
      setUnreadCount(count.unreadCount);
      setPreferences(loadedPreferences);
    } catch (caught) {
      setError(getErrorMessage(caught));
    } finally {
      setLoading(false);
    }
  }

  async function handleRefresh() {
    setNotice(null);
    await loadInbox();
    dispatchNotificationsChanged();
  }

  async function handleMarkRead(notificationId: string) {
    setMarkingId(notificationId);
    setError(null);
    setNotice(null);

    try {
      const updated = await markNotificationRead(notificationId);
      setNotifications((current) => current.map((notification) => (notification.id === updated.id ? updated : notification)));
      const count = await getUnreadNotificationCount();
      setUnreadCount(count.unreadCount);
      dispatchNotificationsChanged();
      setNotice("Notification marked as read.");
    } catch (caught) {
      setError(getErrorMessage(caught));
    } finally {
      setMarkingId(null);
    }
  }

  async function handleMarkAllRead() {
    setMarkingAll(true);
    setError(null);
    setNotice(null);

    try {
      const count = await markAllNotificationsRead();
      setNotifications((current) =>
        current.map((notification) => ({
          ...notification,
          readAt: notification.readAt ?? new Date().toISOString()
        }))
      );
      setUnreadCount(count.unreadCount);
      dispatchNotificationsChanged();
      setNotice("All notifications marked as read.");
    } catch (caught) {
      setError(getErrorMessage(caught));
    } finally {
      setMarkingAll(false);
    }
  }

  async function handlePreferenceChange(nextPreferences: NotificationPreferences) {
    if (!preferences) {
      return;
    }

    setSavingPreferences(true);
    setPreferenceError(null);
    setNotice(null);

    try {
      const updated = await updateNotificationPreferences({
        inAppEnabled: nextPreferences.inAppEnabled,
        showUnreadBadge: nextPreferences.showUnreadBadge
      });
      setPreferences(updated);
      dispatchNotificationsChanged();
      setNotice("Notification preferences saved.");
    } catch (caught) {
      setPreferenceError(getErrorMessage(caught));
    } finally {
      setSavingPreferences(false);
    }
  }

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Inbox"
        title="Notifications"
        actions={
          <>
            <Badge tone={unreadCount > 0 ? "info" : "default"}>{unreadCount} unread</Badge>
            <Button variant="outline" size="icon" onClick={handleRefresh} disabled={loading} aria-label="Refresh notifications" title="Refresh">
              <RefreshCw className="size-4" />
            </Button>
            <Button variant="secondary" onClick={handleMarkAllRead} disabled={markingAll || unreadNotifications.length === 0}>
              <CheckCheck className="size-4" />
              {markingAll ? "Saving..." : "Mark all read"}
            </Button>
          </>
        }
      />

      {error ? <Alert title="Notification error">{error}</Alert> : null}
      {notice ? <Alert title="Notification updated">{notice}</Alert> : null}

      <Card title="Preferences">
        <CardContent className="grid gap-3 p-0 md:grid-cols-2">
          <Switch
            checked={preferences?.inAppEnabled ?? defaultNotificationPreferences.inAppEnabled}
            disabled={savingPreferences || !preferences}
            label="In-app notifications"
            description="Receive trigger-created inbox items."
            onChange={(event) =>
              void handlePreferenceChange({
                ...(preferences ?? defaultNotificationPreferences),
                inAppEnabled: event.currentTarget.checked
              })
            }
          />
          <Switch
            checked={preferences?.showUnreadBadge ?? defaultNotificationPreferences.showUnreadBadge}
            disabled={savingPreferences || !preferences}
            label="Unread badge"
            description="Show unread count in navigation."
            onChange={(event) =>
              void handlePreferenceChange({
                ...(preferences ?? defaultNotificationPreferences),
                showUnreadBadge: event.currentTarget.checked
              })
            }
          />
          {preferenceError ? (
            <div className="md:col-span-2">
              <Alert title="Preference error">{preferenceError}</Alert>
            </div>
          ) : null}
        </CardContent>
      </Card>

      {loading && notifications.length === 0 ? (
        <div className="grid gap-3">
          {[0, 1, 2].map((item) => (
            <div className="h-28 animate-pulse rounded-xl border border-border bg-muted/50" key={item} />
          ))}
        </div>
      ) : notifications.length === 0 ? (
        <EmptyState
          title="No notifications"
          description="New notification activity will appear here."
          action={
            <Button variant="outline" onClick={handleRefresh}>
              <RefreshCw className="size-4" />
              Refresh
            </Button>
          }
        />
      ) : (
        <div className="grid gap-3">
          {notifications.map((notification) => (
            <NotificationRow
              key={notification.id}
              notification={notification}
              marking={markingId === notification.id}
              onMarkRead={handleMarkRead}
            />
          ))}
        </div>
      )}
    </div>
  );
}

function NotificationRow({
  marking,
  notification,
  onMarkRead
}: {
  marking: boolean;
  notification: AppNotification;
  onMarkRead: (notificationId: string) => void;
}) {
  const unread = !notification.readAt;

  return (
    <Card className={unread ? "border-primary/35 bg-primary-soft/40" : undefined}>
      <CardContent className="p-4">
        <div className="flex flex-col gap-4 md:flex-row md:items-start md:justify-between">
          <div className="min-w-0 flex-1">
            <div className="flex flex-wrap items-center gap-2">
              <span className="grid size-9 shrink-0 place-items-center rounded-xl border border-border bg-card text-primary">
                <Bell className="size-4" />
              </span>
              <h2 className="min-w-0 break-words text-base font-bold text-foreground">{notification.title}</h2>
              <Badge tone={unread ? "info" : "default"}>{unread ? "Unread" : "Read"}</Badge>
            </div>
            <p className="mt-3 break-words text-sm leading-6 text-muted-foreground">{notification.body}</p>
            <div className="mt-4 flex flex-wrap gap-2 text-xs font-semibold text-muted-foreground">
              <span>{formatDate(notification.createdAt)}</span>
              <span aria-hidden="true">/</span>
              <span>{formatSource(notification)}</span>
            </div>
          </div>

          <div className="flex shrink-0 flex-wrap gap-2 md:justify-end">
            {unread ? (
              <Button variant="outline" size="sm" onClick={() => onMarkRead(notification.id)} disabled={marking}>
                <Check className="size-4" />
                {marking ? "Saving..." : "Mark read"}
              </Button>
            ) : (
              <Badge tone="success">Read {notification.readAt ? formatDate(notification.readAt) : ""}</Badge>
            )}
          </div>
        </div>
      </CardContent>
    </Card>
  );
}

function formatDate(value: string) {
  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return value;
  }

  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short"
  }).format(date);
}

function formatSource(notification: AppNotification) {
  const sourceType = notification.sourceType.trim() || "System";

  if (!notification.sourceId) {
    return sourceType;
  }

  return `${sourceType} ${notification.sourceId.slice(0, 8)}`;
}

function getErrorMessage(caught: unknown) {
  return caught instanceof Error ? caught.message : "Notifications could not be loaded.";
}

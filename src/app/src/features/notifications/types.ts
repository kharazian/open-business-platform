import type { EntityId } from "../../types/entities";

export type AppNotification = {
  id: EntityId;
  title: string;
  body: string;
  sourceType: string;
  sourceId?: EntityId | null;
  triggerId?: EntityId | null;
  triggerLogId?: EntityId | null;
  actionId?: string | null;
  metadata?: unknown;
  readAt?: string | null;
  createdAt: string;
};

export type NotificationUnreadCount = {
  unreadCount: number;
};

export type NotificationPreferences = {
  inAppEnabled: boolean;
  showUnreadBadge: boolean;
  updatedAt?: string | null;
};

export type UpdateNotificationPreferencesRequest = {
  inAppEnabled: boolean;
  showUnreadBadge: boolean;
};

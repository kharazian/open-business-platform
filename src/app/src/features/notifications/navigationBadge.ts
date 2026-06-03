import type { NavigationItem } from "../../config/appNavigation";

export function formatNotificationBadge(unreadCount: number) {
  if (unreadCount <= 0) {
    return null;
  }

  return unreadCount > 99 ? "99+" : String(unreadCount);
}

export function applyNotificationBadge(
  navigation: NavigationItem[],
  unreadCount: number,
  showUnreadBadge: boolean
): NavigationItem[] {
  const badge = showUnreadBadge ? formatNotificationBadge(unreadCount) : null;
  return navigation.map((item) => applyBadgeToItem(item, badge));
}

function applyBadgeToItem(item: NavigationItem, badge: string | null): NavigationItem {
  const children = item.children?.map((child) => applyBadgeToItem(child, badge));
  const nextBadge = item.path === "/notifications" ? badge ?? undefined : item.badge;

  if (children === item.children && nextBadge === item.badge) {
    return item;
  }

  return {
    ...item,
    badge: nextBadge,
    children
  };
}

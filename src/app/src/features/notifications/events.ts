export const notificationsChangedEventName = "open-business-platform:notifications-changed";

export function dispatchNotificationsChanged() {
  if (typeof window === "undefined") {
    return;
  }

  window.dispatchEvent(new Event(notificationsChangedEventName));
}

export function subscribeToNotificationsChanged(listener: () => void) {
  if (typeof window === "undefined") {
    return () => undefined;
  }

  window.addEventListener(notificationsChangedEventName, listener);
  return () => window.removeEventListener(notificationsChangedEventName, listener);
}

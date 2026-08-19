export const dashboardsChangedEventName = "open-business-platform:dashboards-changed";

export function dispatchDashboardsChanged(): void {
  window.dispatchEvent(new Event(dashboardsChangedEventName));
}

export function subscribeToDashboardsChanged(listener: () => void): () => void {
  window.addEventListener(dashboardsChangedEventName, listener);
  return () => window.removeEventListener(dashboardsChangedEventName, listener);
}

import { Outlet, useNavigate } from "react-router-dom";
import { useEffect, useMemo, useState } from "react";
import { AppShell } from "../components/layout/AppShell";
import { appNavigation } from "../config/appNavigation";
import { useAppTheme } from "../context/AppThemeContext";
import { useAuth } from "../context/AuthContext";
import { getNotificationPreferences, getUnreadNotificationCount } from "../features/notifications/api";
import { applyNotificationBadge } from "../features/notifications/navigationBadge";
import { subscribeToNotificationsChanged } from "../features/notifications/events";
import { filterNavigationByPermissions } from "../platform/moduleRegistry";

type AppLayoutProps = {
  theme: "light" | "dark";
  onThemeToggle: () => void;
};

export function AppLayout({ theme, onThemeToggle }: AppLayoutProps) {
  const { appThemeClassName, appThemeStyle } = useAppTheme();
  const { signOut, user } = useAuth();
  const navigate = useNavigate();
  const [notificationBadgeCount, setNotificationBadgeCount] = useState(0);
  const [showNotificationBadge, setShowNotificationBadge] = useState(false);
  const visibleNavigation = useMemo(
    () => filterNavigationByPermissions(appNavigation, new Set(user?.permissions ?? [])),
    [user?.permissions]
  );
  const visibleNavigationWithBadges = useMemo(
    () => applyNotificationBadge(visibleNavigation, notificationBadgeCount, showNotificationBadge),
    [notificationBadgeCount, showNotificationBadge, visibleNavigation]
  );

  useEffect(() => {
    let active = true;

    async function refreshNotificationBadge() {
      if (!user) {
        setNotificationBadgeCount(0);
        setShowNotificationBadge(false);
        return;
      }

      try {
        const [count, preferences] = await Promise.all([getUnreadNotificationCount(), getNotificationPreferences()]);

        if (!active) {
          return;
        }

        setNotificationBadgeCount(count.unreadCount);
        setShowNotificationBadge(preferences.showUnreadBadge);
      } catch {
        if (!active) {
          return;
        }

        setNotificationBadgeCount(0);
        setShowNotificationBadge(false);
      }
    }

    void refreshNotificationBadge();
    const unsubscribe = subscribeToNotificationsChanged(() => {
      void refreshNotificationBadge();
    });

    return () => {
      active = false;
      unsubscribe();
    };
  }, [user]);

  async function handleSignOut() {
    await signOut();
    navigate("/login", { replace: true });
  }

  return (
    <AppShell
      className={`min-h-screen ${appThemeClassName}`}
      containerClassName="max-w-7xl"
      mode="app"
      navigation={visibleNavigationWithBadges}
      notificationBadgeCount={showNotificationBadge ? notificationBadgeCount : 0}
      onThemeToggle={onThemeToggle}
      style={appThemeStyle}
      theme={theme}
      userEmail={user?.email}
      userMenu={[
        { label: "Profile", to: "/profile" },
        { label: "Settings", to: "/settings" },
        { label: "Sign out", onClick: handleSignOut }
      ]}
      userName={user?.name}
    >
      <Outlet />
    </AppShell>
  );
}

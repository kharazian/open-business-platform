import { Outlet, useNavigate } from "react-router-dom";
import { AppShell } from "../components/layout/AppShell";
import { appNavigation } from "../config/appNavigation";
import { useAppTheme } from "../context/AppThemeContext";
import { useAuth } from "../context/AuthContext";
import { filterNavigationByPermissions } from "../platform/moduleRegistry";

type AppLayoutProps = {
  theme: "light" | "dark";
  onThemeToggle: () => void;
};

export function AppLayout({ theme, onThemeToggle }: AppLayoutProps) {
  const { appThemeClassName, appThemeStyle } = useAppTheme();
  const { signOut, user } = useAuth();
  const navigate = useNavigate();
  const visibleNavigation = filterNavigationByPermissions(appNavigation, new Set(user?.permissions ?? []));

  async function handleSignOut() {
    await signOut();
    navigate("/login", { replace: true });
  }

  return (
    <AppShell
      className={`min-h-screen ${appThemeClassName}`}
      containerClassName="max-w-7xl"
      mode="app"
      navigation={visibleNavigation}
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

import { Outlet } from "react-router-dom";
import { AppShell } from "../components/layout/AppShell";
import { appNavigation } from "../config/appNavigation";
import { useAppTheme } from "../context/AppThemeContext";

type AppLayoutProps = {
  theme: "light" | "dark";
  onThemeToggle: () => void;
};

export function AppLayout({ theme, onThemeToggle }: AppLayoutProps) {
  const { appThemeClassName, appThemeStyle } = useAppTheme();

  return (
    <AppShell
      className={`min-h-screen ${appThemeClassName}`}
      containerClassName="max-w-7xl"
      mode="app"
      navigation={appNavigation}
      onThemeToggle={onThemeToggle}
      style={appThemeStyle}
      theme={theme}
    >
      <Outlet />
    </AppShell>
  );
}

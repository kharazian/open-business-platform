import { useEffect } from "react";
import { Outlet, useLocation } from "react-router-dom";
import { AppShell } from "../components/layout/AppShell";
import { Breadcrumbs } from "../components/layout/Breadcrumbs";
import { shouldShowTopNav, ThemeAppearanceProvider, useThemeAppearance } from "../context/ThemeAppearanceContext";
import { themeNavigation } from "../theme/config/themeNavigation";

type ThemeLayoutProps = {
  theme: "light" | "dark";
};

function ThemeLayoutShell() {
  const { layoutMode, setLayoutMode, setEffectiveLayoutMode, densityClasses, topNavVisibility } = useThemeAppearance();
  const location = useLocation();
  const effectiveMode = location.pathname === "/theme/login" ? "minimal" : layoutMode;

  useEffect(() => {
    setEffectiveLayoutMode(effectiveMode);
  }, [effectiveMode, setEffectiveLayoutMode]);

  return (
    <AppShell
      className="min-h-screen bg-background text-foreground"
      contentBefore={<Breadcrumbs navigation={themeNavigation} rootLabel="Theme" rootPath="/theme" />}
      contentClassName={`flex w-full flex-col ${densityClasses.pageGap}`}
      layout={effectiveMode}
      mainClassName={densityClasses.pagePadding}
      mode="playground"
      navigation={themeNavigation}
      navbarSubtitle="Navigation and design system"
      navbarTitle="Theme playground"
      onSidebarToggle={
        effectiveMode === "hybrid" ? undefined : () => setLayoutMode(effectiveMode === "collapsed" || effectiveMode === "hover-collapsed" ? "sidebar" : "collapsed")
      }
      searchPlaceholder="Search theme pages, users, reports..."
      showSettingsButton
      showTopNav={shouldShowTopNav(effectiveMode, topNavVisibility)}
      sidebarSubtitle="Admin demo"
      sidebarTitle="Theme Lab"
      userEmail="admin@company.test"
      userMenu={[
        { label: "Profile" },
        { label: "Billing" },
        { label: "Keyboard shortcuts" },
        { label: "Sign out" }
      ]}
      userName="Alex Console"
    >
      <Outlet />
    </AppShell>
  );
}

export function ThemeLayout({ theme }: ThemeLayoutProps) {
  return (
    <ThemeAppearanceProvider restoreColorMode={theme}>
      <ThemeLayoutShell />
    </ThemeAppearanceProvider>
  );
}

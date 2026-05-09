import { useEffect, useMemo, useState } from "react";
import { Outlet, useLocation } from "react-router-dom";
import { AppShell } from "../components/layout/AppShell";
import { Breadcrumbs } from "../components/layout/Breadcrumbs";
import { MobileDrawer } from "../components/layout/MobileDrawer";
import { Navbar } from "../components/layout/Navbar";
import { Sidebar } from "../components/layout/Sidebar";
import { shouldShowTopNav, ThemeAppearanceProvider, useThemeAppearance } from "../context/ThemeAppearanceContext";
import { themeNavigation } from "../theme/config/themeNavigation";

type ThemeLayoutProps = {
  theme: "light" | "dark";
};

function ThemeLayoutShell() {
  const { layoutMode, setLayoutMode, setEffectiveLayoutMode, densityClasses, topNavVisibility } = useThemeAppearance();
  const [mobileOpen, setMobileOpen] = useState(false);
  const location = useLocation();
  const effectiveMode = location.pathname === "/theme/login" ? "minimal" : layoutMode;

  useEffect(() => {
    setEffectiveLayoutMode(effectiveMode);
  }, [effectiveMode, setEffectiveLayoutMode]);

  const hasSidebar = effectiveMode === "sidebar" || effectiveMode === "collapsed" || effectiveMode === "hybrid";
  const sidebarIsCollapsed = effectiveMode === "collapsed";

  const contentClass = useMemo(() => {
    if (!hasSidebar) {
      return "lg:pl-0";
    }

    return sidebarIsCollapsed ? "lg:pl-20" : "lg:pl-72";
  }, [hasSidebar, sidebarIsCollapsed]);

  return (
    <AppShell
      className="min-h-screen bg-slate-50 text-slate-950 dark:bg-slate-950 dark:text-slate-100"
      mode="playground"
      navigation={themeNavigation}
      sidebar={
        <>
          <MobileDrawer navigation={themeNavigation} open={mobileOpen} onClose={() => setMobileOpen(false)} />

          {hasSidebar ? (
            <Sidebar
              className="fixed inset-y-0 left-0 z-40 hidden lg:flex"
              collapsed={sidebarIsCollapsed}
              navigation={themeNavigation}
              variant={effectiveMode === "hybrid" ? "hybrid" : "default"}
              onToggleCollapsed={
                effectiveMode === "hybrid" ? undefined : () => setLayoutMode(sidebarIsCollapsed ? "sidebar" : "collapsed")
              }
            />
          ) : null}
        </>
      }
    >
      <div className={`min-h-screen transition-[padding] duration-200 ${contentClass}`}>
        <Navbar
          navigation={themeNavigation}
          onMenuClick={() => setMobileOpen(true)}
          searchPlaceholder="Search theme pages, users, reports..."
          showMobileMenuButton
          showSettingsButton
          showTopNav={shouldShowTopNav(effectiveMode, topNavVisibility)}
          subtitle="Navigation and design system"
          title="Theme playground"
          userEmail="admin@company.test"
          userMenu={[
            { label: "Profile" },
            { label: "Billing" },
            { label: "Keyboard shortcuts" },
            { label: "Sign out" }
          ]}
          userName="Alex Console"
        />

        <main className={`mx-auto flex w-full max-w-7xl flex-col ${densityClasses.pageGap} ${densityClasses.pagePadding}`}>
          <Breadcrumbs navigation={themeNavigation} rootLabel="Theme" rootPath="/theme" />
          <Outlet />
        </main>
      </div>
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

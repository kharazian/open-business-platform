import { useState, type CSSProperties, type ReactNode } from "react";
import type { NavigationItem } from "../../config/appNavigation";
import { useAppTheme } from "../../context/AppThemeContext";
import { cn } from "../../lib/cn";
import { MobileDrawer } from "./MobileDrawer";
import { Navbar } from "./Navbar";
import { Sidebar } from "./Sidebar";

type ShellLayout = "topbar" | "topnav" | "sidebar" | "collapsed" | "hybrid" | "minimal";

type UserMenuLink = {
  label: string;
  to?: string;
  onClick?: () => void;
};

type AppShellProps = {
  navigation: NavigationItem[];
  mode: "app" | "playground";
  children: ReactNode;
  className?: string;
  style?: CSSProperties;
  containerClassName?: string;
  mainClassName?: string;
  contentClassName?: string;
  contentBefore?: ReactNode;
  layout?: ShellLayout;
  showTopNav?: boolean;
  showMobileMenuButton?: boolean;
  showSettingsButton?: boolean;
  settingsHref?: string;
  navbarTitle?: string;
  navbarSubtitle?: string;
  navbarLogoText?: string;
  searchPlaceholder?: string;
  sidebarTitle?: string;
  sidebarSubtitle?: string;
  sidebarLogoText?: string;
  sidebarVariant?: "default" | "hybrid";
  sidebarAriaLabel?: string;
  onSidebarToggle?: () => void;
  theme?: "light" | "dark";
  onThemeToggle?: () => void;
  userName?: string;
  userEmail?: string;
  userMenu?: UserMenuLink[];
};

function getAppLayout(layout: "topbar" | "sidebar" | "collapsed-sidebar"): ShellLayout {
  if (layout === "collapsed-sidebar") return "collapsed";
  return layout;
}

export function AppShell({
  navigation,
  mode,
  children,
  className,
  style,
  containerClassName = "max-w-7xl",
  mainClassName,
  contentClassName,
  contentBefore,
  layout,
  showTopNav,
  showMobileMenuButton = true,
  showSettingsButton = false,
  settingsHref,
  navbarTitle,
  navbarSubtitle,
  navbarLogoText = "OBP",
  searchPlaceholder,
  sidebarTitle,
  sidebarSubtitle,
  sidebarLogoText = "OBP",
  sidebarVariant = "default",
  sidebarAriaLabel,
  onSidebarToggle,
  theme,
  onThemeToggle,
  userName,
  userEmail,
  userMenu
}: AppShellProps) {
  const { appThemeSettings, savedAppThemeSettings, updateAppThemeSettings } = useAppTheme();
  const [mobileOpen, setMobileOpen] = useState(false);
  const effectiveLayout = layout ?? getAppLayout(appThemeSettings.layout);
  const hasSidebar = effectiveLayout === "sidebar" || effectiveLayout === "collapsed" || effectiveLayout === "hybrid";
  const sidebarCollapsed = effectiveLayout === "collapsed";
  const sidebarPadding = sidebarCollapsed ? "lg:pl-20" : "lg:pl-72";
  const resolvedSidebarTitle = sidebarTitle ?? (mode === "app" ? "Open Business Platform" : "Theme Lab");
  const resolvedSidebarSubtitle = sidebarSubtitle ?? (mode === "app" ? "Main app" : "Admin demo");
  const resolvedSidebarAriaLabel = sidebarAriaLabel ?? (mode === "app" ? "Main app navigation" : "Theme navigation");
  const resolvedNavbarTitle = navbarTitle ?? (mode === "app" ? "Open Business Platform" : "Theme playground");
  const resolvedNavbarSubtitle = navbarSubtitle ?? (mode === "app" ? (savedAppThemeSettings ? "Saved theme active" : "App preview") : undefined);
  const resolvedSearchPlaceholder = searchPlaceholder ?? (mode === "app" ? "Search modules, users, reports..." : "Search...");
  const resolvedSettingsHref = settingsHref ?? (mode === "app" ? "/settings" : undefined);
  const resolvedShowTopNav = showTopNav ?? !hasSidebar;
  const resolvedShowMobileMenuButton = showMobileMenuButton && navigation.length > 0;
  const resolvedMainClassName = mainClassName ?? "px-4 py-6 sm:px-6 lg:px-8";
  const resolvedContainerClassName = hasSidebar ? "w-full max-w-none" : containerClassName;
  const resolvedSidebarVariant = effectiveLayout === "hybrid" ? "hybrid" : sidebarVariant;
  const toggleSidebar =
    onSidebarToggle ??
    (mode === "app" && hasSidebar
      ? () => {
          updateAppThemeSettings({ layout: sidebarCollapsed ? "sidebar" : "collapsed-sidebar" });
        }
      : undefined);

  const openMobileMenu = () => setMobileOpen(true);
  const closeMobileMenu = () => setMobileOpen(false);

  const shell = (
    <>
      <MobileDrawer
        navigation={navigation}
        onClose={closeMobileMenu}
        open={mobileOpen}
        sidebarAriaLabel={resolvedSidebarAriaLabel}
        sidebarLogoText={sidebarLogoText}
        sidebarSubtitle={resolvedSidebarSubtitle}
        sidebarTitle={resolvedSidebarTitle}
        sidebarVariant={resolvedSidebarVariant}
      />

      {hasSidebar ? (
        <Sidebar
          ariaLabel={resolvedSidebarAriaLabel}
          className="fixed inset-y-0 left-0 z-40 hidden lg:flex"
          collapsed={sidebarCollapsed}
          logoText={sidebarLogoText}
          navigation={navigation}
          subtitle={resolvedSidebarSubtitle}
          title={resolvedSidebarTitle}
          variant={resolvedSidebarVariant}
        />
      ) : null}

      <div className={cn("min-h-screen transition-[padding] duration-200", hasSidebar && sidebarPadding)}>
        <Navbar
          brandClassName={hasSidebar ? "lg:hidden" : undefined}
          containerClassName={resolvedContainerClassName}
          logoText={navbarLogoText}
          navigation={navigation}
          onMenuClick={openMobileMenu}
          onSidebarToggle={hasSidebar && effectiveLayout !== "hybrid" ? toggleSidebar : undefined}
          searchPlaceholder={resolvedSearchPlaceholder}
          settingsHref={resolvedSettingsHref}
          showMobileMenuButton={resolvedShowMobileMenuButton}
          showSettingsButton={showSettingsButton}
          showTopNav={resolvedShowTopNav}
          sidebarToggleLabel={sidebarCollapsed ? "Expand sidebar" : "Collapse sidebar"}
          subtitle={resolvedNavbarSubtitle}
          theme={theme}
          title={resolvedNavbarTitle}
          userEmail={userEmail}
          userMenu={userMenu}
          userName={userName}
          onThemeToggle={onThemeToggle}
        />

        <main className={resolvedMainClassName}>
          <div className={cn(hasSidebar ? "w-full" : "mx-auto", resolvedContainerClassName, contentClassName)}>
            {contentBefore}
            {children}
          </div>
        </main>
      </div>
    </>
  );

  return (
    <div className={className} style={style}>
      {shell}
    </div>
  );
}

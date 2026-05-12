import { useState, type CSSProperties, type ReactNode } from "react";
import type { NavigationItem } from "../../config/appNavigation";
import { useAppTheme } from "../../context/AppThemeContext";
import { cn } from "../../lib/cn";
import { MobileDrawer } from "./MobileDrawer";
import { Navbar } from "./Navbar";
import { Sidebar } from "./Sidebar";

type ShellLayout = "topbar" | "topnav" | "sidebar" | "collapsed" | "hover-collapsed" | "hybrid" | "minimal";

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

function getAppLayout(layout: "topbar" | "sidebar" | "collapsed-sidebar" | "hover-collapsed-sidebar"): ShellLayout {
  if (layout === "collapsed-sidebar") return "collapsed";
  if (layout === "hover-collapsed-sidebar") return "hover-collapsed";
  return layout;
}

function hasSidebarLayout(layout: ShellLayout) {
  return layout === "sidebar" || layout === "collapsed" || layout === "hover-collapsed" || layout === "hybrid";
}

function getShellDefaults(mode: AppShellProps["mode"], hasSavedAppTheme: boolean) {
  if (mode === "app") {
    return {
      navbarTitle: "Open Business Platform",
      navbarSubtitle: hasSavedAppTheme ? "Saved theme active" : "App preview",
      searchPlaceholder: "Search modules, users, reports...",
      settingsHref: "/settings",
      sidebarAriaLabel: "Main app navigation",
      sidebarSubtitle: "Main app",
      sidebarTitle: "Open Business Platform"
    };
  }

  return {
    navbarTitle: "Theme playground",
    navbarSubtitle: undefined,
    searchPlaceholder: "Search...",
    settingsHref: undefined,
    sidebarAriaLabel: "Theme navigation",
    sidebarSubtitle: "Admin demo",
    sidebarTitle: "Theme Lab"
  };
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
  const hasSidebar = hasSidebarLayout(effectiveLayout);
  const sidebarCollapsed = effectiveLayout === "collapsed" || effectiveLayout === "hover-collapsed";
  const sidebarHoverExpand = effectiveLayout === "hover-collapsed";
  const sidebarPadding = sidebarCollapsed ? "lg:pl-20" : "lg:pl-72";
  const shellDefaults = getShellDefaults(mode, Boolean(savedAppThemeSettings));
  const resolvedSidebarTitle = sidebarTitle ?? shellDefaults.sidebarTitle;
  const resolvedSidebarSubtitle = sidebarSubtitle ?? shellDefaults.sidebarSubtitle;
  const resolvedSidebarAriaLabel = sidebarAriaLabel ?? shellDefaults.sidebarAriaLabel;
  const resolvedNavbarTitle = navbarTitle ?? shellDefaults.navbarTitle;
  const resolvedNavbarSubtitle = navbarSubtitle ?? shellDefaults.navbarSubtitle;
  const resolvedSearchPlaceholder = searchPlaceholder ?? shellDefaults.searchPlaceholder;
  const resolvedSettingsHref = settingsHref ?? shellDefaults.settingsHref;
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
          hoverExpand={sidebarHoverExpand}
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

import type { CSSProperties, ReactNode } from "react";
import { Link, NavLink } from "react-router-dom";
import type { NavigationItem } from "../../config/appNavigation";
import { useAppTheme } from "../../context/AppThemeContext";
import { cn } from "../../lib/cn";
import { Navbar } from "./Navbar";

type AppShellProps = {
  navigation: NavigationItem[];
  mode: "app" | "playground";
  children: ReactNode;
  className?: string;
  style?: CSSProperties;
  containerClassName?: string;
  mainClassName?: string;
  sidebar?: ReactNode;
  header?: ReactNode;
  theme?: "light" | "dark";
  onThemeToggle?: () => void;
};

export function AppShell({
  navigation,
  mode,
  children,
  className,
  style,
  containerClassName = "max-w-7xl",
  mainClassName,
  sidebar,
  header,
  theme,
  onThemeToggle
}: AppShellProps) {
  const { appThemeSettings, savedAppThemeSettings, updateAppThemeSettings } = useAppTheme();
  const hasAppSidebar = mode === "app" && (appThemeSettings.layout === "sidebar" || appThemeSettings.layout === "collapsed-sidebar");
  const appSidebarCollapsed = appThemeSettings.layout === "collapsed-sidebar";
  const sidebarPadding = appSidebarCollapsed ? "lg:pl-20" : "lg:pl-72";
  const toggleAppSidebar = () => {
    updateAppThemeSettings({ layout: appSidebarCollapsed ? "sidebar" : "collapsed-sidebar" });
  };

  if (mode === "playground") {
    return (
      <div className={className} style={style}>
        {sidebar}
        {header}
        {children}
      </div>
    );
  }

  return (
    <div className={className} style={style}>
      {hasAppSidebar ? (
        <aside
          className={cn(
            "fixed inset-y-0 left-0 z-40 hidden flex-col border-r border-border bg-card/90 p-3 shadow-lifted backdrop-blur-xl lg:flex",
            appSidebarCollapsed ? "w-20" : "w-72"
          )}
        >
          <Link className={cn("flex items-center gap-3 px-1 py-2", appSidebarCollapsed && "justify-center")} to="/">
            <span className="grid size-11 shrink-0 place-items-center rounded-xl bg-primary text-sm font-extrabold text-primary-foreground">OBP</span>
            {!appSidebarCollapsed ? (
              <div className="min-w-0">
                <strong className="block truncate leading-tight text-foreground">Open Business Platform</strong>
                <span className="text-sm text-muted-foreground">Main app</span>
              </div>
            ) : null}
          </Link>

          <nav className="mt-5 grid gap-1" aria-label="Main app navigation">
            {navigation.map((item) => {
              const Icon = item.icon;

              return (
                <NavLink
                  className={({ isActive }) =>
                    cn(
                      "control-transition flex min-h-11 items-center gap-3 rounded-xl px-3 text-sm font-semibold",
                      appSidebarCollapsed && "justify-center px-0",
                      isActive ? "bg-primary-soft text-primary" : "text-muted-foreground hover:bg-muted hover:text-foreground"
                    )
                  }
                  end={item.path === "/"}
                  key={item.path}
                  title={appSidebarCollapsed ? item.label : undefined}
                  to={item.path}
                >
                  {Icon ? <Icon className="size-4 shrink-0" /> : null}
                  {!appSidebarCollapsed ? <span className="truncate">{item.label}</span> : null}
                </NavLink>
              );
            })}
          </nav>
        </aside>
      ) : null}

      <div className={cn("min-h-screen transition-[padding] duration-200", hasAppSidebar && sidebarPadding)}>
        <Navbar
          brandClassName={hasAppSidebar ? "lg:hidden" : undefined}
          containerClassName={hasAppSidebar ? "max-w-7xl" : containerClassName}
          navigation={navigation}
          onMenuClick={() => undefined}
          onSidebarToggle={hasAppSidebar ? toggleAppSidebar : undefined}
          searchPlaceholder="Search modules, users, reports..."
          settingsHref="/settings"
          showTopNav={!hasAppSidebar}
          sidebarToggleLabel={appSidebarCollapsed ? "Expand main app sidebar" : "Collapse main app sidebar"}
          subtitle={savedAppThemeSettings ? "Saved theme active" : "App preview"}
          theme={theme}
          onThemeToggle={onThemeToggle}
        />

        <main className={cn("px-4 py-6 sm:px-6 lg:px-8", mainClassName)}>
          <div className={cn("mx-auto", hasAppSidebar ? "max-w-7xl" : containerClassName)}>{children}</div>
        </main>
      </div>
    </div>
  );
}

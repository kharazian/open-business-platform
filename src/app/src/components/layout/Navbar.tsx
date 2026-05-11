import { Bell, Menu, Moon, Search, Settings, Sun } from "lucide-react";
import { Link } from "react-router-dom";
import { cn } from "../../lib/cn";
import type { NavigationItem } from "../../config/appNavigation";
import { useDesignTheme } from "../../context/useDesignTheme";
import { Avatar } from "../ui/Avatar";
import { Button } from "../ui/Button";
import { Dropdown } from "../ui/Dropdown";
import { Input } from "../ui/Input";
import { SettingsButton } from "./SettingsButton";
import { TopNav } from "./TopNav";

type UserMenuLink = {
  label: string;
  to?: string;
  onClick?: () => void;
};

export function Navbar({
  navigation,
  onMenuClick,
  title = "Open Business Platform",
  subtitle,
  logoText = "OBP",
  searchPlaceholder = "Search...",
  showTopNav = false,
  showMobileMenuButton = false,
  showSettingsButton = false,
  settingsHref,
  onSidebarToggle,
  sidebarToggleLabel = "Toggle sidebar",
  brandClassName,
  containerClassName,
  theme,
  onThemeToggle,
  userName = "Admin User",
  userEmail = "admin@company.test",
  userMenu = [
    { label: "Profile", to: "/profile" },
    { label: "Settings", to: "/settings" },
    { label: "Sign out" }
  ]
}: {
  navigation: NavigationItem[];
  onMenuClick?: () => void;
  title?: string;
  subtitle?: string;
  logoText?: string;
  searchPlaceholder?: string;
  showTopNav?: boolean;
  showMobileMenuButton?: boolean;
  showSettingsButton?: boolean;
  settingsHref?: string;
  onSidebarToggle?: () => void;
  sidebarToggleLabel?: string;
  brandClassName?: string;
  containerClassName?: string;
  theme?: "light" | "dark";
  onThemeToggle?: () => void;
  userName?: string;
  userEmail?: string;
  userMenu?: UserMenuLink[];
}) {
  const { density } = useDesignTheme();

  return (
    <header className="sticky top-0 z-30 border-b border-border bg-background/84 px-3 py-3 backdrop-blur-xl sm:px-5 lg:px-6">
      <div className={cn("mx-auto flex items-center gap-3", density === "compact" && "gap-2", containerClassName)}>
        {showMobileMenuButton && onMenuClick ? (
          <Button className="shrink-0 lg:hidden" variant="outline" size="icon" onClick={onMenuClick} aria-label="Open menu">
            <Menu className="size-4" />
          </Button>
        ) : null}

        {onSidebarToggle ? (
          <Button
            className="shrink-0 max-lg:hidden"
            variant="outline"
            size="icon"
            onClick={onSidebarToggle}
            aria-label={sidebarToggleLabel}
            title={sidebarToggleLabel}
          >
            <Menu className="size-4" />
          </Button>
        ) : null}

        <div className={cn("flex min-w-0 items-center gap-3", brandClassName)}>
          <span className="grid size-10 shrink-0 place-items-center rounded-xl bg-primary text-sm font-extrabold text-primary-foreground">
            {logoText}
          </span>
          <div className="min-w-0">
            <p className="truncate text-sm font-bold text-foreground">{title}</p>
            {subtitle ? <p className="truncate text-xs text-muted-foreground">{subtitle}</p> : null}
          </div>
        </div>

        <div className="relative ml-auto hidden min-w-48 max-w-xl flex-1 md:block">
          <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
          <Input className="rounded-full pl-10" placeholder={searchPlaceholder} aria-label={searchPlaceholder} />
        </div>

        <Button variant="outline" size="icon" className="shrink-0" aria-label="Notifications" title="Notifications">
          <Bell className="size-4 shrink-0" />
        </Button>

        {showSettingsButton ? <SettingsButton /> : null}

        {settingsHref ? (
          <Link
            className="control-transition inline-flex h-10 w-10 shrink-0 items-center justify-center gap-2 rounded-xl border border-border bg-card p-0 text-sm font-bold text-foreground shadow-soft outline-none hover:bg-muted focus-visible:ring-4 focus-visible:ring-primary/20 sm:w-auto sm:px-3"
            to={settingsHref}
            aria-label="Open app settings"
            title="Settings"
          >
            <Settings className="size-4 shrink-0 text-primary" />
            <span className="hidden sm:inline">Settings</span>
          </Link>
        ) : null}

        {theme && onThemeToggle ? (
          <Button variant="outline" size="icon" onClick={onThemeToggle} aria-label="Toggle app theme">
            {theme === "dark" ? <Sun className="size-4" /> : <Moon className="size-4" />}
          </Button>
        ) : null}

        <Dropdown
          ariaLabel="Open user menu"
          closeOnContentClick
          trigger={
            <span className="inline-flex items-center gap-2 rounded-full border border-border bg-card px-2 py-1 shadow-soft">
              <Avatar name={userName} className="size-8" />
              <span className="hidden pr-2 text-left sm:block">
                <span className="block text-sm font-bold leading-tight text-foreground">{userName}</span>
                <span className="block text-xs text-muted-foreground">{userEmail}</span>
              </span>
            </span>
          }
        >
          <div className="grid gap-1">
            {userMenu.map((item) =>
              item.to ? (
                <Link
                  className="rounded-lg px-3 py-2 text-sm font-semibold text-muted-foreground hover:bg-muted hover:text-foreground"
                  key={item.label}
                  to={item.to}
                >
                  {item.label}
                </Link>
              ) : (
                <button
                  className="rounded-lg px-3 py-2 text-left text-sm font-semibold text-muted-foreground hover:bg-muted hover:text-foreground"
                  key={item.label}
                  type="button"
                  onClick={item.onClick}
                >
                  {item.label}
                </button>
              )
            )}
          </div>
        </Dropdown>
      </div>

      {showTopNav && onMenuClick ? (
        <div className={cn("mx-auto mt-3 hidden lg:block", containerClassName)}>
          <TopNav navigation={navigation} onMenuClick={onMenuClick} showMobileMenuButton={false} />
        </div>
      ) : null}
    </header>
  );
}

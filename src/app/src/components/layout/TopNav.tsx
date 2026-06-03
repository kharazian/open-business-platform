import { Menu } from "lucide-react";
import { NavLink } from "react-router-dom";
import { cn } from "../../lib/cn";
import type { NavigationItem } from "../../config/appNavigation";
import { Dropdown } from "../ui/Dropdown";
import { Button } from "../ui/Button";
import { useDesignTheme } from "../../context/useDesignTheme";
import { NavigationMenuItems } from "./NavigationMenuItems";
import { getNavigationSections } from "./navigationGroups";

type LinkVariant = "pill" | "menu";

function NavigationLink({ item, variant = "menu" }: { item: NavigationItem; variant?: LinkVariant }) {
  const { palette } = useDesignTheme();
  if (!item.path) return null;

  return (
    <NavLink
      className={({ isActive }) =>
        cn(
          variant === "pill"
            ? "inline-flex items-center gap-2 rounded-full px-3 py-2 text-sm font-bold transition"
            : "inline-flex items-center gap-2 rounded-lg px-3 py-2 text-sm font-bold",
          isActive ? `${palette.activeNavBg} ${palette.activeNavText}` : "text-muted-foreground hover:bg-muted hover:text-foreground"
        )
      }
      end={item.path === "/" || item.path === "/theme"}
      to={item.path}
    >
      <span>{item.label}</span>
      {hasNavigationBadge(item) ? <span className="rounded-full bg-danger px-2 py-0.5 text-xs font-bold text-white">{item.badge}</span> : null}
    </NavLink>
  );
}

function hasNavigationBadge(item: NavigationItem) {
  return item.badge !== undefined && item.badge !== null && String(item.badge).length > 0;
}

function DesktopNavigationItem({ item }: { item: NavigationItem }) {
  if (item.children?.length) {
    return (
      <Dropdown
        align="left"
        trigger={
          <span className="inline-flex min-h-10 items-center rounded-full px-3 text-sm font-bold text-muted-foreground hover:bg-muted hover:text-foreground">
            {item.label}
          </span>
        }
      >
        <div className="min-w-48">
          <NavigationMenuItems items={item.children} />
        </div>
      </Dropdown>
    );
  }

  return <NavigationLink item={item} variant="pill" />;
}

export function TopNav({
  navigation,
  onMenuClick,
  showMobileMenuButton = true
}: {
  navigation: NavigationItem[];
  onMenuClick: () => void;
  showMobileMenuButton?: boolean;
}) {
  const navigationSections = getNavigationSections(navigation);
  const hasSectionLabels = navigation.some((item) => item.section);

  return (
    <nav className="flex items-center gap-2" aria-label="Top navigation">
      {showMobileMenuButton ? (
        <Button className="lg:hidden" variant="outline" onClick={onMenuClick} aria-label="Open menu">
          <Menu className="size-4" />
        </Button>
      ) : null}
      <div className="hidden items-center gap-1 xl:flex">
        {hasSectionLabels
          ? navigationSections.map((section, index) =>
              section.label ? (
                <Dropdown
                  align="left"
                  key={`${section.label}-${index}`}
                  trigger={
                    <span className="inline-flex min-h-10 items-center rounded-full px-3 text-sm font-bold text-muted-foreground hover:bg-muted hover:text-foreground">
                      {section.label}
                    </span>
                  }
                >
                  <div className="min-w-48">
                    <NavigationMenuItems items={section.items} />
                  </div>
                </Dropdown>
              ) : (
                section.items.map((item) => <DesktopNavigationItem item={item} key={item.path ?? item.label} />)
              )
            )
          : navigation.slice(0, 8).map((item) => <DesktopNavigationItem item={item} key={item.path ?? item.label} />)}
      </div>
      <div className="xl:hidden">
        <Dropdown
          align="left"
          trigger={
            <span className="inline-flex min-h-10 items-center rounded-xl border border-border bg-card px-3 text-sm font-bold text-foreground">
              Menu
            </span>
          }
        >
          {hasSectionLabels ? (
            <div className="grid gap-1">
              {navigationSections.map((section, index) => (
                <div className="grid gap-1" key={`${section.label}-${index}`}>
                  {section.label ? <p className="px-3 pt-2 text-xs font-bold uppercase tracking-wide text-muted-foreground">{section.label}</p> : null}
                  <NavigationMenuItems items={section.items} />
                </div>
              ))}
            </div>
          ) : (
            <NavigationMenuItems items={navigation} />
          )}
        </Dropdown>
      </div>
    </nav>
  );
}

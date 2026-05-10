import { ChevronLeft, ChevronRight } from "lucide-react";
import type { HTMLAttributes } from "react";
import { NavLink } from "react-router-dom";
import { cn } from "../../lib/cn";
import type { NavigationItem } from "../../config/appNavigation";
import { useDesignTheme } from "../../context/useDesignTheme";
import { Button } from "../ui/Button";

function getNavigationSections(navigation: NavigationItem[]) {
  return navigation.reduce<Array<{ label: string; items: NavigationItem[] }>>((sections, item) => {
    const label = item.section ?? "";
    const currentSection = sections[sections.length - 1];

    if (currentSection?.label === label) {
      currentSection.items.push(item);
      return sections;
    }

    sections.push({ label, items: [item] });
    return sections;
  }, []);
}

export function Sidebar({
  collapsed,
  navigation,
  onToggleCollapsed,
  variant = "default",
  logoText = "OBP",
  title = "Theme Lab",
  subtitle = "Admin demo",
  ariaLabel = "Navigation",
  className
}: {
  collapsed: boolean;
  navigation: NavigationItem[];
  onToggleCollapsed?: () => void;
  variant?: "default" | "hybrid";
  logoText?: string;
  title?: string;
  subtitle?: string;
  ariaLabel?: string;
  className?: HTMLAttributes<HTMLElement>["className"];
}) {
  const { palette, density } = useDesignTheme();
  const navigationSections = getNavigationSections(navigation);
  const hasSectionLabels = navigation.some((item) => item.section);

  return (
    <aside
      className={cn(
        "flex h-full flex-col border-r border-border bg-card/90 p-3 shadow-lifted backdrop-blur-xl transition-all",
        collapsed ? "w-20" : "w-72",
        className
      )}
    >
      <div className={cn("flex items-center gap-3 px-1 py-2", collapsed && "justify-center")}>
        <span className={cn("grid size-11 shrink-0 place-items-center rounded-xl text-sm font-extrabold text-white", palette.primaryBg)}>
          {logoText}
        </span>
        {!collapsed ? (
          <div className="min-w-0">
            <strong className="block truncate leading-tight text-foreground">
              {variant === "hybrid" ? "Sections" : title}
            </strong>
            <span className="text-sm text-muted-foreground">
              {variant === "hybrid" ? "Hybrid sidebar" : subtitle}
            </span>
          </div>
        ) : null}
      </div>

      {variant === "hybrid" && !collapsed ? (
        <div className="mt-4 rounded-2xl border border-border bg-muted/50 p-3">
          <p className="text-xs font-bold uppercase tracking-wide text-muted-foreground">Workspace</p>
          <p className="mt-1 truncate text-sm font-semibold text-foreground">Northwind Operations</p>
        </div>
      ) : null}

      <nav className="mt-5 grid gap-1" aria-label={ariaLabel}>
        {navigationSections.map((section, sectionIndex) => (
          <div className={cn("grid gap-1", sectionIndex > 0 && (collapsed ? "mt-2" : "mt-4"))} key={`${section.label}-${sectionIndex}`}>
            {hasSectionLabels && section.label && !collapsed ? (
              <p className="px-3 pb-1 text-xs font-bold uppercase tracking-wide text-muted-foreground">{section.label}</p>
            ) : null}
            {section.items.map((item) => {
              const Icon = item.icon;

              return (
                <NavLink
                  className={({ isActive }) =>
                    cn(
                      "control-transition flex min-h-11 items-center gap-3 rounded-xl px-3 text-sm font-semibold",
                      density === "compact" && "min-h-10",
                      collapsed && "justify-center px-0",
                      isActive
                        ? `${palette.activeNavBg} ${palette.activeNavText}`
                        : "text-muted-foreground hover:bg-muted hover:text-foreground"
                    )
                  }
                  end={item.path === "/" || item.path === "/theme"}
                  key={item.path}
                  title={collapsed ? item.label : undefined}
                  to={item.path}
                >
                  {Icon ? <Icon className="size-4 shrink-0" /> : null}
                  {!collapsed ? <span className="truncate">{item.label}</span> : null}
                </NavLink>
              );
            })}
          </div>
        ))}
      </nav>

      {onToggleCollapsed ? (
        <Button className="mt-auto" variant="outline" onClick={onToggleCollapsed}>
          {collapsed ? <ChevronRight className="size-4" /> : <ChevronLeft className="size-4" />}
          {!collapsed ? "Collapse" : null}
        </Button>
      ) : null}
    </aside>
  );
}

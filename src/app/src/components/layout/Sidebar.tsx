import { ChevronDown, ChevronLeft, ChevronRight } from "lucide-react";
import { useMemo, useState, type HTMLAttributes } from "react";
import { NavLink, useLocation } from "react-router-dom";
import type { NavigationItem } from "../../config/appNavigation";
import { appBranding } from "../../config/branding";
import { useDesignTheme } from "../../context/useDesignTheme";
import { cn } from "../../lib/cn";
import { Button } from "../ui/Button";
import { Dropdown } from "../ui/Dropdown";
import { getNavigationSections, isNavigationItemActive } from "./navigationGroups";

type SidebarNavProps = {
  active: boolean;
  collapsed: boolean;
  density: "comfortable" | "compact";
  onNavigate?: () => void;
};

function getNavItemClassName({
  active,
  collapsed,
  density,
  nested,
  palette
}: SidebarNavProps & {
  nested?: boolean;
  palette: ReturnType<typeof useDesignTheme>["palette"];
}) {
  return cn(
    "control-transition flex items-center gap-3 rounded-xl px-3 text-sm font-semibold",
    "w-full",
    nested ? "min-h-10" : "min-h-11",
    density === "compact" && !nested && "min-h-10",
    collapsed && "justify-center px-0",
    active ? `${palette.activeNavBg} ${palette.activeNavText}` : "text-muted-foreground hover:bg-muted hover:text-foreground"
  );
}

function SidebarLink({
  item,
  active,
  collapsed,
  density,
  nested,
  onNavigate
}: SidebarNavProps & {
  item: NavigationItem;
  nested?: boolean;
}) {
  const { palette } = useDesignTheme();
  const Icon = item.icon;
  if (!item.path) return null;

  return (
    <NavLink
      className={getNavItemClassName({ active, collapsed, density, nested, palette })}
      end={item.path === "/" || item.path === "/theme"}
      title={collapsed ? item.label : undefined}
      to={item.path}
      onClick={onNavigate}
    >
      {Icon ? (
        <span className="relative shrink-0">
          <Icon className="size-4" />
          {collapsed && hasNavigationBadge(item) ? (
            <span className="absolute -right-2 -top-2 min-w-4 rounded-full bg-danger px-1 text-center text-[10px] font-bold leading-4 text-white">
              {item.badge}
            </span>
          ) : null}
        </span>
      ) : null}
      {!collapsed ? (
        <>
          <span className="min-w-0 flex-1 truncate">{item.label}</span>
          {hasNavigationBadge(item) ? <span className="shrink-0 rounded-full bg-danger px-2 py-0.5 text-xs font-bold text-white">{item.badge}</span> : null}
        </>
      ) : null}
    </NavLink>
  );
}

function hasNavigationBadge(item: NavigationItem) {
  return item.badge !== undefined && item.badge !== null && String(item.badge).length > 0;
}

function SidebarParentItem({
  item,
  active,
  collapsed,
  density,
  open,
  onNavigate,
  onToggle,
  pathname
}: SidebarNavProps & {
  item: NavigationItem;
  open: boolean;
  onToggle: () => void;
  pathname: string;
}) {
  const { palette } = useDesignTheme();
  const Icon = item.icon;

  if (collapsed) {
    return (
      <Dropdown
        ariaLabel={`Open ${item.label} menu`}
        className="w-full"
        placement="right-start"
        triggerClassName="block w-full"
        trigger={
          <span className={getNavItemClassName({ active, collapsed, density, palette })} title={item.label}>
            {Icon ? <Icon className="size-4 shrink-0" /> : null}
          </span>
        }
      >
        <div className="grid min-w-56 gap-1">
          <p className="px-3 pb-1 text-xs font-bold uppercase tracking-wide text-muted-foreground">{item.label}</p>
          {item.children?.map((child) => (
            <SidebarLink
              active={isNavigationItemActive(pathname, child)}
              collapsed={false}
              density={density}
              item={child}
              key={child.path ?? child.label}
              nested
              onNavigate={onNavigate}
            />
          ))}
        </div>
      </Dropdown>
    );
  }

  return (
    <div className="grid gap-1">
      <button
        className={getNavItemClassName({ active, collapsed, density, palette })}
        type="button"
        onClick={onToggle}
        aria-expanded={open}
        title={collapsed ? item.label : undefined}
      >
        {Icon ? <Icon className="size-4 shrink-0" /> : null}
        {!collapsed ? (
          <>
            <span className="min-w-0 flex-1 truncate text-left">{item.label}</span>
            <ChevronDown className={cn("size-4 shrink-0 transition-transform", !open && "-rotate-90")} />
          </>
        ) : null}
      </button>

      {!collapsed && open ? (
        <div className="ml-5 grid gap-1 border-l border-border pl-3">
          {item.children?.map((child) => (
            <SidebarLink
              active={isNavigationItemActive(pathname, child)}
              collapsed={false}
              density={density}
              item={child}
              key={child.path ?? child.label}
              nested
              onNavigate={onNavigate}
            />
          ))}
        </div>
      ) : null}
    </div>
  );
}

export function Sidebar({
  collapsed,
  hoverExpand = false,
  navigation,
  onNavigate,
  onToggleCollapsed,
  variant = "default",
  logoText = appBranding.logoText,
  title = "Theme Lab",
  subtitle = "Admin demo",
  ariaLabel = "Navigation",
  className
}: {
  collapsed: boolean;
  hoverExpand?: boolean;
  navigation: NavigationItem[];
  onNavigate?: () => void;
  onToggleCollapsed?: () => void;
  variant?: "default" | "hybrid";
  logoText?: string;
  title?: string;
  subtitle?: string;
  ariaLabel?: string;
  className?: HTMLAttributes<HTMLElement>["className"];
}) {
  const { palette, density } = useDesignTheme();
  const location = useLocation();
  const navigationSections = getNavigationSections(navigation);
  const hasSectionLabels = navigation.some((item) => item.section);
  const sectionKeys = useMemo(() => navigationSections.map((section, index) => `${section.label}-${index}`), [navigationSections]);
  const [hovered, setHovered] = useState(false);
  const [collapsedSections, setCollapsedSections] = useState<Set<string>>(() => new Set());
  const [openParentKey, setOpenParentKey] = useState<string | null>(null);
  const [closedParentKey, setClosedParentKey] = useState<string | null>(null);
  const displayCollapsed = collapsed && !(hoverExpand && hovered);

  const toggleGroup = (sectionKey: string) => {
    setCollapsedSections((current) => {
      const next = new Set(current);
      if (next.has(sectionKey)) {
        next.delete(sectionKey);
      } else {
        next.add(sectionKey);
      }
      return next;
    });
  };

  const toggleParent = (parentKey: string, currentlyOpen: boolean) => {
    if (currentlyOpen) {
      setOpenParentKey(null);
      setClosedParentKey(parentKey);
      return;
    }

    setOpenParentKey(parentKey);
    setClosedParentKey(null);
  };

  return (
    <aside
      className={cn(
        "flex h-full flex-col border-r border-border bg-card/90 p-3 shadow-lifted backdrop-blur-xl transition-all duration-200",
        displayCollapsed ? "w-20" : "w-72",
        hoverExpand && collapsed && hovered && "z-50",
        className
      )}
      onMouseEnter={() => setHovered(true)}
      onMouseLeave={() => setHovered(false)}
    >
      <div className={cn("flex items-center gap-3 px-1 py-2", displayCollapsed && "justify-center")}>
        <span className={cn("grid size-11 shrink-0 place-items-center rounded-xl text-sm font-extrabold text-white", palette.primaryBg)}>
          {logoText}
        </span>
        {!displayCollapsed ? (
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

      {variant === "hybrid" && !displayCollapsed ? (
        <div className="mt-4 rounded-2xl border border-border bg-muted/50 p-3">
          <p className="text-xs font-bold uppercase tracking-wide text-muted-foreground">Workspace</p>
          <p className="mt-1 truncate text-sm font-semibold text-foreground">Northwind Operations</p>
        </div>
      ) : null}

      <nav className={cn("mt-5 min-h-0 flex-1 pr-1", displayCollapsed ? "overflow-visible" : "overflow-y-auto")} aria-label={ariaLabel}>
        {navigationSections.map((section, sectionIndex) => (
          <div className={cn("grid gap-1", sectionIndex > 0 && (displayCollapsed ? "mt-2" : "mt-4"))} key={sectionKeys[sectionIndex]}>
            {hasSectionLabels && section.label && !displayCollapsed ? (
              <button
                className="flex min-h-9 items-center justify-between gap-2 rounded-xl px-3 text-left text-xs font-bold uppercase tracking-wide text-muted-foreground hover:bg-muted hover:text-foreground"
                type="button"
                onClick={() => toggleGroup(sectionKeys[sectionIndex])}
                aria-expanded={!collapsedSections.has(sectionKeys[sectionIndex])}
              >
                <span className="truncate">{section.label}</span>
                <ChevronDown className={cn("size-4 shrink-0 transition-transform", collapsedSections.has(sectionKeys[sectionIndex]) && "-rotate-90")} />
              </button>
            ) : null}
            <div className={cn("grid gap-1", hasSectionLabels && section.label && !displayCollapsed && "pl-2", collapsedSections.has(sectionKeys[sectionIndex]) && !displayCollapsed && "hidden")}>
              {section.items.map((item) => {
                const active = isNavigationItemActive(location.pathname, item);
                const parentKey = `${sectionKeys[sectionIndex]}-${item.label}`;
                const hasChildren = Boolean(item.children?.length);

                if (hasChildren) {
                  const parentOpen = openParentKey ? openParentKey === parentKey : active && closedParentKey !== parentKey;

                  return (
                    <SidebarParentItem
                      active={active}
                      collapsed={displayCollapsed}
                      density={density}
                      item={item}
                      key={parentKey}
                      onNavigate={onNavigate}
                      onToggle={() => toggleParent(parentKey, parentOpen)}
                      open={parentOpen}
                      pathname={location.pathname}
                    />
                  );
                }

                return (
                  <SidebarLink
                    active={active}
                    collapsed={displayCollapsed}
                    density={density}
                    item={item}
                    key={item.path ?? item.label}
                    onNavigate={onNavigate}
                  />
                );
              })}
            </div>
          </div>
        ))}
      </nav>

      {onToggleCollapsed ? (
        <Button className="mt-auto" variant="outline" onClick={onToggleCollapsed}>
          {displayCollapsed ? <ChevronRight className="size-4" /> : <ChevronLeft className="size-4" />}
          {!displayCollapsed ? "Collapse" : null}
        </Button>
      ) : null}
    </aside>
  );
}

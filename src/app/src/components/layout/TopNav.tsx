import { Menu } from "lucide-react";
import type { ReactNode } from "react";
import { NavLink } from "react-router-dom";
import { cn } from "../../lib/cn";
import type { NavigationItem } from "../../config/appNavigation";
import { Dropdown } from "../ui/Dropdown";
import { Button } from "../ui/Button";
import { useDesignTheme } from "../../context/useDesignTheme";
import { getNavigationSections } from "./navigationGroups";

function TopNavLink({ item, children }: { item: NavigationItem; children?: ReactNode }) {
  const { palette } = useDesignTheme();
  if (!item.path) return null;

  return (
    <NavLink
      className={({ isActive }) =>
        cn(
          "rounded-full px-3 py-2 text-sm font-bold transition",
          isActive ? `${palette.activeNavBg} ${palette.activeNavText}` : "text-muted-foreground hover:bg-muted hover:text-foreground"
        )
      }
      end={item.path === "/" || item.path === "/theme"}
      to={item.path}
    >
      {children ?? item.label}
    </NavLink>
  );
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
  const { palette } = useDesignTheme();
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
                  <div className="grid min-w-48 gap-1">
                    {section.items.map((item) => (
                      item.children?.length ? (
                        <div className="grid gap-1" key={item.label}>
                          <p className="px-3 pt-2 text-xs font-bold uppercase tracking-wide text-muted-foreground">{item.label}</p>
                          {item.children.map((child) => (
                            <TopNavLink item={child} key={child.path}>
                              {child.label}
                            </TopNavLink>
                          ))}
                        </div>
                      ) : (
                        <TopNavLink item={item} key={item.path ?? item.label}>
                          {item.label}
                        </TopNavLink>
                      )
                    ))}
                  </div>
                </Dropdown>
              ) : (
                section.items.map((item) => <TopNavLink item={item} key={item.path} />)
              )
            )
          : navigation.slice(0, 8).map((item) =>
              item.children?.length ? (
                <Dropdown
                  align="left"
                  key={item.label}
                  trigger={
                    <span className="inline-flex min-h-10 items-center rounded-full px-3 text-sm font-bold text-muted-foreground hover:bg-muted hover:text-foreground">
                      {item.label}
                    </span>
                  }
                >
                  <div className="grid min-w-48 gap-1">
                    {item.children.map((child) => (
                      <TopNavLink item={child} key={child.path}>
                        {child.label}
                      </TopNavLink>
                    ))}
                  </div>
                </Dropdown>
              ) : (
                <TopNavLink item={item} key={item.path ?? item.label} />
              )
            )}
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
          <div className="grid gap-1">
            {hasSectionLabels
              ? navigationSections.map((section, index) => (
                  <div className="grid gap-1" key={`${section.label}-${index}`}>
                    {section.label ? <p className="px-3 pt-2 text-xs font-bold uppercase tracking-wide text-muted-foreground">{section.label}</p> : null}
                    {section.items.map((item) =>
                      item.children?.length ? (
                        <div className="grid gap-1" key={item.label}>
                          <p className="px-3 pt-2 text-xs font-bold text-muted-foreground">{item.label}</p>
                          {item.children.map((child) => (
                            child.path ? (
                              <NavLink
                                className={({ isActive }) =>
                                  cn(
                                    "rounded-lg px-3 py-2 text-sm font-bold",
                                    isActive
                                      ? `${palette.activeNavBg} ${palette.activeNavText}`
                                      : "text-muted-foreground hover:bg-muted hover:text-foreground"
                                  )
                                }
                                end={child.path === "/" || child.path === "/theme"}
                                key={child.path}
                                to={child.path}
                              >
                                {child.label}
                              </NavLink>
                            ) : null
                          ))}
                        </div>
                      ) : item.path ? (
                        <NavLink
                          className={({ isActive }) =>
                            cn(
                              "rounded-lg px-3 py-2 text-sm font-bold",
                              isActive
                                ? `${palette.activeNavBg} ${palette.activeNavText}`
                                : "text-muted-foreground hover:bg-muted hover:text-foreground"
                            )
                          }
                          end={item.path === "/" || item.path === "/theme"}
                          key={item.path}
                          to={item.path}
                        >
                          {item.label}
                        </NavLink>
                      ) : null
                    )}
                  </div>
                ))
              : navigation.map((item) =>
                  item.children?.length ? (
                    <div className="grid gap-1" key={item.label}>
                      <p className="px-3 pt-2 text-xs font-bold text-muted-foreground">{item.label}</p>
                      {item.children.map((child) =>
                        child.path ? (
                          <NavLink
                            className={({ isActive }) =>
                              cn(
                                "rounded-lg px-3 py-2 text-sm font-bold",
                                isActive
                                  ? `${palette.activeNavBg} ${palette.activeNavText}`
                                  : "text-muted-foreground hover:bg-muted hover:text-foreground"
                              )
                            }
                            end={child.path === "/" || child.path === "/theme"}
                            key={child.path}
                            to={child.path}
                          >
                            {child.label}
                          </NavLink>
                        ) : null
                      )}
                    </div>
                  ) : item.path ? (
                    <NavLink
                      className={({ isActive }) =>
                        cn(
                          "rounded-lg px-3 py-2 text-sm font-bold",
                          isActive ? `${palette.activeNavBg} ${palette.activeNavText}` : "text-muted-foreground hover:bg-muted hover:text-foreground"
                        )
                      }
                      end={item.path === "/" || item.path === "/theme"}
                      key={item.path}
                      to={item.path}
                    >
                      {item.label}
                    </NavLink>
                  ) : null
                )}
          </div>
        </Dropdown>
      </div>
    </nav>
  );
}

import { Menu } from "lucide-react";
import { NavLink } from "react-router-dom";
import { cn } from "../../lib/cn";
import type { NavigationItem } from "../../config/appNavigation";
import { Dropdown } from "../ui/Dropdown";
import { Button } from "../ui/Button";
import { useDesignTheme } from "../../context/useDesignTheme";

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

  return (
    <nav className="flex items-center gap-2" aria-label="Top navigation">
      {showMobileMenuButton ? (
        <Button className="lg:hidden" variant="outline" onClick={onMenuClick} aria-label="Open menu">
          <Menu className="size-4" />
        </Button>
      ) : null}
      <div className="hidden items-center gap-1 xl:flex">
        {navigation.slice(0, 8).map((item) => (
          <NavLink
            className={({ isActive }) =>
              cn(
                "rounded-full px-3 py-2 text-sm font-bold transition",
                isActive ? `${palette.activeNavBg} ${palette.activeNavText}` : "text-muted-foreground hover:bg-muted hover:text-foreground"
              )
            }
            end={item.path === "/" || item.path === "/theme"}
            key={item.path}
            to={item.path}
          >
            {item.label}
          </NavLink>
        ))}
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
            {navigation.map((item) => (
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
            ))}
          </div>
        </Dropdown>
      </div>
    </nav>
  );
}

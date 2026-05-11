import { NavLink } from "react-router-dom";
import type { NavigationItem } from "../../config/appNavigation";
import { useDesignTheme } from "../../context/useDesignTheme";
import { cn } from "../../lib/cn";

function NavigationMenuLink({ item }: { item: NavigationItem }) {
  const { palette } = useDesignTheme();
  if (!item.path) return null;

  return (
    <NavLink
      className={({ isActive }) =>
        cn(
          "rounded-lg px-3 py-2 text-sm font-bold",
          isActive ? `${palette.activeNavBg} ${palette.activeNavText}` : "text-muted-foreground hover:bg-muted hover:text-foreground"
        )
      }
      end={item.path === "/" || item.path === "/theme"}
      to={item.path}
    >
      {item.label}
    </NavLink>
  );
}

export function NavigationMenuItems({ items }: { items: NavigationItem[] }) {
  return (
    <div className="grid gap-1">
      {items.map((item) =>
        item.children?.length ? (
          <div className="grid gap-1" key={item.label}>
            <p className="px-3 pt-2 text-xs font-bold uppercase tracking-wide text-muted-foreground">{item.label}</p>
            <NavigationMenuItems items={item.children} />
          </div>
        ) : (
          <NavigationMenuLink item={item} key={item.path ?? item.label} />
        )
      )}
    </div>
  );
}

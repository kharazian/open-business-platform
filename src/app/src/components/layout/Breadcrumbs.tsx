import { ChevronRight, Home } from "lucide-react";
import { Link, useLocation } from "react-router-dom";
import type { NavigationItem } from "../../config/appNavigation";
import { useDesignTheme } from "../../context/useDesignTheme";

export function Breadcrumbs({ navigation, rootLabel = "Home", rootPath = "/" }: { navigation: NavigationItem[]; rootLabel?: string; rootPath?: string }) {
  const location = useLocation();
  const current = navigation.find((item) => item.path === location.pathname);
  const { palette } = useDesignTheme();

  return (
    <nav className="mb-4 flex items-center gap-2 text-sm text-muted-foreground" aria-label="Breadcrumb">
      <Link className={`inline-flex items-center gap-1 font-semibold hover:text-foreground ${palette.primaryText}`} to={rootPath}>
        <Home className="size-3.5" />
        {rootLabel}
      </Link>
      {current && current.path !== rootPath ? (
        <>
          <ChevronRight className="size-3.5" />
          <span className="font-semibold text-foreground">{current.label}</span>
        </>
      ) : null}
    </nav>
  );
}

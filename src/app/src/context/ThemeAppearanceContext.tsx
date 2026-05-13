import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { getThemePalette, isThemePaletteId, type ThemePalette, type ThemePaletteId } from "../config/themePalettes";
import { isThemeDensity, themeDensity, type ThemeDensity } from "../config/themeTokens";
import { isThemeLayoutMode, type ThemeLayoutMode } from "../config/themeLayoutModes";

export type ThemeColorMode = "light" | "dark" | "system";
export type TopNavVisibility = "auto" | "show" | "hide";

type ThemeAppearanceContextValue = {
  layoutMode: ThemeLayoutMode;
  setLayoutMode: (mode: ThemeLayoutMode) => void;
  effectiveLayoutMode: ThemeLayoutMode;
  setEffectiveLayoutMode: (mode: ThemeLayoutMode) => void;
  sidebarCollapsed: boolean;
  setSidebarCollapsed: (collapsed: boolean) => void;
  paletteId: ThemePaletteId;
  palette: ThemePalette;
  setPaletteId: (palette: ThemePaletteId) => void;
  colorMode: ThemeColorMode;
  resolvedColorMode: "light" | "dark";
  setColorMode: (mode: ThemeColorMode) => void;
  density: ThemeDensity;
  setDensity: (density: ThemeDensity) => void;
  densityClasses: (typeof themeDensity)[ThemeDensity];
  topNavVisibility: TopNavVisibility;
  setTopNavVisibility: (visibility: TopNavVisibility) => void;
};

const ThemeAppearanceContext = createContext<ThemeAppearanceContextValue | null>(null);

const noop = () => undefined;

const defaultThemeAppearance: ThemeAppearanceContextValue = {
  layoutMode: "sidebar",
  setLayoutMode: noop,
  effectiveLayoutMode: "sidebar",
  setEffectiveLayoutMode: noop,
  sidebarCollapsed: false,
  setSidebarCollapsed: noop,
  paletteId: "slate-blue",
  palette: getThemePalette("slate-blue"),
  setPaletteId: noop,
  colorMode: "system",
  resolvedColorMode: "light",
  setColorMode: noop,
  density: "comfortable",
  setDensity: noop,
  densityClasses: themeDensity.comfortable,
  topNavVisibility: "auto",
  setTopNavVisibility: noop
};

const storageKeys = {
  layoutMode: "themeLayoutMode",
  palette: "themeColorPalette",
  sidebarCollapsed: "themeSidebarCollapsed",
  colorMode: "themeColorMode",
  density: "themeDensity",
  topNavVisibility: "themeTopNavVisibility"
};

function isThemeColorMode(value: string | null): value is ThemeColorMode {
  return value === "light" || value === "dark" || value === "system";
}

function isTopNavVisibility(value: string | null): value is TopNavVisibility {
  return value === "auto" || value === "show" || value === "hide";
}

export function shouldShowTopNav(layoutMode: ThemeLayoutMode, topNavVisibility: TopNavVisibility) {
  if (topNavVisibility === "show") return true;
  if (topNavVisibility === "hide") return false;

  return layoutMode === "topnav";
}

function getInitialSystemMode() {
  if (typeof window === "undefined") return "light";
  return window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
}

export function ThemeAppearanceProvider({
  children,
  restoreColorMode
}: {
  children: ReactNode;
  restoreColorMode?: "light" | "dark";
}) {
  const [layoutMode, setLayoutModeState] = useState<ThemeLayoutMode>("sidebar");
  const [sidebarCollapsed, setSidebarCollapsedState] = useState(false);
  const [paletteId, setPaletteIdState] = useState<ThemePaletteId>("slate-blue");
  const [colorMode, setColorModeState] = useState<ThemeColorMode>("system");
  const [density, setDensityState] = useState<ThemeDensity>("comfortable");
  const [topNavVisibility, setTopNavVisibilityState] = useState<TopNavVisibility>("auto");
  const [effectiveLayoutMode, setEffectiveLayoutMode] = useState<ThemeLayoutMode>("sidebar");
  const [systemMode, setSystemMode] = useState<"light" | "dark">(getInitialSystemMode);

  useEffect(() => {
    const storedLayoutMode = window.localStorage.getItem(storageKeys.layoutMode);
    const storedPalette = window.localStorage.getItem(storageKeys.palette);
    const storedCollapsed = window.localStorage.getItem(storageKeys.sidebarCollapsed);
    const storedColorMode = window.localStorage.getItem(storageKeys.colorMode);
    const storedDensity = window.localStorage.getItem(storageKeys.density);
    const storedTopNavVisibility = window.localStorage.getItem(storageKeys.topNavVisibility);

    if (isThemeLayoutMode(storedLayoutMode)) setLayoutModeState(storedLayoutMode);
    if (isThemePaletteId(storedPalette)) setPaletteIdState(storedPalette);
    if (storedCollapsed === "true" || storedCollapsed === "false") setSidebarCollapsedState(storedCollapsed === "true");
    if (isThemeColorMode(storedColorMode)) setColorModeState(storedColorMode);
    if (isThemeDensity(storedDensity)) setDensityState(storedDensity);
    if (isTopNavVisibility(storedTopNavVisibility)) setTopNavVisibilityState(storedTopNavVisibility);
  }, []);

  useEffect(() => {
    const query = window.matchMedia("(prefers-color-scheme: dark)");
    const update = () => setSystemMode(query.matches ? "dark" : "light");

    update();
    query.addEventListener("change", update);
    return () => query.removeEventListener("change", update);
  }, []);

  const resolvedColorMode = colorMode === "system" ? systemMode : colorMode;

  useEffect(() => {
    document.documentElement.classList.toggle("dark", resolvedColorMode === "dark");
  }, [resolvedColorMode]);

  useEffect(() => {
    return () => {
      if (restoreColorMode) {
        document.documentElement.classList.toggle("dark", restoreColorMode === "dark");
      }
    };
  }, [restoreColorMode]);

  const setLayoutMode = (nextMode: ThemeLayoutMode) => {
    setLayoutModeState(nextMode);
    window.localStorage.setItem(storageKeys.layoutMode, nextMode);
  };

  const setSidebarCollapsed = (collapsed: boolean) => {
    setSidebarCollapsedState(collapsed);
    window.localStorage.setItem(storageKeys.sidebarCollapsed, String(collapsed));
  };

  const setPaletteId = (nextPalette: ThemePaletteId) => {
    setPaletteIdState(nextPalette);
    window.localStorage.setItem(storageKeys.palette, nextPalette);
  };

  const setColorMode = (nextMode: ThemeColorMode) => {
    setColorModeState(nextMode);
    window.localStorage.setItem(storageKeys.colorMode, nextMode);
  };

  const setDensity = (nextDensity: ThemeDensity) => {
    setDensityState(nextDensity);
    window.localStorage.setItem(storageKeys.density, nextDensity);
  };

  const setTopNavVisibility = (nextVisibility: TopNavVisibility) => {
    setTopNavVisibilityState(nextVisibility);
    window.localStorage.setItem(storageKeys.topNavVisibility, nextVisibility);
  };

  const value = useMemo(
    () => ({
      layoutMode,
      setLayoutMode,
      effectiveLayoutMode,
      setEffectiveLayoutMode,
      sidebarCollapsed,
      setSidebarCollapsed,
      paletteId,
      palette: getThemePalette(paletteId),
      setPaletteId,
      colorMode,
      resolvedColorMode,
      setColorMode,
      density,
      setDensity,
      densityClasses: themeDensity[density],
      topNavVisibility,
      setTopNavVisibility
    }),
    [layoutMode, effectiveLayoutMode, sidebarCollapsed, paletteId, colorMode, resolvedColorMode, density, topNavVisibility]
  );

  return <ThemeAppearanceContext.Provider value={value}>{children}</ThemeAppearanceContext.Provider>;
}

export function useThemeAppearance() {
  const context = useContext(ThemeAppearanceContext);
  return context ?? defaultThemeAppearance;
}

export function useOptionalThemeAppearance() {
  return useContext(ThemeAppearanceContext);
}

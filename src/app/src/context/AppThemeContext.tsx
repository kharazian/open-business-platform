import { createContext, useContext, useEffect, useMemo, useState, type CSSProperties, type ReactNode } from "react";
import { isThemeDensity, type ThemeDensity } from "../config/themeTokens";
import { getThemePalette, isThemePaletteId, type ThemePaletteId } from "../config/themePalettes";
import type { ThemeColorMode } from "./ThemeAppearanceContext";

export type AppThemeRadius = "sm" | "md" | "lg" | "xl";
export type AppThemeShadow = "none" | "sm" | "md" | "lg";
export type AppThemeLayout = "topbar" | "sidebar" | "collapsed-sidebar" | "hover-collapsed-sidebar";

export type AppThemeSettings = {
  paletteId: ThemePaletteId;
  colorMode: ThemeColorMode;
  density: ThemeDensity;
  layout: AppThemeLayout;
  radius: AppThemeRadius;
  shadow: AppThemeShadow;
};

type AppThemeContextValue = {
  appThemeSettings: AppThemeSettings;
  savedAppThemeSettings: AppThemeSettings | null;
  updateAppThemeSettings: (settings: Partial<AppThemeSettings>) => void;
  saveAppThemeSettings: (settings?: AppThemeSettings) => void;
  resetAppThemeSettings: () => void;
  applyPreviewTheme: (settings: AppThemeSettings) => void;
  clearPreviewTheme: () => void;
  appThemeClassName: string;
  appThemeStyle: CSSProperties;
};

export const appThemeStorageKey = "appThemeSettings";

export const defaultAppThemeSettings: AppThemeSettings = {
  paletteId: "slate-blue",
  colorMode: "system",
  density: "comfortable",
  layout: "topbar",
  radius: "lg",
  shadow: "md"
};

const AppThemeContext = createContext<AppThemeContextValue | null>(null);

function isThemeColorMode(value: unknown): value is ThemeColorMode {
  return value === "light" || value === "dark" || value === "system";
}

function isAppThemeRadius(value: unknown): value is AppThemeRadius {
  return value === "sm" || value === "md" || value === "lg" || value === "xl";
}

function isAppThemeShadow(value: unknown): value is AppThemeShadow {
  return value === "none" || value === "sm" || value === "md" || value === "lg";
}

function isAppThemeLayout(value: unknown): value is AppThemeLayout {
  return value === "topbar" || value === "sidebar" || value === "collapsed-sidebar" || value === "hover-collapsed-sidebar";
}

function normalizeStoredLayout(layout: unknown): AppThemeLayout {
  if (isAppThemeLayout(layout)) return layout;
  if (layout === "standard" || layout === "wide" || layout === "focused") return "topbar";
  return defaultAppThemeSettings.layout;
}

function parseStoredAppTheme(value: string | null): AppThemeSettings | null {
  if (!value) return null;

  try {
    const parsed = JSON.parse(value) as Partial<AppThemeSettings>;

    if (
      isThemePaletteId(parsed.paletteId ?? null) &&
      isThemeColorMode(parsed.colorMode) &&
      isThemeDensity(parsed.density ?? null) &&
      isAppThemeRadius(parsed.radius) &&
      isAppThemeShadow(parsed.shadow)
    ) {
      return { ...defaultAppThemeSettings, ...parsed, layout: normalizeStoredLayout(parsed.layout) } as AppThemeSettings;
    }
  } catch {
    return null;
  }

  return null;
}

function getSystemMode() {
  if (typeof window === "undefined") return "light";
  return window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
}

function getResolvedColorMode(mode: ThemeColorMode) {
  return mode === "system" ? getSystemMode() : mode;
}

const paletteVariableMap: Record<ThemePaletteId, CSSProperties> = {
  "slate-blue": {
    "--app-primary": "oklch(0.55 0.18 255)",
    "--app-primary-hover": "oklch(0.49 0.19 255)",
    "--app-accent": "oklch(0.62 0.16 240)",
    "--app-ring": "oklch(0.55 0.18 255)",
    "--color-primary": "var(--app-primary)",
    "--color-primary-soft": "oklch(0.93 0.045 255)",
    "--color-primary-foreground": "oklch(0.985 0.004 230)"
  } as CSSProperties,
  "indigo-violet": {
    "--app-primary": "oklch(0.52 0.22 285)",
    "--app-primary-hover": "oklch(0.46 0.23 285)",
    "--app-accent": "oklch(0.62 0.2 305)",
    "--app-ring": "oklch(0.52 0.22 285)",
    "--color-primary": "var(--app-primary)",
    "--color-primary-soft": "oklch(0.93 0.052 285)",
    "--color-primary-foreground": "oklch(0.985 0.004 230)"
  } as CSSProperties,
  "emerald-teal": {
    "--app-primary": "oklch(0.54 0.15 165)",
    "--app-primary-hover": "oklch(0.47 0.15 165)",
    "--app-accent": "oklch(0.62 0.13 185)",
    "--app-ring": "oklch(0.54 0.15 165)",
    "--color-primary": "var(--app-primary)",
    "--color-primary-soft": "oklch(0.93 0.055 165)",
    "--color-primary-foreground": "oklch(0.985 0.004 230)"
  } as CSSProperties,
  "rose-pink": {
    "--app-primary": "oklch(0.58 0.19 12)",
    "--app-primary-hover": "oklch(0.51 0.2 12)",
    "--app-accent": "oklch(0.65 0.18 350)",
    "--app-ring": "oklch(0.58 0.19 12)",
    "--color-primary": "var(--app-primary)",
    "--color-primary-soft": "oklch(0.94 0.055 12)",
    "--color-primary-foreground": "oklch(0.985 0.004 230)"
  } as CSSProperties,
  "amber-orange": {
    "--app-primary": "oklch(0.66 0.17 70)",
    "--app-primary-hover": "oklch(0.58 0.17 70)",
    "--app-accent": "oklch(0.7 0.16 50)",
    "--app-ring": "oklch(0.66 0.17 70)",
    "--color-primary": "var(--app-primary)",
    "--color-primary-soft": "oklch(0.94 0.065 78)",
    "--color-primary-foreground": "oklch(0.18 0.022 250)"
  } as CSSProperties,
  "zinc-neutral": {
    "--app-primary": "oklch(0.25 0.01 250)",
    "--app-primary-hover": "oklch(0.2 0.01 250)",
    "--app-accent": "oklch(0.52 0.01 250)",
    "--app-ring": "oklch(0.45 0.01 250)",
    "--color-primary": "var(--app-primary)",
    "--color-primary-soft": "oklch(0.93 0.006 250)",
    "--color-primary-foreground": "oklch(0.985 0.004 230)"
  } as CSSProperties
};

const radiusVariables: Record<AppThemeRadius, CSSProperties> = {
  sm: { "--app-radius": "0.375rem", "--radius-panel": "0.375rem" } as CSSProperties,
  md: { "--app-radius": "0.625rem", "--radius-panel": "0.625rem" } as CSSProperties,
  lg: { "--app-radius": "0.875rem", "--radius-panel": "0.875rem" } as CSSProperties,
  xl: { "--app-radius": "1.25rem", "--radius-panel": "1.25rem" } as CSSProperties
};

const shadowVariables: Record<AppThemeShadow, CSSProperties> = {
  none: { "--app-shadow": "none", "--shadow-soft": "none", "--shadow-lifted": "none" } as CSSProperties,
  sm: {
    "--app-shadow": "0 10px 28px -24px oklch(0.205 0.024 250 / 0.28)",
    "--shadow-soft": "var(--app-shadow)",
    "--shadow-lifted": "0 14px 34px -28px oklch(0.205 0.024 250 / 0.34)"
  } as CSSProperties,
  md: {
    "--app-shadow": "0 16px 42px -30px oklch(0.205 0.024 250 / 0.34), inset 0 1px 0 oklch(1 0 0 / 0.7)",
    "--shadow-soft": "var(--app-shadow)",
    "--shadow-lifted": "0 22px 54px -34px oklch(0.205 0.024 250 / 0.42)"
  } as CSSProperties,
  lg: {
    "--app-shadow": "0 22px 54px -32px oklch(0.205 0.024 250 / 0.42), inset 0 1px 0 oklch(1 0 0 / 0.75)",
    "--shadow-soft": "var(--app-shadow)",
    "--shadow-lifted": "0 30px 72px -38px oklch(0.205 0.024 250 / 0.5)"
  } as CSSProperties
};

function getAppThemeClassName(settings: AppThemeSettings) {
  const densityClass = settings.density === "compact" ? "app-density-compact" : "app-density-comfortable";
  const modeClass = getResolvedColorMode(settings.colorMode) === "dark" ? "dark" : "";
  return ["app-theme-root", densityClass, modeClass].filter(Boolean).join(" ");
}

function getAppThemeStyle(settings: AppThemeSettings): CSSProperties {
  const palette = getThemePalette(settings.paletteId);

  return {
    "--app-bg": "var(--color-background)",
    "--app-surface": "var(--color-card)",
    "--app-text": "var(--color-foreground)",
    "--app-muted": "var(--color-muted-foreground)",
    "--app-border": "var(--color-border)",
    ...paletteVariableMap[palette.id],
    ...radiusVariables[settings.radius],
    ...shadowVariables[settings.shadow]
  } as CSSProperties;
}

export function createAppThemeSettingsFromDemo(settings: {
  paletteId: ThemePaletteId;
  density: ThemeDensity;
  colorMode: ThemeColorMode;
}): AppThemeSettings {
  return {
    paletteId: settings.paletteId,
    colorMode: settings.colorMode,
    density: settings.density,
    layout: "topbar",
    radius: "lg",
    shadow: "md"
  };
}

export function AppThemeProvider({ children }: { children: ReactNode }) {
  const [savedAppThemeSettings, setSavedAppThemeSettings] = useState<AppThemeSettings | null>(null);
  const [appThemeSettings, setAppThemeSettings] = useState<AppThemeSettings>(defaultAppThemeSettings);

  useEffect(() => {
    const storedSettings = parseStoredAppTheme(window.localStorage.getItem(appThemeStorageKey));
    if (storedSettings) {
      setSavedAppThemeSettings(storedSettings);
      setAppThemeSettings(storedSettings);
    }
  }, []);

  const updateAppThemeSettings = (settings: Partial<AppThemeSettings>) => {
    setAppThemeSettings((current) => ({ ...current, ...settings }));
  };

  const saveAppThemeSettings = (settings = appThemeSettings) => {
    setSavedAppThemeSettings(settings);
    setAppThemeSettings(settings);
    window.localStorage.setItem(appThemeStorageKey, JSON.stringify(settings));
  };

  const resetAppThemeSettings = () => {
    setSavedAppThemeSettings(null);
    setAppThemeSettings(defaultAppThemeSettings);
    window.localStorage.removeItem(appThemeStorageKey);
  };

  const value = useMemo(
    () => ({
      appThemeSettings,
      savedAppThemeSettings,
      updateAppThemeSettings,
      saveAppThemeSettings,
      resetAppThemeSettings,
      applyPreviewTheme: setAppThemeSettings,
      clearPreviewTheme: () => setAppThemeSettings(savedAppThemeSettings ?? defaultAppThemeSettings),
      appThemeClassName: getAppThemeClassName(appThemeSettings),
      appThemeStyle: getAppThemeStyle(appThemeSettings)
    }),
    [appThemeSettings, savedAppThemeSettings]
  );

  return <AppThemeContext.Provider value={value}>{children}</AppThemeContext.Provider>;
}

export function useAppTheme() {
  const context = useContext(AppThemeContext);
  if (!context) {
    throw new Error("useAppTheme must be used inside AppThemeProvider");
  }
  return context;
}

export function useOptionalAppTheme() {
  return useContext(AppThemeContext);
}

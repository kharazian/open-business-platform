import { getThemePalette } from "../config/themePalettes";
import { themeDensity } from "../config/themeTokens";
import { defaultAppThemeSettings, useOptionalAppTheme } from "./AppThemeContext";
import { useOptionalThemeAppearance } from "./ThemeAppearanceContext";

export function useDesignTheme() {
  const appTheme = useOptionalAppTheme();
  const playgroundTheme = useOptionalThemeAppearance();
  const paletteId = playgroundTheme?.paletteId ?? appTheme?.appThemeSettings.paletteId ?? defaultAppThemeSettings.paletteId;
  const density = playgroundTheme?.density ?? appTheme?.appThemeSettings.density ?? defaultAppThemeSettings.density;

  return {
    paletteId,
    palette: getThemePalette(paletteId),
    density,
    densityClasses: themeDensity[density]
  };
}

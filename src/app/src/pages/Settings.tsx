import { Save } from "lucide-react";
import { useState } from "react";
import { Badge } from "../components/ui/Badge";
import { Button } from "../components/ui/Button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "../components/ui/Card";
import { Input } from "../components/ui/Input";
import { Select } from "../components/ui/Select";
import { themePalettes, type ThemePaletteId } from "../config/themePalettes";
import type { ThemeDensity } from "../config/themeTokens";
import {
  appThemeStorageKey,
  defaultAppThemeSettings,
  useAppTheme,
  type AppThemeLayout,
  type AppThemeRadius,
  type AppThemeShadow
} from "../context/AppThemeContext";
import type { ThemeColorMode } from "../context/ThemeAppearanceContext";

export function Settings() {
  const {
    appThemeSettings,
    savedAppThemeSettings,
    updateAppThemeSettings,
    saveAppThemeSettings,
    resetAppThemeSettings
  } = useAppTheme();
  const [message, setMessage] = useState("");

  const updateTheme = (settings: Partial<typeof appThemeSettings>) => {
    updateAppThemeSettings(settings);
    setMessage("Previewing changes. Save the theme to keep them after refresh.");
  };

  const saveTheme = () => {
    saveAppThemeSettings();
    setMessage(`Saved theme settings to localStorage as ${appThemeStorageKey}.`);
  };

  const resetTheme = () => {
    resetAppThemeSettings();
    setMessage("Theme reset. Saved app theme settings were cleared.");
  };

  return (
    <div className="grid gap-6">
      <div>
        <h1 className="text-3xl font-bold text-foreground">Settings</h1>
        <p className="mt-2 text-muted-foreground">Configure workspace preferences, app layout, and saved visual theme.</p>
      </div>

      <Card>
        <CardHeader>
          <div className="flex flex-wrap items-start justify-between gap-3">
            <div>
              <CardTitle>Appearance</CardTitle>
              <CardDescription>These settings control the real main app, not the /theme playground demo.</CardDescription>
            </div>
            <Badge tone={savedAppThemeSettings ? "success" : "default"}>
              {savedAppThemeSettings ? "Saved theme active" : "Default theme"}
            </Badge>
          </div>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
            <Select
              label="Color palette"
              value={appThemeSettings.paletteId}
              onChange={(event) => updateTheme({ paletteId: event.target.value as ThemePaletteId })}
              options={themePalettes.map((palette) => ({ label: palette.name, value: palette.id }))}
            />
            <Select
              label="Mode"
              value={appThemeSettings.colorMode}
              onChange={(event) => updateTheme({ colorMode: event.target.value as ThemeColorMode })}
              options={[
                { label: "Light", value: "light" },
                { label: "Dark", value: "dark" },
                { label: "System", value: "system" }
              ]}
            />
            <Select
              label="Density"
              value={appThemeSettings.density}
              onChange={(event) => updateTheme({ density: event.target.value as ThemeDensity })}
              options={[
                { label: "Comfortable", value: "comfortable" },
                { label: "Compact", value: "compact" }
              ]}
            />
            <Select
              label="Main app layout"
              help="This changes only the real app shell. Theme playground navigation stays separate."
              value={appThemeSettings.layout}
              onChange={(event) => updateTheme({ layout: event.target.value as AppThemeLayout })}
              options={[
                { label: "Topbar", value: "topbar" },
                { label: "Sidebar", value: "sidebar" },
                { label: "Collapsed sidebar", value: "collapsed-sidebar" }
              ]}
            />
            <Select
              label="Border radius"
              value={appThemeSettings.radius}
              onChange={(event) => updateTheme({ radius: event.target.value as AppThemeRadius })}
              options={[
                { label: "Small", value: "sm" },
                { label: "Medium", value: "md" },
                { label: "Large", value: "lg" },
                { label: "Extra large", value: "xl" }
              ]}
            />
            <Select
              label="Shadow"
              value={appThemeSettings.shadow}
              onChange={(event) => updateTheme({ shadow: event.target.value as AppThemeShadow })}
              options={[
                { label: "None", value: "none" },
                { label: "Small", value: "sm" },
                { label: "Medium", value: "md" },
                { label: "Large", value: "lg" }
              ]}
            />
          </div>

          <div className="mt-6 rounded-xl border border-border bg-muted/35 p-4 text-sm text-muted-foreground">
            <p>
              Current preview: <strong className="text-foreground">{appThemeSettings.layout}</strong> layout,{" "}
              <strong className="text-foreground">{appThemeSettings.paletteId}</strong> palette,{" "}
              <strong className="text-foreground">{appThemeSettings.density}</strong> density.
            </p>
            <p className="mt-2">
              Saving writes these real app settings to <code>{appThemeStorageKey}</code>. Reset clears that key and returns to the default{" "}
              <strong className="text-foreground">{defaultAppThemeSettings.layout}</strong> layout.
            </p>
            {message ? <p className="mt-3 font-semibold text-foreground">{message}</p> : null}
          </div>

          <div className="mt-6 flex flex-wrap justify-end gap-3">
            <Button variant="outline" onClick={resetTheme}>
              Reset theme
            </Button>
            <Button onClick={saveTheme}>
              <Save className="size-4" />
              Save theme
            </Button>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Workspace</CardTitle>
          <CardDescription>These controls are static samples for the first UI shell.</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 md:grid-cols-2">
            <Input label="Workspace name" defaultValue="Open Business Platform" />
            <Input label="Support email" defaultValue="support@company.test" type="email" />
            <Select
              label="Default module"
              defaultValue="dashboard"
              options={[
                { label: "Dashboard", value: "dashboard" },
                { label: "Users", value: "users" },
                { label: "Reports", value: "reports" }
              ]}
            />
            <Select
              label="Audit retention"
              defaultValue="365"
              options={[
                { label: "90 days", value: "90" },
                { label: "180 days", value: "180" },
                { label: "365 days", value: "365" }
              ]}
            />
          </div>
          <div className="mt-6 flex justify-end">
            <Button>
              <Save className="size-4" />
              Save changes
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}

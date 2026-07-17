import { Save } from "lucide-react";
import { useEffect, useState, type ChangeEvent } from "react";
import { Badge } from "../components/ui/Badge";
import { Button } from "../components/ui/Button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "../components/ui/Card";
import { Input } from "../components/ui/Input";
import { PageHeader } from "../components/ui/PageHeader";
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
import { useWorkspaceBranding } from "../context/WorkspaceBrandingContext";
import { LocalizationSettingsCard } from "../features/localization/LocalizationSettingsCard";
import { CustomDomainsSettingsCard } from "../features/domains/CustomDomainsSettingsCard";

export function Settings() {
  const { branding, canManage, saveBranding } = useWorkspaceBranding();
  const {
    appThemeSettings,
    savedAppThemeSettings,
    updateAppThemeSettings,
    saveAppThemeSettings,
    resetAppThemeSettings
  } = useAppTheme();
  const [message, setMessage] = useState("");
  const [brandingDraft, setBrandingDraft] = useState(branding);
  const [brandingMessage, setBrandingMessage] = useState("");
  const [savingBranding, setSavingBranding] = useState(false);

  useEffect(() => setBrandingDraft(branding), [branding]);

  const uploadLogo = (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;
    if (!["image/png", "image/jpeg", "image/webp"].includes(file.type) || file.size > 256 * 1024) {
      setBrandingMessage("Choose a PNG, JPEG, or WebP logo that is 256 KiB or smaller.");
      return;
    }
    const reader = new FileReader();
    reader.onload = () => setBrandingDraft((current) => ({ ...current, logoDataUrl: String(reader.result) }));
    reader.readAsDataURL(file);
  };

  const persistBranding = async () => {
    setSavingBranding(true);
    setBrandingMessage("");
    try {
      const saved = await saveBranding(brandingDraft);
      setBrandingDraft(saved);
      setBrandingMessage("Workspace branding saved.");
    } catch (error) {
      setBrandingMessage(error instanceof Error ? error.message : "Workspace branding could not be saved.");
    } finally {
      setSavingBranding(false);
    }
  };

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
      <PageHeader
        eyebrow="Workspace"
        title="Settings"
        description="Configure workspace preferences, app layout, and saved visual theme."
      />

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
                { label: "Collapsed sidebar", value: "collapsed-sidebar" },
                { label: "Collapsed hover sidebar", value: "hover-collapsed-sidebar" }
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

      <LocalizationSettingsCard />

      <CustomDomainsSettingsCard />

      <Card>
        <CardHeader>
          <div className="flex flex-wrap items-start justify-between gap-3">
            <div>
              <CardTitle>Workspace branding</CardTitle>
              <CardDescription>Shared identity for this workspace's login page and real app chrome.</CardDescription>
            </div>
            <Badge tone={canManage ? "success" : "default"}>{canManage ? "Can manage" : "Read only"}</Badge>
          </div>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 md:grid-cols-2">
            <Input disabled={!canManage} label="App name" maxLength={120} onChange={(event) => setBrandingDraft({ ...brandingDraft, appName: event.target.value })} value={brandingDraft.appName} />
            <Input disabled={!canManage} help="Up to 8 characters for compact navigation." label="Logo text" maxLength={8} onChange={(event) => setBrandingDraft({ ...brandingDraft, logoText: event.target.value })} value={brandingDraft.logoText} />
            <Input disabled={!canManage} label="Primary color" onChange={(event) => setBrandingDraft({ ...brandingDraft, primaryColor: event.target.value })} type="color" value={brandingDraft.primaryColor} />
            <Input disabled={!canManage} label="Login message" maxLength={240} onChange={(event) => setBrandingDraft({ ...brandingDraft, loginMessage: event.target.value || null })} value={brandingDraft.loginMessage ?? ""} />
            <Input accept="image/png,image/jpeg,image/webp" disabled={!canManage} help="PNG, JPEG, or WebP; maximum 256 KiB." label="Logo image" onChange={uploadLogo} type="file" />
            <div className="flex items-center gap-3 rounded-xl border border-border bg-muted/35 p-3">
              {brandingDraft.logoDataUrl ? <img alt="Brand preview" className="size-12 rounded-xl object-contain" src={brandingDraft.logoDataUrl} /> : <span className="grid size-12 place-items-center rounded-xl bg-primary text-sm font-extrabold text-primary-foreground">{brandingDraft.logoText}</span>}
              <div className="min-w-0"><p className="truncate font-bold text-foreground">{brandingDraft.appName}</p><p className="text-sm text-muted-foreground">Workspace preview</p></div>
              {brandingDraft.logoDataUrl && canManage ? <Button className="ml-auto" onClick={() => setBrandingDraft({ ...brandingDraft, logoDataUrl: null })} size="sm" variant="outline">Remove</Button> : null}
            </div>
          </div>
          {brandingMessage ? <p className="mt-4 text-sm font-semibold text-foreground">{brandingMessage}</p> : null}
          <div className="mt-6 flex justify-end">
            <Button disabled={!canManage || savingBranding} onClick={() => void persistBranding()}>
              <Save className="size-4" />
              {savingBranding ? "Saving..." : "Save branding"}
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}

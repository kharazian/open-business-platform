import { useState } from "react";
import { Button } from "../../components/ui/Button";
import { Card } from "../../components/ui/Card";
import { DensitySwitcher } from "../../components/layout/DensitySwitcher";
import { Input } from "../../components/ui/Input";
import { LayoutSwitcher } from "../../components/layout/LayoutSwitcher";
import { ModeToggle } from "../../components/layout/ModeToggle";
import { PageHeader } from "../../components/ui/PageHeader";
import { PaletteSwitcher } from "../../components/layout/PaletteSwitcher";
import { Select } from "../../components/ui/Select";
import { Switch } from "../../components/ui/Switch";
import { Tabs } from "../../components/ui/Tabs";
import { Textarea } from "../../components/ui/Textarea";
import { appThemeStorageKey, createAppThemeSettingsFromDemo, useAppTheme } from "../../context/AppThemeContext";
import { useThemeAppearance } from "../../context/ThemeAppearanceContext";

const tabs = ["General", "Security", "Notifications", "Billing"];

export function ThemeSettings() {
  const [active, setActive] = useState(tabs[0]);
  const [appThemeMessage, setAppThemeMessage] = useState("");
  const { paletteId, palette, density, colorMode } = useThemeAppearance();
  const { appThemeSettings, saveAppThemeSettings, resetAppThemeSettings, applyPreviewTheme, clearPreviewTheme } = useAppTheme();

  const selectedAppThemeSettings = createAppThemeSettingsFromDemo({ paletteId, density, colorMode });

  const previewTheme = () => {
    applyPreviewTheme(selectedAppThemeSettings);
    setAppThemeMessage("Previewing these visual settings on the real app. Nothing has been saved yet.");
  };

  const saveTheme = () => {
    saveAppThemeSettings(selectedAppThemeSettings);
    setAppThemeMessage(`Saved to localStorage as ${appThemeStorageKey}.`);
  };

  const resetTheme = () => {
    resetAppThemeSettings();
    clearPreviewTheme();
    setAppThemeMessage("Saved app theme cleared. The real app will use default styling.");
  };

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Workspace"
        title="Settings"
        description="Tabbed forms and compact controls for a practical admin settings area."
        actions={<Button>Save changes</Button>}
      />

      <Tabs tabs={tabs} activeTab={active} onChange={setActive} />

      <Card title="Theme appearance" description="These controls are scoped to /theme and persist with localStorage.">
        <div className="grid gap-6">
          <div>
            <h3 className="mb-3 text-sm font-bold text-foreground">Color palette</h3>
            <PaletteSwitcher />
          </div>
          <div className="grid gap-4 xl:grid-cols-3">
            <div>
              <h3 className="mb-3 text-sm font-bold text-foreground">Layout</h3>
              <LayoutSwitcher />
            </div>
            <div>
              <h3 className="mb-3 text-sm font-bold text-foreground">Density</h3>
              <DensitySwitcher />
            </div>
            <div>
              <h3 className="mb-3 text-sm font-bold text-foreground">Mode</h3>
              <ModeToggle />
            </div>
          </div>
        </div>
      </Card>

      <Card
        title="Apply Theme to App"
        description="This saves visual style settings for the real app. It does not copy demo pages or theme navigation."
      >
        <div className="grid gap-5 lg:grid-cols-[1fr_auto] lg:items-start">
          <div className="grid gap-4 sm:grid-cols-3">
            <div className="rounded-2xl border border-border bg-muted/40 p-4">
              <p className="text-xs font-bold uppercase tracking-wide text-muted-foreground">Palette</p>
              <p className="mt-2 font-bold text-foreground">{palette.name}</p>
              <p className="mt-1 text-sm text-muted-foreground">{palette.description}</p>
            </div>
            <div className="rounded-2xl border border-border bg-muted/40 p-4">
              <p className="text-xs font-bold uppercase tracking-wide text-muted-foreground">Density</p>
              <p className="mt-2 font-bold capitalize text-foreground">{density}</p>
              <p className="mt-1 text-sm text-muted-foreground">Saved as app spacing preference.</p>
            </div>
            <div className="rounded-2xl border border-border bg-muted/40 p-4">
              <p className="text-xs font-bold uppercase tracking-wide text-muted-foreground">Mode</p>
              <p className="mt-2 font-bold capitalize text-foreground">{colorMode}</p>
              <p className="mt-1 text-sm text-muted-foreground">Light, dark, or system-aware.</p>
            </div>
          </div>

          <div className="flex flex-wrap gap-2 lg:justify-end">
            <Button variant="outline" onClick={previewTheme}>Preview Theme</Button>
            <Button onClick={saveTheme}>Save Theme</Button>
            <Button variant="ghost" onClick={resetTheme}>Reset Theme</Button>
          </div>
        </div>

        {appThemeMessage ? (
          <p className="mt-4 rounded-xl border border-border bg-muted/50 px-4 py-3 text-sm font-semibold text-foreground">{appThemeMessage}</p>
        ) : null}

        <div className="mt-5 rounded-2xl border border-border bg-card/70 p-4">
          <h3 className="text-sm font-bold text-foreground">How to use this theme in the real app</h3>
          <div className="mt-3 grid gap-2 text-sm leading-6 text-muted-foreground">
            <p>Choose a palette, density, and mode in the theme playground.</p>
            <p>Click Save Theme to write the visual settings to <span className="font-semibold text-foreground">{appThemeStorageKey}</span>.</p>
            <p>The real app reads those settings through AppThemeContext and CSS variables.</p>
            <p>Demo navigation, demo pages, and fake dashboard data stay only inside /theme.</p>
          </div>
        </div>

        {appThemeSettings ? (
          <p className="mt-4 text-xs text-muted-foreground">
            Current saved app theme: {appThemeSettings.paletteId}, {appThemeSettings.density}, {appThemeSettings.colorMode}.
          </p>
        ) : (
          <p className="mt-4 text-xs text-muted-foreground">No saved app theme yet. The real app is using defaults.</p>
        )}
      </Card>

      <Card title={`${active} settings`}>
        <div className="grid gap-5 lg:grid-cols-2">
          <Input label="Workspace name" defaultValue="Open Business Platform" help="Shown in headers and system emails." />
          <Select label="Default timezone">
            <option>America/Toronto</option>
            <option>UTC</option>
            <option>Europe/London</option>
          </Select>
          <Input label="Support email" defaultValue="support@company.com" />
          <Select label="Data retention">
            <option>12 months</option>
            <option>24 months</option>
            <option>Indefinite</option>
          </Select>
          <div className="lg:col-span-2">
            <Textarea label="Workspace note" placeholder="Add a short internal note for administrators..." help="Textarea styling for longer configuration values." />
          </div>
        </div>
      </Card>

      <div className="grid gap-4 lg:grid-cols-2">
        <Switch label="Require MFA" description="Ask all admins and managers to use multi-factor authentication." defaultChecked />
        <Switch label="Weekly digest" description="Send product and audit summaries every Monday morning." defaultChecked />
        <Switch label="Strict audit mode" description="Capture extra metadata for sensitive configuration changes." />
        <Switch label="Billing alerts" description="Notify owners when usage approaches plan limits." />
      </div>
    </div>
  );
}

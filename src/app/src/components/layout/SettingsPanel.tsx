import type { ReactNode } from "react";
import { DensitySwitcher } from "./DensitySwitcher";
import { LayoutSwitcher } from "./LayoutSwitcher";
import { ModeToggle } from "./ModeToggle";
import { PaletteSwitcher } from "./PaletteSwitcher";
import { TopNavVisibilitySwitcher } from "./TopNavVisibilitySwitcher";

function SettingsSection({ title, children }: { title: string; children: ReactNode }) {
  return (
    <section className="space-y-3">
      <h3 className="text-xs font-bold uppercase tracking-wide text-muted-foreground">{title}</h3>
      {children}
    </section>
  );
}

export function SettingsPanel() {
  return (
    <div className="space-y-5 p-2">
      <div>
        <p className="text-sm font-bold text-foreground">Theme settings</p>
        <p className="mt-1 text-xs leading-5 text-muted-foreground">Configure the playground layout, navigation, color, and display preferences.</p>
      </div>

      <SettingsSection title="Layout">
        <LayoutSwitcher />
      </SettingsSection>

      <SettingsSection title="Navigation">
        <TopNavVisibilitySwitcher />
      </SettingsSection>

      <SettingsSection title="Color">
        <PaletteSwitcher compact />
      </SettingsSection>

      <SettingsSection title="Display">
        <div className="grid gap-3">
          <DensitySwitcher />
          <ModeToggle />
        </div>
      </SettingsSection>
    </div>
  );
}

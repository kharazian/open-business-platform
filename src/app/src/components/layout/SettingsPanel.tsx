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

const settingsSections: Array<{ title: string; content: ReactNode }> = [
  { title: "Layout", content: <LayoutSwitcher /> },
  { title: "Navigation", content: <TopNavVisibilitySwitcher /> },
  { title: "Color", content: <PaletteSwitcher compact /> },
  {
    title: "Display",
    content: (
      <div className="grid gap-3">
        <DensitySwitcher />
        <ModeToggle />
      </div>
    )
  }
];

export function SettingsPanel() {
  return (
    <div className="space-y-5 p-2">
      <div>
        <p className="text-sm font-bold text-foreground">Theme settings</p>
        <p className="mt-1 text-xs leading-5 text-muted-foreground">Configure the playground layout, navigation, color, and display preferences.</p>
      </div>

      {settingsSections.map((section) => (
        <SettingsSection key={section.title} title={section.title}>
          {section.content}
        </SettingsSection>
      ))}
    </div>
  );
}

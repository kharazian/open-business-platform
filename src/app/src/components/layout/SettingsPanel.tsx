import type { ReactNode } from "react";
import { DensitySwitcher } from "./DensitySwitcher";
import { LayoutSwitcher } from "./LayoutSwitcher";
import { ModeToggle } from "./ModeToggle";
import { PaletteSwitcher } from "./PaletteSwitcher";
import { TopNavVisibilitySwitcher } from "./TopNavVisibilitySwitcher";

function SettingsSection({ title, description, children }: { title: string; description: string; children: ReactNode }) {
  return (
    <section className="space-y-3">
      <div>
        <h3 className="text-xs font-bold uppercase tracking-wide text-muted-foreground">{title}</h3>
        <p className="mt-1 text-xs leading-5 text-muted-foreground">{description}</p>
      </div>
      {children}
    </section>
  );
}

const settingsSections: Array<{ title: string; description: string; content: ReactNode }> = [
  { title: "Layout", description: "Choose how the playground shell frames each page.", content: <LayoutSwitcher /> },
  { title: "Navigation", description: "Control when route links appear in the top bar.", content: <TopNavVisibilitySwitcher /> },
  { title: "Color", description: "Preview the palette used by shared theme components.", content: <PaletteSwitcher compact /> },
  {
    title: "Display",
    description: "Adjust density and color mode for the theme preview.",
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
        <SettingsSection key={section.title} title={section.title} description={section.description}>
          {section.content}
        </SettingsSection>
      ))}
    </div>
  );
}

import { Settings } from "lucide-react";
import { cn } from "../../lib/cn";
import { useDesignTheme } from "../../context/useDesignTheme";
import { Dropdown } from "../ui/Dropdown";
import { SettingsPanel } from "./SettingsPanel";

const settingsPanelWidthClass = "w-[min(24rem,calc(100vw-2rem))]";

function SettingsTrigger() {
  const { palette } = useDesignTheme();

  return (
    <span className="inline-flex h-10 w-10 shrink-0 items-center justify-center gap-2 rounded-xl border border-border bg-card p-0 text-sm font-bold text-foreground shadow-soft sm:w-auto sm:px-3">
      <Settings className={cn("size-4 shrink-0", palette.primaryText)} />
      <span className="hidden sm:inline">Settings</span>
    </span>
  );
}

export function SettingsButton() {
  return (
    <Dropdown
      ariaLabel="Open theme settings"
      contentClassName={settingsPanelWidthClass}
      trigger={<SettingsTrigger />}
    >
      <SettingsPanel />
    </Dropdown>
  );
}

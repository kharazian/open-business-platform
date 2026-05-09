import { Settings } from "lucide-react";
import { cn } from "../../lib/cn";
import { useDesignTheme } from "../../context/useDesignTheme";
import { Dropdown } from "../ui/Dropdown";
import { SettingsPanel } from "./SettingsPanel";

export function SettingsButton() {
  const { palette } = useDesignTheme();

  return (
    <Dropdown
      ariaLabel="Open theme settings"
      trigger={
        <span className="inline-flex min-h-10 items-center justify-center gap-2 rounded-xl border border-border bg-card px-3 text-sm font-bold text-foreground shadow-soft">
          <Settings className={cn("size-4", palette.primaryText)} />
          <span className="hidden sm:inline">Settings</span>
        </span>
      }
    >
      <SettingsPanel />
    </Dropdown>
  );
}

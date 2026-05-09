import { Laptop, Moon, Sun } from "lucide-react";
import { cn } from "../../lib/cn";
import { useThemeAppearance, type ThemeColorMode } from "../../context/ThemeAppearanceContext";

const modes: Array<{ label: string; value: ThemeColorMode; icon: typeof Sun }> = [
  { label: "Light", value: "light", icon: Sun },
  { label: "Dark", value: "dark", icon: Moon },
  { label: "System", value: "system", icon: Laptop }
];

export function ModeToggle({ compact = false }: { compact?: boolean }) {
  const { colorMode, setColorMode, palette } = useThemeAppearance();

  return (
    <div className="flex rounded-xl border border-border bg-muted/60 p-1" aria-label="Theme color mode">
      {modes.map((mode) => {
        const Icon = mode.icon;

        return (
          <button
            className={cn(
              "inline-flex min-h-8 items-center justify-center gap-2 rounded-lg px-2.5 text-xs font-bold transition",
              colorMode === mode.value ? `bg-card ${palette.primaryText} shadow-soft` : "text-muted-foreground hover:text-foreground"
            )}
            key={mode.value}
            type="button"
            onClick={() => setColorMode(mode.value)}
            aria-label={`Use ${mode.label} mode`}
          >
            <Icon className="size-4" />
            {!compact ? mode.label : null}
          </button>
        );
      })}
    </div>
  );
}

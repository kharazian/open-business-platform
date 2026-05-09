import type { ThemeLayoutMode } from "../../config/themeLayoutModes";
import { themeLayoutModes } from "../../config/themeLayoutModes";
import { cn } from "../../lib/cn";
import { useThemeAppearance } from "../../context/ThemeAppearanceContext";

export function LayoutSwitcher({
  mode,
  onChange
}: {
  mode?: ThemeLayoutMode;
  onChange?: (mode: ThemeLayoutMode) => void;
}) {
  const { layoutMode, setLayoutMode, palette } = useThemeAppearance();
  const selectedMode = mode ?? layoutMode;
  const handleChange = onChange ?? setLayoutMode;

  return (
    <div className="flex max-w-full gap-1 overflow-x-auto rounded-xl border border-border bg-muted/60 p-1" aria-label="Theme layout mode">
      {themeLayoutModes.map((item) => (
        <button
          className={cn(
            "whitespace-nowrap rounded-lg px-3 py-2 text-xs font-bold transition",
            selectedMode === item.value ? `bg-card ${palette.primaryText} shadow-soft` : "text-muted-foreground hover:text-foreground"
          )}
          key={item.value}
          type="button"
          onClick={() => handleChange(item.value)}
          title={item.description}
        >
          {item.label}
        </button>
      ))}
    </div>
  );
}

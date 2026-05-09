import { cn } from "../../lib/cn";
import { useThemeAppearance } from "../../context/ThemeAppearanceContext";
import type { ThemeDensity } from "../../config/themeTokens";

const densityOptions: Array<{ label: string; value: ThemeDensity; description: string }> = [
  { label: "Comfortable", value: "comfortable", description: "Roomier spacing for dashboard review." },
  { label: "Compact", value: "compact", description: "Denser controls for operational tools." }
];

export function DensitySwitcher() {
  const { density, setDensity, palette } = useThemeAppearance();

  return (
    <div className="flex rounded-xl border border-border bg-muted/60 p-1" aria-label="Theme density">
      {densityOptions.map((item) => (
        <button
          className={cn(
            "rounded-lg px-3 py-2 text-xs font-bold transition",
            density === item.value ? `bg-card ${palette.primaryText} shadow-soft` : "text-muted-foreground hover:text-foreground"
          )}
          key={item.value}
          title={item.description}
          type="button"
          onClick={() => setDensity(item.value)}
        >
          {item.label}
        </button>
      ))}
    </div>
  );
}

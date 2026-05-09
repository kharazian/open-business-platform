import { Check } from "lucide-react";
import { cn } from "../../lib/cn";
import { themePalettes, type ThemePaletteId } from "../../config/themePalettes";
import { useThemeAppearance } from "../../context/ThemeAppearanceContext";

export function PaletteSwitcher({
  value,
  onChange,
  compact = false
}: {
  value?: ThemePaletteId;
  onChange?: (palette: ThemePaletteId) => void;
  compact?: boolean;
}) {
  const { paletteId, setPaletteId } = useThemeAppearance();
  const selected = value ?? paletteId;
  const handleChange = onChange ?? setPaletteId;

  return (
    <div className={cn("grid gap-2", compact ? "grid-cols-3" : "grid-cols-1 sm:grid-cols-2 xl:grid-cols-3")}>
      {themePalettes.map((palette) => (
        <button
          className={cn(
            "rounded-2xl border bg-card p-3 text-left transition hover:-translate-y-0.5 hover:shadow-soft focus-visible:outline-none focus-visible:ring-4",
            palette.primaryRing,
            selected === palette.id ? palette.primaryBorder : "border-border"
          )}
          key={palette.id}
          type="button"
          onClick={() => handleChange(palette.id)}
        >
          <span className="flex items-center gap-3">
            <span className={cn("size-8 rounded-full bg-gradient-to-br", palette.gradientFrom, palette.gradientTo)} />
            <span className="min-w-0 flex-1">
              <span className="block truncate text-sm font-bold text-foreground">{palette.name}</span>
              {!compact ? <span className="mt-0.5 block text-xs text-muted-foreground">{palette.description}</span> : null}
            </span>
            {selected === palette.id ? <Check className={cn("size-4", palette.primaryText)} /> : null}
          </span>
        </button>
      ))}
    </div>
  );
}

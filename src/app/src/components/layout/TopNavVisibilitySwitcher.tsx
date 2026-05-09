import { cn } from "../../lib/cn";
import { useThemeAppearance, type TopNavVisibility } from "../../context/ThemeAppearanceContext";

const options: Array<{ label: string; value: TopNavVisibility; description: string }> = [
  { label: "Auto", value: "auto", description: "Show route links for minimal and top-nav layouts only." },
  { label: "Show", value: "show", description: "Always show route links in the theme header." },
  { label: "Hide", value: "hide", description: "Hide route links while keeping mobile navigation available." }
];

export function TopNavVisibilitySwitcher() {
  const { palette, topNavVisibility, setTopNavVisibility } = useThemeAppearance();

  return (
    <div className="grid gap-2">
      {options.map((option) => (
        <button
          className={cn(
            "rounded-xl border p-3 text-left transition hover:bg-muted/60 focus-visible:outline-none focus-visible:ring-4",
            palette.primaryRing,
            topNavVisibility === option.value ? `${palette.primaryBorder} ${palette.softBg}` : "border-border bg-card/70"
          )}
          key={option.value}
          type="button"
          onClick={() => setTopNavVisibility(option.value)}
        >
          <span className={cn("block text-sm font-bold", topNavVisibility === option.value ? palette.primaryText : "text-foreground")}>
            {option.label}
          </span>
          <span className="mt-1 block text-xs leading-5 text-muted-foreground">{option.description}</span>
        </button>
      ))}
    </div>
  );
}

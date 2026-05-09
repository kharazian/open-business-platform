import type { ReactNode } from "react";
import { cn } from "../../lib/cn";
import { useDesignTheme } from "../../context/useDesignTheme";

type ThemeTab = { label: string; value: string; content?: ReactNode };

export function Tabs({
  tabs,
  active,
  activeTab,
  onChange
}: {
  tabs: Array<ThemeTab | string>;
  active?: string;
  activeTab?: string;
  onChange: (value: string) => void;
}) {
  const { palette } = useDesignTheme();
  const normalizedTabs = tabs.map((tab) => (typeof tab === "string" ? { label: tab, value: tab } : tab));
  const selected = active ?? activeTab ?? normalizedTabs[0]?.value;
  const selectedTab = normalizedTabs.find((tab) => tab.value === selected) ?? normalizedTabs[0];

  return (
    <div>
      <div className="flex flex-wrap gap-2 rounded-xl bg-muted/60 p-1">
        {normalizedTabs.map((tab) => (
          <button
            className={cn(
              "rounded-lg px-3 py-2 text-sm font-bold transition",
              selected === tab.value ? `bg-card ${palette.primaryText} shadow-soft` : "text-muted-foreground hover:text-foreground"
            )}
            key={tab.value}
            type="button"
            onClick={() => onChange(tab.value)}
          >
            {tab.label}
          </button>
        ))}
      </div>
      {selectedTab?.content ? <div className="mt-4">{selectedTab.content}</div> : null}
    </div>
  );
}

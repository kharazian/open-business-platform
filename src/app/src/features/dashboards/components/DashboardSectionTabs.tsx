import { useRef } from "react";
import { Activity, BadgeDollarSign, BarChart3, ClipboardList, Factory, Gauge, HeartPulse, PackageCheck, ShieldCheck, TrendingUp, Wrench, type LucideIcon } from "lucide-react";
import type { SavedDashboardSection } from "../types";

const icons: Record<string, LucideIcon> = {
  activity: Activity,
  "badge-dollar-sign": BadgeDollarSign,
  "chart-column": BarChart3,
  "clipboard-list": ClipboardList,
  factory: Factory,
  gauge: Gauge,
  "heart-pulse": HeartPulse,
  "package-check": PackageCheck,
  "shield-check": ShieldCheck,
  "trending-up": TrendingUp,
  wrench: Wrench
};

export function DashboardSectionTabs({ activeSectionId, onChange, sections }: { activeSectionId: string; onChange: (sectionId: string) => void; sections: SavedDashboardSection[] }) {
  const refs = useRef<Array<HTMLButtonElement | null>>([]);

  function select(index: number) {
    const section = sections[index];
    if (!section) return;
    onChange(section.id);
    refs.current[index]?.focus();
  }

  return <div aria-label="Dashboard sections" className="flex gap-1 overflow-x-auto border-b border-border pb-px" role="tablist">{sections.map((section, index) => {
    const Icon = section.icon ? icons[section.icon] : undefined;
    return <button
      aria-controls={`dashboard-panel-${section.id}`}
      aria-selected={activeSectionId === section.id}
      className={`flex min-h-11 shrink-0 items-center gap-2 border-b-2 px-3 py-2 text-sm font-bold focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary ${activeSectionId === section.id ? "border-primary text-foreground" : "border-transparent text-muted-foreground hover:text-foreground"}`}
      id={`dashboard-tab-${section.id}`}
      key={section.id}
      onClick={() => onChange(section.id)}
      onKeyDown={(event) => {
        if (event.key === "ArrowRight") { event.preventDefault(); select((index + 1) % sections.length); }
        if (event.key === "ArrowLeft") { event.preventDefault(); select((index - 1 + sections.length) % sections.length); }
        if (event.key === "Home") { event.preventDefault(); select(0); }
        if (event.key === "End") { event.preventDefault(); select(sections.length - 1); }
      }}
      ref={(element) => { refs.current[index] = element; }}
      role="tab"
      tabIndex={activeSectionId === section.id ? 0 : -1}
      type="button"
    >{Icon ? <Icon aria-hidden="true" className="size-4" /> : null}{section.title}</button>;
  })}</div>;
}

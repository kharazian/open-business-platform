export type ThemePalette = {
  id: string;
  name: string;
  description: string;
  primaryBg: string;
  primaryHoverBg: string;
  primaryText: string;
  primaryBorder: string;
  primaryRing: string;
  softBg: string;
  softText: string;
  activeNavBg: string;
  activeNavText: string;
  badgeBg: string;
  badgeText: string;
  gradientFrom: string;
  gradientTo: string;
};

export const themePalettes = [
  {
    id: "slate-blue",
    name: "Slate Blue",
    description: "Professional admin/dashboard default with crisp blue accents.",
    primaryBg: "bg-blue-600",
    primaryHoverBg: "hover:bg-blue-700",
    primaryText: "text-blue-700 dark:text-blue-300",
    primaryBorder: "border-blue-200 dark:border-blue-800",
    primaryRing: "focus-visible:ring-blue-500 focus:ring-blue-500",
    softBg: "bg-blue-50 dark:bg-blue-950/45",
    softText: "text-blue-700 dark:text-blue-300",
    activeNavBg: "bg-blue-50 dark:bg-blue-950/55",
    activeNavText: "text-blue-700 dark:text-blue-200",
    badgeBg: "bg-blue-100 dark:bg-blue-950/65",
    badgeText: "text-blue-800 dark:text-blue-200",
    gradientFrom: "from-blue-500",
    gradientTo: "to-sky-400"
  },
  {
    id: "indigo-violet",
    name: "Indigo Violet",
    description: "Modern SaaS palette with confident indigo and violet depth.",
    primaryBg: "bg-indigo-600",
    primaryHoverBg: "hover:bg-indigo-700",
    primaryText: "text-indigo-700 dark:text-indigo-300",
    primaryBorder: "border-indigo-200 dark:border-indigo-800",
    primaryRing: "focus-visible:ring-indigo-500 focus:ring-indigo-500",
    softBg: "bg-indigo-50 dark:bg-indigo-950/45",
    softText: "text-indigo-700 dark:text-indigo-300",
    activeNavBg: "bg-indigo-50 dark:bg-indigo-950/55",
    activeNavText: "text-indigo-700 dark:text-indigo-200",
    badgeBg: "bg-indigo-100 dark:bg-indigo-950/65",
    badgeText: "text-indigo-800 dark:text-indigo-200",
    gradientFrom: "from-indigo-500",
    gradientTo: "to-violet-500"
  },
  {
    id: "emerald-teal",
    name: "Emerald Teal",
    description: "Clean finance, health, and productivity theme.",
    primaryBg: "bg-emerald-600",
    primaryHoverBg: "hover:bg-emerald-700",
    primaryText: "text-emerald-700 dark:text-emerald-300",
    primaryBorder: "border-emerald-200 dark:border-emerald-800",
    primaryRing: "focus-visible:ring-emerald-500 focus:ring-emerald-500",
    softBg: "bg-emerald-50 dark:bg-emerald-950/45",
    softText: "text-emerald-700 dark:text-emerald-300",
    activeNavBg: "bg-emerald-50 dark:bg-emerald-950/55",
    activeNavText: "text-emerald-700 dark:text-emerald-200",
    badgeBg: "bg-emerald-100 dark:bg-emerald-950/65",
    badgeText: "text-emerald-800 dark:text-emerald-200",
    gradientFrom: "from-emerald-500",
    gradientTo: "to-teal-400"
  },
  {
    id: "rose-pink",
    name: "Rose Pink",
    description: "Creative, modern, marketing-friendly color range.",
    primaryBg: "bg-rose-600",
    primaryHoverBg: "hover:bg-rose-700",
    primaryText: "text-rose-700 dark:text-rose-300",
    primaryBorder: "border-rose-200 dark:border-rose-800",
    primaryRing: "focus-visible:ring-rose-500 focus:ring-rose-500",
    softBg: "bg-rose-50 dark:bg-rose-950/45",
    softText: "text-rose-700 dark:text-rose-300",
    activeNavBg: "bg-rose-50 dark:bg-rose-950/55",
    activeNavText: "text-rose-700 dark:text-rose-200",
    badgeBg: "bg-rose-100 dark:bg-rose-950/65",
    badgeText: "text-rose-800 dark:text-rose-200",
    gradientFrom: "from-rose-500",
    gradientTo: "to-pink-400"
  },
  {
    id: "amber-orange",
    name: "Amber Orange",
    description: "Warm startup palette with amber and orange energy.",
    primaryBg: "bg-amber-600",
    primaryHoverBg: "hover:bg-amber-700",
    primaryText: "text-amber-700 dark:text-amber-300",
    primaryBorder: "border-amber-200 dark:border-amber-800",
    primaryRing: "focus-visible:ring-amber-500 focus:ring-amber-500",
    softBg: "bg-amber-50 dark:bg-amber-950/45",
    softText: "text-amber-800 dark:text-amber-300",
    activeNavBg: "bg-amber-50 dark:bg-amber-950/55",
    activeNavText: "text-amber-800 dark:text-amber-200",
    badgeBg: "bg-amber-100 dark:bg-amber-950/65",
    badgeText: "text-amber-900 dark:text-amber-200",
    gradientFrom: "from-amber-500",
    gradientTo: "to-orange-500"
  },
  {
    id: "zinc-neutral",
    name: "Zinc Neutral",
    description: "Minimal enterprise look with neutral contrast.",
    primaryBg: "bg-zinc-900",
    primaryHoverBg: "hover:bg-zinc-800",
    primaryText: "text-zinc-800 dark:text-zinc-200",
    primaryBorder: "border-zinc-300 dark:border-zinc-700",
    primaryRing: "focus-visible:ring-zinc-500 focus:ring-zinc-500",
    softBg: "bg-zinc-100 dark:bg-zinc-900",
    softText: "text-zinc-800 dark:text-zinc-200",
    activeNavBg: "bg-zinc-100 dark:bg-zinc-900",
    activeNavText: "text-zinc-950 dark:text-zinc-50",
    badgeBg: "bg-zinc-200 dark:bg-zinc-800",
    badgeText: "text-zinc-900 dark:text-zinc-100",
    gradientFrom: "from-zinc-700",
    gradientTo: "to-zinc-400"
  }
] satisfies ThemePalette[];

export type ThemePaletteId = (typeof themePalettes)[number]["id"];

export function isThemePaletteId(value: string | null): value is ThemePaletteId {
  return themePalettes.some((palette) => palette.id === value);
}

export function getThemePalette(id: ThemePaletteId) {
  return themePalettes.find((palette) => palette.id === id) ?? themePalettes[0];
}

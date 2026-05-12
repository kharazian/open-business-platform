export type ThemeLayoutMode = "sidebar" | "collapsed" | "hover-collapsed" | "topnav" | "hybrid" | "minimal";

export const themeLayoutModes: Array<{ label: string; value: ThemeLayoutMode; description: string }> = [
  { label: "Sidebar", value: "sidebar", description: "Classic dashboard sidebar with top header." },
  { label: "Collapsed", value: "collapsed", description: "Icon-only sidebar for dense admin tools." },
  { label: "Hover Expand", value: "hover-collapsed", description: "Collapsed sidebar that expands while hovering." },
  { label: "Top Nav", value: "topnav", description: "Horizontal navigation with no left rail." },
  { label: "Hybrid", value: "hybrid", description: "Top product bar plus section sidebar." },
  { label: "Minimal", value: "minimal", description: "Simple top bar for focused screens." }
];

export function isThemeLayoutMode(value: string | null): value is ThemeLayoutMode {
  return themeLayoutModes.some((mode) => mode.value === value);
}

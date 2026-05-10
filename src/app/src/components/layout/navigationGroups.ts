import type { NavigationItem } from "../../config/appNavigation";

export function getNavigationSections(navigation: NavigationItem[]) {
  return navigation.reduce<Array<{ label: string; items: NavigationItem[] }>>((sections, item) => {
    const label = item.section ?? "";
    const currentSection = sections[sections.length - 1];

    if (currentSection?.label === label) {
      currentSection.items.push(item);
      return sections;
    }

    sections.push({ label, items: [item] });
    return sections;
  }, []);
}

export function isNavigationItemActive(pathname: string, item: NavigationItem) {
  if (item.children?.some((child) => isNavigationItemActive(pathname, child))) return true;
  if (!item.path) return false;
  if (item.path === "/" || item.path === "/theme") return pathname === item.path;
  return pathname === item.path || pathname.startsWith(`${item.path}/`);
}

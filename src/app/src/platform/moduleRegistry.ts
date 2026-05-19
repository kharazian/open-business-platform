import type { ComponentType, ReactNode } from "react";

export type ModuleOwner = "core" | "app";

export type NavigationItem = {
  label: string;
  path?: string;
  icon?: ComponentType<{ className?: string }>;
  permission?: string;
  section?: string;
  children?: NavigationItem[];
  external?: boolean;
};

export type PlatformRoute = {
  element: ReactNode;
  index?: boolean;
  path?: string;
  permission?: string;
  requiresAuth?: boolean;
};

export type ModuleNavigationItem = NavigationItem & {
  order?: number;
  permission?: string;
};

export type PlatformModule = {
  id: string;
  name: string;
  owner: ModuleOwner;
  order?: number;
  routes?: PlatformRoute[];
  navigation?: ModuleNavigationItem[];
  permissions?: string[];
  icon?: ComponentType<{ className?: string }>;
};

export function getModuleRoutes(modules: PlatformModule[]): PlatformRoute[] {
  return sortModules(modules).flatMap((module) => module.routes ?? []);
}

export function getModuleNavigation(modules: PlatformModule[]): NavigationItem[] {
  return sortModules(modules)
    .flatMap((module) => module.navigation ?? [])
    .sort(compareOrderedItems)
    .map(({ order: _order, ...item }) => item);
}

export function filterNavigationByPermissions(items: NavigationItem[], permissions: ReadonlySet<string>): NavigationItem[] {
  return items.flatMap((item) => {
    const children = item.children ? filterNavigationByPermissions(item.children, permissions) : undefined;
    const hasPermission = !item.permission || permissions.has(item.permission);

    if (!hasPermission && (!children || children.length === 0)) {
      return [];
    }

    if (children && children.length > 0) {
      return [{ ...item, children }];
    }

    return hasPermission ? [item] : [];
  });
}

export function getModulesByOwner(modules: PlatformModule[], owner: ModuleOwner): PlatformModule[] {
  return sortModules(modules).filter((module) => module.owner === owner);
}

function sortModules(modules: PlatformModule[]) {
  return [...modules].sort(compareOrderedItems);
}

function compareOrderedItems(left: { name?: string; label?: string; order?: number }, right: { name?: string; label?: string; order?: number }) {
  const orderDifference = (left.order ?? 0) - (right.order ?? 0);

  if (orderDifference !== 0) {
    return orderDifference;
  }

  return (left.name ?? left.label ?? "").localeCompare(right.name ?? right.label ?? "");
}

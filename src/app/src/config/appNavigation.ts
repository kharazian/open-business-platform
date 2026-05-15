import { platformModules } from "../modules";
import { getModuleNavigation, type NavigationItem } from "../platform/moduleRegistry";

export type { NavigationItem };

export const appNavigation: NavigationItem[] = getModuleNavigation(platformModules);

import { X } from "lucide-react";
import { Sidebar } from "./Sidebar";
import { Button } from "../ui/Button";
import type { NavigationItem } from "../../config/appNavigation";

export function MobileDrawer({
  open,
  navigation,
  onClose,
  sidebarTitle,
  sidebarSubtitle,
  sidebarLogoText,
  sidebarVariant = "default",
  sidebarAriaLabel
}: {
  open: boolean;
  navigation: NavigationItem[];
  onClose: () => void;
  sidebarTitle?: string;
  sidebarSubtitle?: string;
  sidebarLogoText?: string;
  sidebarVariant?: "default" | "hybrid";
  sidebarAriaLabel?: string;
}) {
  if (!open) return null;

  return (
    <div className="fixed inset-0 z-50 lg:hidden">
      <button className="absolute inset-0 bg-foreground/35 backdrop-blur-sm" type="button" onClick={onClose} aria-label="Close menu" />
      <div className="relative h-full w-72">
        <div className="absolute right-3 top-3 z-10">
          <Button className="size-9 p-0" variant="outline" onClick={onClose} aria-label="Close menu">
            <X className="size-4" />
          </Button>
        </div>
        <Sidebar
          ariaLabel={sidebarAriaLabel}
          collapsed={false}
          logoText={sidebarLogoText}
          navigation={navigation}
          onNavigate={onClose}
          subtitle={sidebarSubtitle}
          title={sidebarTitle}
          variant={sidebarVariant}
        />
      </div>
    </div>
  );
}

import type { ReactNode } from "react";
import { useEffect, useState } from "react";
import { cn } from "../../lib/cn";
import { useDesignTheme } from "../../context/useDesignTheme";

type DropdownItem = {
  label: string;
  onClick: () => void;
};

type DropdownPlacement = "bottom-left" | "bottom-right" | "right-start";

const placementClasses: Record<DropdownPlacement, string> = {
  "bottom-left": "left-0 mt-2",
  "bottom-right": "right-0 mt-2",
  "right-start": "left-full top-0 ml-2"
};

export function Dropdown({
  trigger,
  children,
  items,
  ariaLabel,
  align = "right",
  placement,
  closeOnContentClick = false,
  contentClassName,
  className,
  triggerClassName
}: {
  trigger: ReactNode;
  children?: ReactNode;
  items?: DropdownItem[];
  ariaLabel?: string;
  align?: "left" | "right";
  placement?: DropdownPlacement;
  closeOnContentClick?: boolean;
  contentClassName?: string;
  className?: string;
  triggerClassName?: string;
}) {
  const [open, setOpen] = useState(false);
  const { palette } = useDesignTheme();
  const resolvedPlacement = placement ?? (align === "right" ? "bottom-right" : "bottom-left");

  useEffect(() => {
    if (!open) return;

    const closeOnEscape = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        setOpen(false);
      }
    };

    window.addEventListener("keydown", closeOnEscape);
    return () => window.removeEventListener("keydown", closeOnEscape);
  }, [open]);

  return (
    <div className={cn("relative", className)}>
      <button
        className={cn("rounded-xl focus-visible:outline-none focus-visible:ring-4", palette.primaryRing, triggerClassName)}
        type="button"
        onClick={() => setOpen((value) => !value)}
        aria-expanded={open}
        aria-label={ariaLabel}
      >
        {trigger}
      </button>
      {open ? (
        <>
          <button className="fixed inset-0 z-20 cursor-default" type="button" aria-label="Close dropdown" onClick={() => setOpen(false)} />
          <div
            className={cn(
              "absolute z-30 max-h-[calc(100vh-5rem)] min-w-48 overflow-y-auto overscroll-contain rounded-xl border border-border bg-card p-2 shadow-lifted",
              placementClasses[resolvedPlacement],
              contentClassName
            )}
            onClickCapture={closeOnContentClick ? () => setOpen(false) : undefined}
          >
            {items
              ? items.map((item) => (
                  <button
                    className="block w-full rounded-lg px-3 py-2 text-left text-sm font-semibold text-foreground hover:bg-muted"
                    key={item.label}
                    type="button"
                    onClick={() => {
                      item.onClick();
                      setOpen(false);
                    }}
                  >
                    {item.label}
                  </button>
                ))
              : children}
          </div>
        </>
      ) : null}
    </div>
  );
}

import type { ReactNode } from "react";
import { useEffect, useState } from "react";
import { cn } from "../../lib/cn";
import { useDesignTheme } from "../../context/useDesignTheme";

type DropdownItem = {
  label: string;
  onClick: () => void;
};

export function Dropdown({
  trigger,
  children,
  items,
  ariaLabel,
  align = "right",
  closeOnContentClick = false,
  contentClassName
}: {
  trigger: ReactNode;
  children?: ReactNode;
  items?: DropdownItem[];
  ariaLabel?: string;
  align?: "left" | "right";
  closeOnContentClick?: boolean;
  contentClassName?: string;
}) {
  const [open, setOpen] = useState(false);
  const { palette } = useDesignTheme();

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
    <div className="relative">
      <button
        className={cn("rounded-xl focus-visible:outline-none focus-visible:ring-4", palette.primaryRing)}
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
              "absolute z-30 mt-2 max-h-[calc(100vh-5rem)] min-w-48 overflow-y-auto overscroll-contain rounded-xl border border-border bg-card p-2 shadow-lifted",
              align === "right" ? "right-0" : "left-0",
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

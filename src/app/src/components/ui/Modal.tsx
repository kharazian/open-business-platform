import type { ReactNode } from "react";
import { X } from "lucide-react";
import { Button } from "./Button";
import { useDesignTheme } from "../../context/useDesignTheme";
import { cn } from "../../lib/cn";

export function Modal({
  open,
  title,
  description,
  children,
  footer,
  panelClassName,
  onClose
}: {
  open: boolean;
  title: string;
  description?: string;
  children: ReactNode;
  footer?: ReactNode;
  panelClassName?: string;
  onClose: () => void;
}) {
  const { densityClasses } = useDesignTheme();

  if (!open) return null;

  return (
    <div className="fixed inset-0 z-50 grid place-items-center bg-foreground/35 p-4 backdrop-blur-sm" role="dialog" aria-modal="true">
      <div className={cn("surface w-full max-w-lg overflow-hidden", panelClassName)}>
        <div className={cn("flex items-start justify-between gap-4 border-b border-border", densityClasses.cardPadding)}>
          <div>
            <h2 className="text-xl font-bold text-foreground">{title}</h2>
            {description ? <p className="mt-1 text-sm text-muted-foreground">{description}</p> : null}
          </div>
          <Button variant="ghost" className="size-9 p-0" onClick={onClose} aria-label="Close modal">
            <X className="size-4" />
          </Button>
        </div>
        <div className={densityClasses.cardPadding}>{children}</div>
        {footer ? <div className={cn("flex justify-end gap-2 border-t border-border", densityClasses.cardPadding)}>{footer}</div> : null}
      </div>
    </div>
  );
}

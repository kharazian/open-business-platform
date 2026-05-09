import type { ReactNode } from "react";
import { AlertCircle } from "lucide-react";
import { cn } from "../../lib/cn";
import { useDesignTheme } from "../../context/useDesignTheme";

export function Alert({ title, children }: { title: string; children: ReactNode }) {
  const { palette } = useDesignTheme();

  return (
    <div className={cn("rounded-2xl border p-4", palette.primaryBorder, palette.softBg)}>
      <div className="flex gap-3">
        <AlertCircle className={cn("mt-0.5 size-5 shrink-0", palette.primaryText)} />
        <div>
          <h3 className="text-sm font-bold text-foreground">{title}</h3>
          <div className="mt-1 text-sm leading-6 text-muted-foreground">{children}</div>
        </div>
      </div>
    </div>
  );
}

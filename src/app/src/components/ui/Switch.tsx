import type { InputHTMLAttributes } from "react";
import { cn } from "../../lib/cn";
import { useDesignTheme } from "../../context/useDesignTheme";

type SwitchProps = Omit<InputHTMLAttributes<HTMLInputElement>, "type"> & {
  label: string;
  description?: string;
};

export function Switch({ label, description, className, ...props }: SwitchProps) {
  const { palette } = useDesignTheme();

  return (
    <label className={cn("flex cursor-pointer items-center justify-between gap-4 rounded-xl border border-border bg-card/70 p-3 transition hover:bg-muted/50", className)}>
      <span>
        <span className="block text-sm font-bold text-foreground">{label}</span>
        {description ? <span className="mt-0.5 block text-sm text-muted-foreground">{description}</span> : null}
      </span>
      <input className="peer sr-only" type="checkbox" {...props} />
      <span className="relative h-6 w-11 shrink-0 rounded-full bg-muted transition peer-checked:bg-current peer-focus-visible:ring-4 peer-disabled:opacity-60 peer-checked:[&>span:last-child]:translate-x-5">
        <span className={cn("absolute inset-0 rounded-full", palette.primaryText)} />
        <span className="absolute left-1 top-1 size-4 rounded-full bg-white shadow transition" />
      </span>
    </label>
  );
}

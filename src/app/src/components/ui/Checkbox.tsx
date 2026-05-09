import type { InputHTMLAttributes } from "react";
import { cn } from "../../lib/cn";
import { useDesignTheme } from "../../context/useDesignTheme";

type CheckboxProps = InputHTMLAttributes<HTMLInputElement> & {
  label: string;
  description?: string;
};

export function Checkbox({ label, description, className, ...props }: CheckboxProps) {
  const { palette } = useDesignTheme();

  return (
    <label className={cn("flex cursor-pointer items-start gap-3 rounded-xl border border-border bg-card/70 p-3 transition hover:bg-muted/50", className)}>
      <input className={cn("mt-1 size-4 rounded border-border", palette.primaryText)} type="checkbox" {...props} />
      <span>
        <span className="block text-sm font-bold text-foreground">{label}</span>
        {description ? <span className="mt-0.5 block text-sm text-muted-foreground">{description}</span> : null}
      </span>
    </label>
  );
}

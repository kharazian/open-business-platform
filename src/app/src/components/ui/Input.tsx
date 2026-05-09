import type { InputHTMLAttributes, ReactNode } from "react";
import { cn } from "../../lib/cn";
import { useDesignTheme } from "../../context/useDesignTheme";

type InputProps = InputHTMLAttributes<HTMLInputElement> & {
  label?: string;
  icon?: ReactNode;
  help?: string;
  helperText?: string;
  error?: string;
};

export function Input({ className, label, icon, help, helperText, error, id, ...props }: InputProps) {
  const inputId = id ?? (label ? label.toLowerCase().replace(/\s+/g, "-") : undefined);
  const { palette, densityClasses } = useDesignTheme();

  return (
    <label className="block">
      {label ? <span className="mb-2 block text-sm font-bold text-foreground">{label}</span> : null}
      <span className="relative block">
        {icon ? <span className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground">{icon}</span> : null}
        <input
          id={inputId}
          className={cn(
            "w-full rounded-xl border bg-card/90 px-3 text-sm text-foreground outline-none transition placeholder:text-muted-foreground/70 focus:ring-4 disabled:cursor-not-allowed disabled:opacity-60",
            error ? "border-danger" : "border-border",
            densityClasses.controlHeight,
            palette.primaryRing,
            icon ? "pl-10" : "",
            className
          )}
          {...props}
        />
      </span>
      {error ? <span className="mt-1.5 block text-xs font-semibold text-danger">{error}</span> : null}
      {!error && (help || helperText) ? <span className="mt-1.5 block text-xs text-muted-foreground">{help ?? helperText}</span> : null}
    </label>
  );
}

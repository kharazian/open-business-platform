import type { SelectHTMLAttributes } from "react";
import { cn } from "../../lib/cn";
import { useDesignTheme } from "../../context/useDesignTheme";

type SelectProps = SelectHTMLAttributes<HTMLSelectElement> & {
  label?: string;
  help?: string;
  error?: string;
  options?: Array<{ label: string; value: string }>;
};

export function Select({ className, children, label, help, error, options, id, ...props }: SelectProps) {
  const selectId = id ?? (label ? label.toLowerCase().replace(/\s+/g, "-") : undefined);
  const { palette, densityClasses } = useDesignTheme();

  return (
    <label className="block">
      {label ? <span className="mb-2 block text-sm font-bold text-foreground">{label}</span> : null}
      <select
        id={selectId}
        className={cn(
          "w-full rounded-xl border bg-card/90 px-3 text-sm font-semibold text-foreground outline-none transition focus:ring-4 disabled:cursor-not-allowed disabled:opacity-60",
          error ? "border-danger" : "border-border",
          densityClasses.controlHeight,
          palette.primaryRing,
          className
        )}
        {...props}
      >
        {options
          ? options.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))
          : children}
      </select>
      {error ? <span className="mt-1.5 block text-xs font-semibold text-danger">{error}</span> : null}
      {!error && help ? <span className="mt-1.5 block text-xs text-muted-foreground">{help}</span> : null}
    </label>
  );
}

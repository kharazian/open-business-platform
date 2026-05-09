import type { TextareaHTMLAttributes } from "react";
import { cn } from "../../lib/cn";
import { useDesignTheme } from "../../context/useDesignTheme";

type TextareaProps = TextareaHTMLAttributes<HTMLTextAreaElement> & {
  label?: string;
  help?: string;
  error?: string;
};

export function Textarea({ className, label, help, error, id, ...props }: TextareaProps) {
  const { palette } = useDesignTheme();
  const textareaId = id ?? (label ? label.toLowerCase().replace(/\s+/g, "-") : undefined);

  return (
    <label className="block">
      {label ? <span className="mb-2 block text-sm font-bold text-foreground">{label}</span> : null}
      <textarea
        id={textareaId}
        className={cn(
          "min-h-28 w-full resize-y rounded-xl border bg-card/90 px-3 py-2.5 text-sm text-foreground outline-none transition placeholder:text-muted-foreground/70 focus:ring-4 disabled:cursor-not-allowed disabled:opacity-60",
          error ? "border-danger focus-visible:ring-danger" : "border-border",
          palette.primaryRing,
          className
        )}
        {...props}
      />
      {error ? <span className="mt-1.5 block text-xs font-semibold text-danger">{error}</span> : null}
      {!error && help ? <span className="mt-1.5 block text-xs text-muted-foreground">{help}</span> : null}
    </label>
  );
}

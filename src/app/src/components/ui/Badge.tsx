import type { HTMLAttributes } from "react";
import { cn } from "../../lib/cn";
import { useDesignTheme } from "../../context/useDesignTheme";

type BadgeTone = "default" | "info" | "success" | "warning" | "danger" | "indigo";

const tones: Record<BadgeTone, string> = {
  default: "border-border bg-muted text-muted-foreground",
  info: "border-primary/25 bg-primary-soft text-primary",
  success: "border-success/25 bg-success-soft text-success",
  warning: "border-warning/25 bg-warning-soft text-warning",
  danger: "border-danger/25 bg-danger-soft text-danger",
  indigo: "border-indigo/25 bg-indigo-soft text-indigo"
};

export function Badge({
  className,
  tone,
  variant,
  ...props
}: HTMLAttributes<HTMLSpanElement> & { tone?: BadgeTone; variant?: BadgeTone }) {
  const { palette } = useDesignTheme();
  const selectedTone = tone ?? variant ?? "default";
  const toneClass = selectedTone === "info" ? `${palette.primaryBorder} ${palette.badgeBg} ${palette.badgeText}` : tones[selectedTone];

  return <span className={cn("inline-flex min-h-7 items-center rounded-full border px-2.5 py-1 text-xs font-bold", toneClass, className)} {...props} />;
}

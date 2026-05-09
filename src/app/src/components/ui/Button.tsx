import type { ButtonHTMLAttributes, ReactNode } from "react";
import { cn } from "../../lib/cn";
import { useDesignTheme } from "../../context/useDesignTheme";

type ButtonVariant = "primary" | "secondary" | "outline" | "ghost" | "danger";
type ButtonSize = "sm" | "md" | "lg" | "icon";

const variants: Record<ButtonVariant, string> = {
  primary: "bg-primary text-primary-foreground shadow-lifted hover:bg-primary/90",
  secondary: "bg-card-muted text-foreground hover:bg-muted",
  outline: "border border-border bg-card/90 text-foreground hover:bg-muted",
  ghost: "text-muted-foreground hover:bg-muted hover:text-foreground",
  danger: "bg-danger text-white hover:bg-danger/90"
};

export function Button({
  className,
  variant = "primary",
  size = "md",
  type = "button",
  children,
  ...props
}: ButtonHTMLAttributes<HTMLButtonElement> & { variant?: ButtonVariant; size?: ButtonSize; children: ReactNode }) {
  const { palette } = useDesignTheme();
  const variantClass = variant === "primary" ? `${palette.primaryBg} ${palette.primaryHoverBg} text-white shadow-lifted` : variants[variant];

  return (
    <button
      className={cn(
        "control-transition inline-flex items-center justify-center gap-2 rounded-xl text-sm font-bold outline-none ring-primary/20 focus-visible:ring-4 disabled:pointer-events-none disabled:opacity-50",
        size === "sm" ? "min-h-8 px-3" : size === "lg" ? "min-h-11 px-5 text-base" : size === "icon" ? "size-10 p-0" : "min-h-10 px-4",
        palette.primaryRing,
        variantClass,
        className
      )}
      type={type}
      {...props}
    >
      {children}
    </button>
  );
}

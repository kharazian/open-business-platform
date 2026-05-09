import { cn } from "../../lib/cn";
import { useDesignTheme } from "../../context/useDesignTheme";

export function Avatar({ name, size = "md", className }: { name: string; size?: "sm" | "md" | "lg"; className?: string }) {
  const { palette } = useDesignTheme();
  const initials = name
    .split(" ")
    .map((part) => part[0])
    .join("")
    .slice(0, 2)
    .toUpperCase();

  return (
    <span
      className={cn(
        "grid place-items-center rounded-full font-extrabold",
        palette.softBg,
        palette.softText,
        size === "sm" ? "size-8 text-xs" : size === "lg" ? "size-20 text-2xl" : "size-10 text-sm",
        className
      )}
    >
      {initials}
    </span>
  );
}

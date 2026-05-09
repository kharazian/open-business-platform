import { cn } from "../../lib/cn";
import { useDesignTheme } from "../../context/useDesignTheme";

export function Progress({ value, label }: { value: number; label?: string }) {
  const { palette } = useDesignTheme();
  const clampedValue = Math.max(0, Math.min(100, value));

  return (
    <div>
      {label ? (
        <div className="mb-2 flex justify-between text-sm">
          <span className="font-bold text-foreground">{label}</span>
          <span className="text-muted-foreground">{clampedValue}%</span>
        </div>
      ) : null}
      <div className="h-2.5 overflow-hidden rounded-full bg-muted">
        <div className={cn("h-full rounded-full transition-all", palette.primaryBg)} style={{ width: `${clampedValue}%` }} />
      </div>
    </div>
  );
}

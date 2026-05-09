import type { ComponentType } from "react";
import { Card } from "./Card";
import { useDesignTheme } from "../../context/useDesignTheme";

export function StatCard({
  label,
  value,
  hint,
  change,
  icon: Icon,
  tone,
  toneClassName
}: {
  label: string;
  value: string;
  hint?: string;
  change?: string;
  icon: ComponentType<{ className?: string }>;
  tone?: "success" | "info" | "indigo" | "warning";
  toneClassName?: string;
}) {
  const { palette } = useDesignTheme();
  const toneClasses = {
    success: "bg-emerald-500/10 text-emerald-600 dark:text-emerald-300",
    info: "bg-cyan-500/10 text-cyan-600 dark:text-cyan-300",
    indigo: "bg-indigo-500/10 text-indigo-600 dark:text-indigo-300",
    warning: "bg-amber-500/10 text-amber-600 dark:text-amber-300"
  };

  return (
    <Card className="p-5">
      <div className="flex items-start justify-between gap-4">
        <div>
          <p className="text-sm font-bold text-muted-foreground">{label}</p>
          <p className="mt-3 text-3xl font-bold text-foreground">{value}</p>
        </div>
        <span className={`grid size-10 place-items-center rounded-xl ${tone ? toneClasses[tone] : toneClassName ?? `${palette.softBg} ${palette.softText}`}`}>
          <Icon className="size-5" />
        </span>
      </div>
      <p className="mt-4 text-sm leading-6 text-muted-foreground">{change ?? hint}</p>
    </Card>
  );
}

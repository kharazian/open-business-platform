import type { ReactNode } from "react";
import { cn } from "../../lib/cn";

export function PageHeader({
  eyebrow,
  title,
  description,
  actions
}: {
  eyebrow?: string;
  title: string;
  description?: string;
  actions?: ReactNode;
}) {
  return (
    <section className="border-b border-border/80 pb-4">
      <div className="flex flex-col gap-3 md:flex-row md:items-start md:justify-between">
        <div className="min-w-0">
          {eyebrow ? <p className="text-xs font-bold uppercase tracking-normal text-muted-foreground">{eyebrow}</p> : null}
          <h1 className={cn("max-w-4xl text-xl font-bold tracking-normal text-foreground sm:text-2xl", eyebrow ? "mt-1" : undefined)}>
            {title}
          </h1>
          {description ? <p className="mt-1 max-w-3xl text-sm leading-5 text-muted-foreground">{description}</p> : null}
        </div>
        {actions ? <div className="flex shrink-0 flex-wrap gap-2 md:justify-end">{actions}</div> : null}
      </div>
    </section>
  );
}

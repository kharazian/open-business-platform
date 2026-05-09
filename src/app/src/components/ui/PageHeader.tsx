import type { ReactNode } from "react";
import { Badge } from "./Badge";
import { cn } from "../../lib/cn";
import { useDesignTheme } from "../../context/useDesignTheme";

export function PageHeader({
  eyebrow,
  title,
  description,
  actions
}: {
  eyebrow?: string;
  title: string;
  description: string;
  actions?: ReactNode;
}) {
  const { densityClasses } = useDesignTheme();

  return (
    <section className={cn("surface", densityClasses.cardPadding)}>
      <div className="grid gap-5 md:grid-cols-[minmax(0,1fr)_auto] md:items-end">
        <div>
          {eyebrow ? <Badge tone="info">{eyebrow}</Badge> : null}
          <h1 className="mt-4 max-w-4xl text-3xl font-bold tracking-normal text-foreground sm:text-4xl">{title}</h1>
          <p className="mt-3 max-w-3xl text-sm leading-6 text-muted-foreground sm:text-base">{description}</p>
        </div>
        {actions ? <div className="flex flex-wrap gap-2">{actions}</div> : null}
      </div>
    </section>
  );
}

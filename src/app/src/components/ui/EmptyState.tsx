import type { ReactNode } from "react";
import { Inbox } from "lucide-react";
import { Button } from "./Button";
import { useDesignTheme } from "../../context/useDesignTheme";
import { cn } from "../../lib/cn";

export function EmptyState({ title, description, action }: { title: string; description: string; action?: ReactNode }) {
  const { palette } = useDesignTheme();

  return (
    <div className="rounded-xl border border-dashed border-border bg-muted/40 p-8 text-center">
      <span className={cn("mx-auto grid size-12 place-items-center rounded-xl", palette.softBg, palette.softText)}>
        <Inbox className="size-6" />
      </span>
      <h3 className="mt-4 text-lg font-bold text-foreground">{title}</h3>
      <p className="mx-auto mt-2 max-w-md text-sm leading-6 text-muted-foreground">{description}</p>
      {action ? <div className="mt-5">{action}</div> : <Button className="mt-5">Create item</Button>}
    </div>
  );
}

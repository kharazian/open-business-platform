import { cn } from "../../lib/cn";
import { useDesignTheme } from "../../context/useDesignTheme";

export type TimelineItem = {
  title: string;
  description: string;
  time: string;
};

export function Timeline({ items }: { items: TimelineItem[] }) {
  const { palette } = useDesignTheme();

  return (
    <div className="space-y-4">
      {items.map((item) => (
        <div className="flex gap-3" key={`${item.title}-${item.time}`}>
          <span className={cn("mt-1.5 size-2.5 rounded-full", palette.primaryBg)} />
          <div>
            <p className="text-sm font-bold text-foreground">{item.title}</p>
            <p className="mt-0.5 text-sm text-muted-foreground">{item.description}</p>
            <p className="mt-1 text-xs font-semibold text-muted-foreground">{item.time}</p>
          </div>
        </div>
      ))}
    </div>
  );
}

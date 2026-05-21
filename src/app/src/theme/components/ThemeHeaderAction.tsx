import type { ComponentProps, ComponentType, ReactNode } from "react";
import type { LucideProps } from "lucide-react";
import { Button } from "../../components/ui/Button";
import { cn } from "../../lib/cn";

type ThemeHeaderActionProps = Omit<ComponentProps<typeof Button>, "children"> & {
  children: ReactNode;
  icon: ComponentType<LucideProps>;
};

export function ThemeHeaderAction({ children, className, icon: Icon, ...props }: ThemeHeaderActionProps) {
  return (
    <Button className={cn("gap-2.5", className)} {...props}>
      <Icon aria-hidden="true" className="size-5 shrink-0" strokeWidth={2.35} />
      <span>{children}</span>
    </Button>
  );
}

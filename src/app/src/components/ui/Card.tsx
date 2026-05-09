import type { HTMLAttributes, ReactNode } from "react";
import { cn } from "../../lib/cn";
import { useDesignTheme } from "../../context/useDesignTheme";

type CardProps = HTMLAttributes<HTMLDivElement> & {
  title?: string;
  description?: string;
  children?: ReactNode;
};

export function Card({ className, title, description, children, ...props }: CardProps) {
  const { densityClasses } = useDesignTheme();

  return (
    <div className={cn("surface min-w-0", densityClasses.cardPadding, className)} {...props}>
      {title || description ? (
        <div className="mb-5">
          {title ? <h2 className="text-lg font-bold text-foreground">{title}</h2> : null}
          {description ? <p className="mt-1 text-sm text-muted-foreground">{description}</p> : null}
        </div>
      ) : null}
      {children}
    </div>
  );
}

export function CardHeader({ className, ...props }: HTMLAttributes<HTMLDivElement>) {
  return <div className={cn("border-b border-border px-5 py-4", className)} {...props} />;
}

export function CardContent({ className, ...props }: HTMLAttributes<HTMLDivElement>) {
  return <div className={cn("p-5", className)} {...props} />;
}

export function CardTitle({ className, children, ...props }: HTMLAttributes<HTMLHeadingElement> & { children: ReactNode }) {
  return (
    <h2 className={cn("text-lg font-bold tracking-normal text-foreground", className)} {...props}>
      {children}
    </h2>
  );
}

export function CardDescription({ className, ...props }: HTMLAttributes<HTMLParagraphElement>) {
  return <p className={cn("mt-1 text-sm leading-6 text-muted-foreground", className)} {...props} />;
}

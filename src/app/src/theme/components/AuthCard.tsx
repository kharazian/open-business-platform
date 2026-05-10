import type { ReactNode } from "react";
import { cn } from "../../lib/cn";
import { Card } from "../../components/ui/Card";
import { useThemeAppearance } from "../../context/ThemeAppearanceContext";

export function AuthCard({ title, description, children }: { title: string; description: string; children: ReactNode }) {
  const { palette } = useThemeAppearance();

  return (
    <div className="mx-auto flex min-h-[70vh] w-full max-w-md items-center">
      <Card>
        <div className="mb-8 text-center">
          <div className={cn("mx-auto flex size-12 items-center justify-center rounded-2xl text-lg font-black text-white", palette.primaryBg)}>
            OB
          </div>
          <h1 className="mt-5 text-2xl font-bold text-foreground">{title}</h1>
          <p className="mt-2 text-sm text-muted-foreground">{description}</p>
        </div>
        {children}
      </Card>
    </div>
  );
}

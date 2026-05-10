import { CheckCircle2 } from "lucide-react";
import { themeLayoutModes, type ThemeLayoutMode } from "../../config/themeLayoutModes";
import { useThemeAppearance } from "../../context/ThemeAppearanceContext";
import { Badge } from "../../components/ui/Badge";
import { Button } from "../../components/ui/Button";
import { Card } from "../../components/ui/Card";
import { PageHeader } from "../../components/ui/PageHeader";
import { cn } from "../../lib/cn";

function LayoutPreview({ mode, active }: { mode: ThemeLayoutMode; active: boolean }) {
  const { palette } = useThemeAppearance();
  const hasSidebar = mode === "sidebar" || mode === "collapsed" || mode === "hybrid";
  const hasTopNav = mode === "topnav" || mode === "hybrid" || mode === "minimal";

  return (
    <div className="h-36 overflow-hidden rounded-xl border border-border bg-muted/45 p-3">
      {hasTopNav ? <div className={cn("mb-3 h-5 rounded-lg bg-gradient-to-r", palette.gradientFrom, palette.gradientTo)} /> : null}
      <div className="flex h-full gap-3">
        {hasSidebar ? <div className={cn(mode === "collapsed" ? "w-8" : "w-16", "rounded-xl", active ? palette.primaryBg : "bg-muted-foreground")} /> : null}
        <div className="grid flex-1 gap-2">
          <div className="rounded-xl bg-card" />
          <div className="grid grid-cols-3 gap-2">
            <div className="rounded-xl bg-card" />
            <div className="rounded-xl bg-card" />
            <div className="rounded-xl bg-card" />
          </div>
        </div>
      </div>
    </div>
  );
}

export function ThemeLayouts() {
  const { layoutMode, setLayoutMode } = useThemeAppearance();

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Navigation patterns"
        title="Layouts"
        description="Switch between sidebar, collapsed sidebar, top navigation, hybrid, and minimal modes. The selected mode persists after refresh."
      />

      <div className="grid gap-5 lg:grid-cols-2 xl:grid-cols-3">
        {themeLayoutModes.map((layout) => (
          <Card key={layout.value}>
            <LayoutPreview mode={layout.value} active={layoutMode === layout.value} />
            <div className="mt-5 flex items-start justify-between gap-4">
              <div>
                <div className="flex items-center gap-2">
                  <h2 className="font-semibold text-foreground">{layout.label}</h2>
                  {layoutMode === layout.value ? <Badge tone="info">Active</Badge> : null}
                </div>
                <p className="mt-2 text-sm text-muted-foreground">{layout.description}</p>
              </div>
              {layoutMode === layout.value ? <CheckCircle2 className="shrink-0 text-success" size={20} /> : null}
            </div>
            <Button className="mt-5 w-full justify-center" variant={layoutMode === layout.value ? "secondary" : "outline"} onClick={() => setLayoutMode(layout.value)}>
              Use {layout.label}
            </Button>
          </Card>
        ))}
      </div>
    </div>
  );
}

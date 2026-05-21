import { AlertTriangle, Clock, FileText, Hammer, Home, ServerCrash } from "lucide-react";
import { Badge } from "../../components/ui/Badge";
import { Button } from "../../components/ui/Button";
import { Card } from "../../components/ui/Card";
import { EmptyState } from "../../components/ui/EmptyState";
import { PageHeader } from "../../components/ui/PageHeader";
import { ThemeHeaderAction } from "../components/ThemeHeaderAction";

const utilityTemplates = [
  {
    title: "404 Not Found",
    code: "404",
    description: "Use when a route, record, or module entry cannot be found.",
    icon: AlertTriangle,
    tone: "warning"
  },
  {
    title: "Server Error",
    code: "500",
    description: "Use when an unexpected platform error blocks a request.",
    icon: ServerCrash,
    tone: "danger"
  },
  {
    title: "Maintenance",
    code: "503",
    description: "Use for planned downtime or module-level service windows.",
    icon: Hammer,
    tone: "info"
  },
  {
    title: "Coming Soon",
    code: "Soon",
    description: "Use for planned modules that are visible but not ready.",
    icon: Clock,
    tone: "indigo"
  }
] as const;

export function ThemeUtilityPages() {
  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Foundation"
        title="Utility Pages"
        description="Reusable empty, error, maintenance, coming-soon, and blank page templates for complete admin flows."
        actions={<ThemeHeaderAction icon={Home} variant="outline">Preview dashboard</ThemeHeaderAction>}
      />

      <div className="grid gap-5 lg:grid-cols-2 xl:grid-cols-4">
        {utilityTemplates.map((template) => {
          const Icon = template.icon;

          return (
            <Card key={template.title}>
              <div className="flex items-start justify-between gap-4">
                <span className="grid size-11 place-items-center rounded-xl bg-primary-soft text-primary">
                  <Icon size={20} />
                </span>
                <Badge tone={template.tone}>{template.code}</Badge>
              </div>
              <h2 className="mt-5 text-lg font-bold text-foreground">{template.title}</h2>
              <p className="mt-2 text-sm leading-6 text-muted-foreground">{template.description}</p>
              <Button className="mt-5 w-full justify-center" variant="outline">
                View pattern
              </Button>
            </Card>
          );
        })}
      </div>

      <div className="grid gap-6 xl:grid-cols-[1fr_1fr]">
        <Card title="Empty state">
          <EmptyState
            title="No audit events yet"
            description="Events will appear here when modules start writing to the audit stream."
            action={<Button size="sm">Open audit setup</Button>}
          />
        </Card>

        <Card title="Blank page">
          <div className="rounded-xl border border-dashed border-border bg-muted/35 p-8">
            <div className="flex items-start gap-3">
              <span className="grid size-11 shrink-0 place-items-center rounded-xl bg-card text-primary">
                <FileText size={20} />
              </span>
              <div>
                <h2 className="text-lg font-bold text-foreground">Blank module canvas</h2>
                <p className="mt-2 text-sm leading-6 text-muted-foreground">
                  Start new module pages with a page header, one primary content surface, and predictable actions.
                </p>
              </div>
            </div>
            <div className="mt-6 h-48 rounded-xl border border-border bg-card/80" />
          </div>
        </Card>
      </div>
    </div>
  );
}

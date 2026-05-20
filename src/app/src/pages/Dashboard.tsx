import { ArrowRight, BarChart3, ShieldCheck } from "lucide-react";
import { Badge } from "../components/ui/Badge";
import { Button } from "../components/ui/Button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "../components/ui/Card";
import { PageHeader } from "../components/ui/PageHeader";
import { Table, type TableColumn } from "../components/ui/Table";
import { dashboardStats, quickActions, recentActivity } from "../lib/data";

type ActivityRow = (typeof recentActivity)[number];

const activityColumns: Array<TableColumn<ActivityRow>> = [
  { header: "Event", accessor: "event" },
  { header: "Actor", accessor: "actor" },
  { header: "Time", accessor: "time" },
  {
    header: "Status",
    accessor: "status",
    render: (row) => <Badge variant={row.status === "Completed" ? "success" : "warning"}>{row.status}</Badge>
  }
];

const statToneClasses: Record<string, string> = {
  teal: "bg-primary-soft text-primary",
  indigo: "bg-indigo-soft text-indigo",
  amber: "bg-amber-soft text-amber",
  rose: "bg-rose-soft text-rose"
};

export function Dashboard() {
  return (
    <div className="grid gap-6">
      <PageHeader
        eyebrow="Workspace overview"
        title="Dashboard"
        description="Track users, reports, permissions, and operational activity from one admin workspace."
        actions={
          <div className="flex min-h-10 items-center gap-2 rounded-xl border border-border bg-card/90 px-3 text-sm font-semibold text-foreground">
            <ShieldCheck className="size-4 text-primary" />
            System healthy
          </div>
        }
      />

      <section className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        {dashboardStats.map((stat) => (
          <Card className="p-5" key={stat.label}>
            <div className="flex items-start justify-between gap-4">
              <div>
                <p className="text-sm font-bold text-muted-foreground">{stat.label}</p>
                <p className="mt-3 text-3xl font-bold text-foreground">{stat.value}</p>
              </div>
              <span className={`rounded-lg px-2.5 py-1 text-xs font-bold ${statToneClasses[stat.tone]}`}>
                {stat.change}
              </span>
            </div>
          </Card>
        ))}
      </section>

      <section className="grid min-w-0 gap-6 xl:grid-cols-[minmax(0,1fr)_22rem]">
        <Card>
          <CardHeader>
            <CardTitle>Recent activity</CardTitle>
            <CardDescription>Latest platform events and access changes.</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="grid gap-3 md:hidden">
              {recentActivity.map((activity) => (
                <div className="rounded-xl border border-border bg-muted/45 p-3" key={`${activity.event}-${activity.time}`}>
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <p className="font-bold text-foreground">{activity.event}</p>
                      <p className="mt-1 text-sm text-muted-foreground">{activity.actor}</p>
                    </div>
                    <Badge variant={activity.status === "Completed" ? "success" : "warning"}>{activity.status}</Badge>
                  </div>
                  <p className="mt-3 text-xs font-bold text-muted-foreground">{activity.time}</p>
                </div>
              ))}
            </div>
            <div className="hidden md:block">
              <Table columns={activityColumns} rows={recentActivity} />
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Quick actions</CardTitle>
            <CardDescription>Common administrator workflows.</CardDescription>
          </CardHeader>
          <CardContent className="grid gap-3">
            {quickActions.map((action) => {
              const Icon = action.icon;

              return (
                <button
                  className="control-transition flex items-start gap-3 rounded-xl border border-border bg-muted/50 p-3 text-left hover:bg-muted"
                  key={action.label}
                  type="button"
                >
                  <span className="grid size-9 place-items-center rounded-lg bg-card text-primary">
                    <Icon className="size-4" />
                  </span>
                  <span>
                    <span className="block font-bold text-foreground">{action.label}</span>
                    <span className="mt-1 block text-sm leading-5 text-muted-foreground">{action.description}</span>
                  </span>
                </button>
              );
            })}
            <Button className="mt-1 w-full" variant="outline">
              View all workflows
              <ArrowRight className="size-4" />
            </Button>
          </CardContent>
        </Card>
      </section>

      <Card className="p-5">
        <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
          <div className="flex items-start gap-3">
            <span className="grid size-10 shrink-0 place-items-center rounded-lg bg-indigo-soft text-indigo">
              <BarChart3 className="size-5" />
            </span>
            <div>
              <p className="font-bold text-foreground">Reporting baseline</p>
              <p className="mt-1 text-sm leading-6 text-muted-foreground">
                Connect real audit and module data when the backend entities are ready.
              </p>
            </div>
          </div>
          <Badge>Modular monolith</Badge>
        </div>
      </Card>
    </div>
  );
}

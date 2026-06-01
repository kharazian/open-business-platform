import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { BarChart3, FileText, RefreshCcw, ShieldCheck } from "lucide-react";
import { getDashboardSummary } from "../features/dashboards/api";
import type { DashboardActivityItem, DashboardSummary } from "../features/dashboards/types";
import { Alert } from "../components/ui/Alert";
import { Badge } from "../components/ui/Badge";
import { Button } from "../components/ui/Button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "../components/ui/Card";
import { PageHeader } from "../components/ui/PageHeader";
import { Skeleton } from "../components/ui/Skeleton";
import { Table, type TableColumn } from "../components/ui/Table";

type ActivityRow = DashboardActivityItem;

const activityColumns: Array<TableColumn<ActivityRow>> = [
  { header: "Event", accessor: "event" },
  { header: "Actor", accessor: "actor" },
  { header: "Time", render: (row) => formatActivityTime(row.createdAt) },
  {
    header: "Status",
    accessor: "status",
    render: (row) => <Badge variant={row.status === "Completed" ? "success" : "warning"}>{row.status}</Badge>
  }
];

const statToneClasses: Record<string, string> = {
  users: "bg-primary-soft text-primary",
  forms: "bg-indigo-soft text-indigo",
  records: "bg-success-soft text-success",
  reports: "bg-amber-soft text-amber",
  audit_logs: "bg-rose-soft text-rose"
};

const quickActions = [
  { label: "Review permissions", description: "Roles and form access.", icon: ShieldCheck, path: "/users" },
  { label: "Open forms", description: "Drafts and published forms.", icon: FileText, path: "/forms" },
  { label: "Run reports", description: "Saved report views.", icon: BarChart3, path: "/reports" }
];

export function Dashboard() {
  const navigate = useNavigate();
  const [summary, setSummary] = useState<DashboardSummary | null>(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const loadSummary = async (refresh = false) => {
    if (refresh) {
      setRefreshing(true);
    } else {
      setLoading(true);
    }

    setError(null);

    try {
      setSummary(await getDashboardSummary());
    } catch (summaryError) {
      setError(summaryError instanceof Error ? summaryError.message : "Dashboard request failed.");
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  useEffect(() => {
    void loadSummary();
  }, []);

  const metrics = summary?.metrics ?? [];
  const recentActivity = summary?.recentActivity ?? [];
  const metricCards = useMemo(
    () =>
      metrics.map((metric) => ({
        ...metric,
        valueText: new Intl.NumberFormat().format(metric.value),
        toneClass: statToneClasses[metric.key] ?? "bg-muted text-muted-foreground"
      })),
    [metrics]
  );

  return (
    <div className="grid gap-6">
      <PageHeader
        eyebrow="Workspace overview"
        title="Dashboard"
        description={summary?.title ?? "Open Business Platform"}
        actions={
          <Button disabled={loading || refreshing} onClick={() => void loadSummary(true)} variant="outline">
            <RefreshCcw className={refreshing ? "size-4 animate-spin" : "size-4"} />
            Refresh
          </Button>
        }
      />

      {error ? (
        <Alert title="Dashboard unavailable">
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <span>{error}</span>
            <Button disabled={loading || refreshing} onClick={() => void loadSummary(true)} size="sm" variant="outline">
              <RefreshCcw className={refreshing ? "size-4 animate-spin" : "size-4"} />
              Retry
            </Button>
          </div>
        </Alert>
      ) : null}

      <section className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        {loading
          ? Array.from({ length: 4 }, (_, index) => <Skeleton className="h-32" key={index} />)
          : metricCards.map((stat) => (
              <Card className="p-5" key={stat.key}>
                <div className="flex items-start justify-between gap-4">
                  <div>
                    <p className="text-sm font-bold text-muted-foreground">{stat.label}</p>
                    <p className="mt-3 text-3xl font-bold text-foreground">{stat.valueText}</p>
                  </div>
                  <span className={`rounded-lg px-2.5 py-1 text-xs font-bold ${stat.toneClass}`}>Live</span>
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
            {loading ? (
              <div className="grid gap-3">
                <Skeleton className="h-12" />
                <Skeleton className="h-12" />
                <Skeleton className="h-12" />
              </div>
            ) : recentActivity.length === 0 ? (
              <div className="rounded-xl border border-dashed border-border bg-muted/40 p-6 text-center">
                <p className="font-bold text-foreground">No recent activity</p>
                <p className="mt-1 text-sm leading-6 text-muted-foreground">Audit events will appear here after forms, records, reports, or access changes are recorded.</p>
              </div>
            ) : (
              <>
                <div className="grid gap-3 md:hidden">
                  {recentActivity.map((activity) => (
                    <div className="rounded-xl border border-border bg-muted/45 p-3" key={activity.id}>
                      <div className="flex items-start justify-between gap-3">
                        <div>
                          <p className="font-bold text-foreground">{activity.event}</p>
                          <p className="mt-1 text-sm text-muted-foreground">{activity.actor}</p>
                        </div>
                        <Badge variant={activity.status === "Completed" ? "success" : "warning"}>{activity.status}</Badge>
                      </div>
                      <p className="mt-3 text-xs font-bold text-muted-foreground">{formatActivityTime(activity.createdAt)}</p>
                    </div>
                  ))}
                </div>
                <div className="hidden md:block">
                  <Table columns={activityColumns} rows={recentActivity} />
                </div>
              </>
            )}
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
                  onClick={() => navigate(action.path)}
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
                {summary ? `${summary.metrics.length} live metrics are available for this workspace.` : "Workspace metrics are loading."}
              </p>
            </div>
          </div>
          <Badge>Modular monolith</Badge>
        </div>
      </Card>
    </div>
  );
}

function formatActivityTime(value: string) {
  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return "Unknown";
  }

  return new Intl.DateTimeFormat(undefined, {
    month: "short",
    day: "numeric",
    hour: "numeric",
    minute: "2-digit"
  }).format(date);
}

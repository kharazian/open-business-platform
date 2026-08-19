import { useEffect, useState } from "react";
import { ArrowRight, Plus } from "lucide-react";
import { Link } from "react-router-dom";
import { Badge } from "../../../components/ui/Badge";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "../../../components/ui/Card";
import { EmptyState } from "../../../components/ui/EmptyState";
import { PageHeader } from "../../../components/ui/PageHeader";
import { useAuth } from "../../../context/AuthContext";
import { listDashboards } from "../api";
import { resolveDashboardIcon } from "../navigation";
import type { DashboardSummaryItem } from "../types";

export function DashboardDirectoryPage() {
  const { user } = useAuth();
  const [dashboards, setDashboards] = useState<DashboardSummaryItem[]>([]);
  const [error, setError] = useState<string | null>(null);
  const canManage = user?.permissions.includes("dashboards.manage") ?? false;
  useEffect(() => { listDashboards().then((items) => setDashboards(items.filter((item) => item.publication.status === "published"))).catch((caught) => setError(caught instanceof Error ? caught.message : "Dashboards could not be loaded.")); }, []);
  return <div className="grid gap-6">
    <PageHeader eyebrow="Dashboards" title="Dashboard directory" description="Open the published dashboards available to you." actions={canManage ? <Link className="inline-flex min-h-10 items-center gap-2 rounded-xl bg-primary px-4 text-sm font-bold text-primary-foreground" to="/dashboard-builder"><Plus className="size-4" />Manage dashboards</Link> : undefined} />
    {error ? <p className="text-sm font-semibold text-danger">{error}</p> : null}
    {dashboards.length === 0 ? <EmptyState title="No dashboards available" description="Published dashboards you can access will appear here." /> : <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">{dashboards.map((dashboard) => { const Icon = resolveDashboardIcon(dashboard.publication.menuIcon); return <Card key={dashboard.id}><CardHeader><div className="flex items-start justify-between gap-3"><div className="flex min-w-0 gap-3">{Icon ? <Icon className="mt-0.5 size-5 shrink-0 text-primary" /> : null}<div><CardTitle>{dashboard.name}</CardTitle><CardDescription>{dashboard.description ?? "Published dashboard"}</CardDescription></div></div>{dashboard.isDefault ? <Badge tone="success">Default</Badge> : null}</div></CardHeader><CardContent className="grid gap-4"><p className="text-xs font-semibold text-muted-foreground">Published {new Date(dashboard.publishedAt ?? dashboard.updatedAt ?? dashboard.createdAt).toLocaleDateString()}</p><Link className="inline-flex min-h-10 items-center justify-center gap-2 rounded-xl border border-border px-4 text-sm font-bold text-foreground hover:bg-muted" to={`/dashboards/${dashboard.publication.slug}`}><ArrowRight className="size-4" />Open</Link></CardContent></Card>; })}</div>}
  </div>;
}

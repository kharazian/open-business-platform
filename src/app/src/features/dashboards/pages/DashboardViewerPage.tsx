import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { Alert } from "../../../components/ui/Alert";
import { getDashboardBySlug } from "../api";
import { SavedDashboardViewer } from "../components/SavedDashboardViewer";
import type { DashboardDetail } from "../types";

export function DashboardViewerPage() {
  const { slug = "" } = useParams();
  const [dashboard, setDashboard] = useState<DashboardDetail | null>(null);
  const [error, setError] = useState<string | null>(null);
  useEffect(() => { setDashboard(null); setError(null); getDashboardBySlug(slug).then(setDashboard).catch((caught) => setError(caught instanceof Error ? caught.message : "Dashboard could not be loaded.")); }, [slug]);
  if (error) return <Alert title="Dashboard unavailable">{error}</Alert>;
  if (!dashboard) return <p className="py-10 text-sm font-semibold text-muted-foreground">Loading dashboard…</p>;
  return <SavedDashboardViewer dashboard={dashboard} />;
}

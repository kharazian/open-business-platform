import { BellRing, FileText, ShieldCheck } from "lucide-react";
import { sampleActivities, sampleDashboardStats, sampleReports, sampleUsers } from "./sampleData";

export const dashboardStats = sampleDashboardStats;

export const recentActivity = sampleActivities;

export const users = sampleUsers;

export const reportRows = sampleReports.map((report) => ({
  name: report.name,
  owner: report.owner,
  frequency: report.type,
  status: report.status
}));

export const quickActions = [
  { label: "Review permissions", description: "Check role coverage and stale access.", icon: ShieldCheck },
  { label: "Export audit log", description: "Generate a CSV for the current period.", icon: FileText },
  { label: "Notification rules", description: "Tune alert delivery by module.", icon: BellRing }
];

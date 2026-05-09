import {
  BarChart3,
  BellRing,
  FileText,
  LayoutDashboard,
  Palette,
  Settings,
  ShieldCheck,
  UserCircle,
  Users
} from "lucide-react";

export const navigationItems = [
  { label: "Dashboard", path: "/", icon: LayoutDashboard },
  { label: "Users", path: "/users", icon: Users },
  { label: "Reports", path: "/reports", icon: BarChart3 },
  { label: "Settings", path: "/settings", icon: Settings },
  { label: "Profile", path: "/profile", icon: UserCircle },
  { label: "Theme", path: "/theme", icon: Palette }
];

export const dashboardStats = [
  { label: "Active users", value: "1,284", change: "+12.8%", tone: "teal" },
  { label: "Open reports", value: "38", change: "+4 this week", tone: "indigo" },
  { label: "Audit events", value: "9,421", change: "99.9% captured", tone: "amber" },
  { label: "Permission sets", value: "24", change: "6 modules", tone: "rose" }
];

export const recentActivity = [
  { event: "User invited", actor: "Maya Chen", time: "8 min ago", status: "Completed" },
  { event: "Role updated", actor: "Omar Patel", time: "32 min ago", status: "Review" },
  { event: "Report exported", actor: "Nora Wells", time: "1 hr ago", status: "Completed" },
  { event: "Policy synced", actor: "System", time: "2 hrs ago", status: "Completed" }
];

export const users = [
  { name: "Maya Chen", email: "maya@company.test", role: "Admin", status: "Active" },
  { name: "Omar Patel", email: "omar@company.test", role: "Manager", status: "Active" },
  { name: "Nora Wells", email: "nora@company.test", role: "Analyst", status: "Invited" },
  { name: "Leo Morgan", email: "leo@company.test", role: "Viewer", status: "Suspended" }
];

export const reportRows = [
  { name: "Operations overview", owner: "Analytics", frequency: "Daily", status: "Ready" },
  { name: "User access review", owner: "Security", frequency: "Weekly", status: "Draft" },
  { name: "Audit event export", owner: "Compliance", frequency: "Monthly", status: "Ready" }
];

export const quickActions = [
  { label: "Review permissions", description: "Check role coverage and stale access.", icon: ShieldCheck },
  { label: "Export audit log", description: "Generate a CSV for the current period.", icon: FileText },
  { label: "Notification rules", description: "Tune alert delivery by module.", icon: BellRing }
];

export const sampleUsers = [
  { name: "Maya Chen", email: "maya@company.test", role: "Admin", status: "Active", team: "Operations" },
  { name: "Omar Patel", email: "omar@company.test", role: "Manager", status: "Active", team: "Finance" },
  { name: "Nora Wells", email: "nora@company.test", role: "Analyst", status: "Invited", team: "Reports" },
  { name: "Leo Morgan", email: "leo@company.test", role: "Viewer", status: "Suspended", team: "Support" }
];

export const sampleActivities = [
  {
    event: "User invited",
    actor: "Maya Chen",
    time: "8 min ago",
    status: "Completed",
    summary: "Maya invited a finance reviewer"
  },
  {
    event: "Role updated",
    actor: "Omar Patel",
    time: "32 min ago",
    status: "Review",
    summary: "Omar updated manager permissions"
  },
  {
    event: "Report exported",
    actor: "Nora Wells",
    time: "1 hr ago",
    status: "Completed",
    summary: "Nora exported the audit activity report"
  },
  {
    event: "Policy synced",
    actor: "System",
    time: "2 hrs ago",
    status: "Completed",
    summary: "System completed the nightly audit sync"
  }
];

export const sampleReports = [
  { name: "Operations overview", owner: "Analytics", type: "Daily", status: "Ready", updated: "Today" },
  { name: "User access review", owner: "Security", type: "Weekly", status: "Draft", updated: "Yesterday" },
  { name: "Audit event export", owner: "Compliance", type: "Monthly", status: "Ready", updated: "2 days ago" },
  { name: "Customer health", owner: "Success", type: "Quarterly", status: "Scheduled", updated: "May 1" }
];

export const sampleRoles = [
  { name: "Platform Admin", users: 8, permissions: 42, status: "System", updated: "Today" },
  { name: "Manager", users: 24, permissions: 28, status: "Active", updated: "Yesterday" },
  { name: "Analyst", users: 17, permissions: 18, status: "Active", updated: "2 days ago" },
  { name: "Viewer", users: 61, permissions: 9, status: "Default", updated: "May 1" }
];

export const samplePermissions = [
  { key: "users.manage", module: "Users", level: "Write", risk: "High", assignedRoles: 2 },
  { key: "roles.assign", module: "Roles", level: "Admin", risk: "High", assignedRoles: 1 },
  { key: "reports.export", module: "Reports", level: "Execute", risk: "Medium", assignedRoles: 3 },
  { key: "audit.view", module: "Audit Logs", level: "Read", risk: "Low", assignedRoles: 4 }
];

export const sampleAuditLogs = [
  { event: "User invited", actor: "Maya Chen", module: "Users", severity: "Info", time: "8 min ago" },
  { event: "Role permissions changed", actor: "Omar Patel", module: "Roles", severity: "Warning", time: "32 min ago" },
  { event: "Audit export downloaded", actor: "Nora Wells", module: "Audit Logs", severity: "Info", time: "1 hr ago" },
  { event: "Failed sign-in blocked", actor: "System", module: "Authentication", severity: "Critical", time: "2 hrs ago" }
];

export const sampleDashboardStats = [
  { label: "Active users", value: "1,284", change: "+12.8%", tone: "teal" },
  { label: "Open reports", value: "38", change: "+4 this week", tone: "indigo" },
  { label: "Audit events", value: "9,421", change: "99.9% captured", tone: "amber" },
  { label: "Permission sets", value: "24", change: "6 modules", tone: "rose" }
];

export const sampleTrendHeights = [36, 58, 44, 70, 62, 86, 74, 92, 81, 98];

export const sampleWorkflowActions = ["Invite user", "Create role", "Run audit", "Open settings"];

export const sampleComponentRows = [
  { component: "Button", status: "Stable", usage: "Primary actions and secondary controls" },
  { component: "Card", status: "Stable", usage: "Grouped content and metrics" },
  { component: "Modal", status: "Preview", usage: "Focused flows and confirmations" }
];

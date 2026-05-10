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

export const sampleNotifications = [
  {
    title: "New access request",
    sender: "Maya Chen",
    channel: "Workspace",
    priority: "High",
    status: "Unread",
    time: "4 min ago",
    summary: "Maya requested access for a finance reviewer."
  },
  {
    title: "Weekly report is ready",
    sender: "Analytics",
    channel: "Reports",
    priority: "Normal",
    status: "Unread",
    time: "24 min ago",
    summary: "The operations overview report finished generating."
  },
  {
    title: "Role policy reviewed",
    sender: "Omar Patel",
    channel: "Access",
    priority: "Normal",
    status: "Read",
    time: "1 hr ago",
    summary: "Manager permissions were reviewed and approved."
  },
  {
    title: "Audit threshold warning",
    sender: "System",
    channel: "Governance",
    priority: "Critical",
    status: "Unread",
    time: "2 hrs ago",
    summary: "Failed sign-in volume crossed the review threshold."
  }
];

export const sampleCalendarEvents = [
  {
    title: "Access review",
    owner: "Security",
    date: "May 12",
    time: "09:00",
    type: "Review",
    status: "Confirmed",
    attendees: 8
  },
  {
    title: "Operations standup",
    owner: "Operations",
    date: "May 13",
    time: "10:30",
    type: "Meeting",
    status: "Confirmed",
    attendees: 14
  },
  {
    title: "Audit export",
    owner: "Compliance",
    date: "May 15",
    time: "14:00",
    type: "Task",
    status: "Scheduled",
    attendees: 3
  },
  {
    title: "Release readiness",
    owner: "Platform",
    date: "May 17",
    time: "11:00",
    type: "Planning",
    status: "Draft",
    attendees: 6
  }
];

export const sampleTasks = [
  {
    title: "Finalize user import",
    owner: "Maya Chen",
    area: "Users",
    priority: "High",
    status: "Todo",
    due: "May 12",
    progress: 20
  },
  {
    title: "Review manager role policy",
    owner: "Omar Patel",
    area: "Roles",
    priority: "Critical",
    status: "In Progress",
    due: "May 13",
    progress: 55
  },
  {
    title: "Publish audit summary",
    owner: "Nora Wells",
    area: "Reports",
    priority: "Normal",
    status: "In Progress",
    due: "May 15",
    progress: 70
  },
  {
    title: "Update notification defaults",
    owner: "Leo Morgan",
    area: "Settings",
    priority: "Normal",
    status: "Done",
    due: "May 16",
    progress: 100
  },
  {
    title: "Validate MFA recovery flow",
    owner: "Security",
    area: "Authentication",
    priority: "High",
    status: "Todo",
    due: "May 17",
    progress: 10
  }
];

export const sampleInvoices = [
  {
    number: "INV-2048",
    customer: "Northwind Operations",
    plan: "Business",
    amount: "$2,400",
    issued: "May 1",
    status: "Paid"
  },
  {
    number: "INV-2049",
    customer: "Contoso Finance",
    plan: "Business",
    amount: "$1,850",
    issued: "May 3",
    status: "Open"
  },
  {
    number: "INV-2050",
    customer: "Fabrikam Support",
    plan: "Starter",
    amount: "$680",
    issued: "May 6",
    status: "Overdue"
  },
  {
    number: "INV-2051",
    customer: "Tailspin Analytics",
    plan: "Enterprise",
    amount: "$4,900",
    issued: "May 8",
    status: "Draft"
  }
];

export const sampleFormFields = [
  { name: "Workspace name", type: "Text", required: true, owner: "Settings" },
  { name: "Support email", type: "Email", required: true, owner: "Settings" },
  { name: "Default module", type: "Select", required: false, owner: "Navigation" },
  { name: "Audit retention", type: "Select", required: true, owner: "Governance" },
  { name: "Enable notifications", type: "Switch", required: false, owner: "Notifications" }
];

export const sampleTableViews = [
  { name: "User directory", rows: 1284, columns: 6, status: "Ready", owner: "Identity" },
  { name: "Role matrix", rows: 12, columns: 8, status: "Review", owner: "Access" },
  { name: "Audit stream", rows: 9421, columns: 5, status: "Ready", owner: "Governance" },
  { name: "Report library", rows: 38, columns: 5, status: "Draft", owner: "Analytics" }
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

import { ArrowUpRight, FileText, ShieldCheck, Users } from "lucide-react";
import { themeActivities, themeReports, themeUsers } from "../mockData";
import { Badge } from "../../components/ui/Badge";
import { Button } from "../../components/ui/Button";
import { Card } from "../../components/ui/Card";
import { PageHeader } from "../../components/ui/PageHeader";
import { StatCard } from "../../components/ui/StatCard";
import { Table } from "../../components/ui/Table";
import { useThemeAppearance } from "../../context/ThemeAppearanceContext";
import { cn } from "../../lib/cn";

export function ThemeDashboard() {
  const { palette } = useThemeAppearance();

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Theme playground"
        title="Dashboard"
        description="A polished SaaS dashboard with modular cards, responsive grids, quick actions, and activity surfaces."
        actions={
          <>
            <Button variant="outline">Export</Button>
            <Button>New workflow</Button>
          </>
        }
      />

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <StatCard label="Active users" value="1,284" change="+12.4%" icon={Users} tone="success" />
        <StatCard label="Open reports" value="38" change="+5 today" icon={FileText} tone="info" />
        <StatCard label="Access reviews" value="94%" change="On track" icon={ShieldCheck} tone="indigo" />
        <StatCard label="Automations" value="17" change="+3 this week" icon={ArrowUpRight} tone="warning" />
      </div>

      <div className="grid gap-6 xl:grid-cols-[1.35fr_0.65fr]">
        <Card title="Operations trend" description="Placeholder surface for future charts.">
          <div className="flex h-72 items-end gap-3 rounded-2xl border border-dashed border-slate-300 bg-slate-50 p-5 dark:border-slate-700 dark:bg-slate-900/50">
            {[36, 58, 44, 70, 62, 86, 74, 92, 81, 98].map((height, index) => (
              <div key={index} className="flex flex-1 items-end">
                <div
                  className={cn("w-full rounded-t-xl bg-gradient-to-t", palette.gradientFrom, palette.gradientTo)}
                  style={{ height: `${height}%` }}
                />
              </div>
            ))}
          </div>
        </Card>

        <Card title="Recent activity">
          <div className="space-y-4">
            {themeActivities.map((activity, index) => (
              <div key={activity} className="flex gap-3">
                <span className={cn("mt-1 size-2 rounded-full", palette.primaryBg)} />
                <div>
                  <p className="text-sm font-medium text-slate-900 dark:text-white">{activity}</p>
                  <p className="text-xs text-slate-500 dark:text-slate-400">{index + 1}h ago</p>
                </div>
              </div>
            ))}
          </div>
        </Card>
      </div>

      <div className="grid gap-6 xl:grid-cols-[0.8fr_1.2fr]">
        <Card title="Quick actions">
          <div className="grid gap-3 sm:grid-cols-2">
            {["Invite user", "Create role", "Run audit", "Open settings"].map((action) => (
              <Button key={action} variant="outline" className="justify-start">
                {action}
              </Button>
            ))}
          </div>
        </Card>

        <Card title="Latest reports" description="Reusable table styling with status badges.">
          <Table
            columns={[
              { header: "Report", accessor: "name" },
              { header: "Owner", accessor: "owner" },
              {
                header: "Status",
                render: (report) => <Badge tone={report.status === "Ready" ? "success" : "warning"}>{report.status}</Badge>,
              },
            ]}
            data={themeReports.slice(0, 3)}
          />
        </Card>
      </div>

      <Card title="New users">
        <Table
          columns={[
            { header: "Name", accessor: "name" },
            { header: "Team", accessor: "team" },
            { header: "Role", render: (user) => <Badge tone="indigo">{user.role}</Badge> },
          ]}
          data={themeUsers.slice(0, 3)}
        />
      </Card>
    </div>
  );
}

import { ArrowUpRight, FileText, PlayCircle, ShieldCheck, Users } from "lucide-react";
import { themeActivities, themeReports, themeTrendHeights, themeUsers, themeWorkflowActions } from "../mockData";
import { Badge } from "../../components/ui/Badge";
import { Button } from "../../components/ui/Button";
import { Card } from "../../components/ui/Card";
import { PageHeader } from "../../components/ui/PageHeader";
import { StatCard } from "../../components/ui/StatCard";
import { Table } from "../../components/ui/Table";
import { ThemeHeaderAction } from "../components/ThemeHeaderAction";
import { useThemeAppearance } from "../../context/ThemeAppearanceContext";
import { cn } from "../../lib/cn";

export function ThemeDashboard() {
  const { palette } = useThemeAppearance();

  return (
    <div className="space-y-5">
      <PageHeader
        eyebrow="Theme playground"
        title="Dashboard"
        description="A higher-density dashboard view for sample users, reports, access reviews, and workflow activity."
        actions={
          <>
            <ThemeHeaderAction icon={PlayCircle} variant="outline">Run workflow</ThemeHeaderAction>
            <Button>Export snapshot</Button>
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
          <div className="flex h-72 items-end gap-3 rounded-xl border border-dashed border-border bg-muted/45 p-5">
            {themeTrendHeights.map((height, index) => (
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
                  <p className="text-sm font-medium text-foreground">{activity}</p>
                  <p className="text-xs text-muted-foreground">{index + 1}h ago</p>
                </div>
              </div>
            ))}
          </div>
        </Card>
      </div>

      <div className="grid gap-6 xl:grid-cols-[0.8fr_1.2fr]">
        <Card title="Quick actions">
          <div className="grid gap-3 sm:grid-cols-2">
            {themeWorkflowActions.map((action) => (
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

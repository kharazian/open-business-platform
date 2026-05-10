import { Download, Search, Table2 } from "lucide-react";
import { themeAuditLogs, themeTableViews, themeUsers } from "../mockData";
import { Badge } from "../../components/ui/Badge";
import { Button } from "../../components/ui/Button";
import { Card } from "../../components/ui/Card";
import { Input } from "../../components/ui/Input";
import { PageHeader } from "../../components/ui/PageHeader";
import { Select } from "../../components/ui/Select";
import { Table } from "../../components/ui/Table";

export function ThemeTables() {
  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Foundation"
        title="Tables"
        description="Table patterns for directories, audit streams, dense review lists, and reusable data surfaces."
        actions={
          <Button variant="outline">
            <Download size={16} />
            Export view
          </Button>
        }
      />

      <Card title="Table views">
        <Table
          columns={[
            { header: "View", accessor: "name" },
            { header: "Owner", accessor: "owner" },
            { header: "Rows", render: (view) => view.rows.toLocaleString() },
            { header: "Columns", render: (view) => view.columns.toLocaleString() },
            {
              header: "Status",
              render: (view) => (
                <Badge tone={view.status === "Ready" ? "success" : view.status === "Review" ? "warning" : "default"}>
                  {view.status}
                </Badge>
              )
            }
          ]}
          data={themeTableViews}
        />
      </Card>

      <div className="grid gap-6 xl:grid-cols-[1fr_1fr]">
        <Card title="Directory table">
          <div className="grid gap-3 pb-5 md:grid-cols-[1fr_180px_auto]">
            <Input icon={<Search size={16} />} placeholder="Search users..." />
            <Select>
              <option>All statuses</option>
              <option>Active</option>
              <option>Invited</option>
            </Select>
            <Button variant="outline">Filter</Button>
          </div>
          <Table
            columns={[
              { header: "Name", accessor: "name" },
              { header: "Team", accessor: "team" },
              { header: "Role", render: (user) => <Badge tone="indigo">{user.role}</Badge> },
              { header: "Status", render: (user) => <Badge tone={user.status === "Active" ? "success" : "warning"}>{user.status}</Badge> }
            ]}
            data={themeUsers.slice(0, 3)}
          />
        </Card>

        <Card title="Audit table">
          <div className="mb-5 flex items-center gap-3 text-sm text-muted-foreground">
            <Table2 className="size-4 text-primary" />
            <span>Compact event rows for compliance-heavy workflows.</span>
          </div>
          <Table
            columns={[
              { header: "Event", accessor: "event" },
              { header: "Actor", accessor: "actor" },
              { header: "Module", accessor: "module" },
              { header: "Severity", render: (event) => <Badge tone={event.severity === "Critical" ? "danger" : "info"}>{event.severity}</Badge> }
            ]}
            data={themeAuditLogs}
          />
        </Card>
      </div>
    </div>
  );
}

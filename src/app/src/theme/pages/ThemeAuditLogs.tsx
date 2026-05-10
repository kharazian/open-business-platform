import { Download, ScrollText, Search, ShieldAlert } from "lucide-react";
import { themeAuditLogs } from "../mockData";
import { Badge } from "../../components/ui/Badge";
import { Button } from "../../components/ui/Button";
import { Card } from "../../components/ui/Card";
import { Input } from "../../components/ui/Input";
import { PageHeader } from "../../components/ui/PageHeader";
import { Select } from "../../components/ui/Select";
import { StatCard } from "../../components/ui/StatCard";
import { Table } from "../../components/ui/Table";

function getSeverityTone(severity: string) {
  if (severity === "Critical") return "danger";
  if (severity === "Warning") return "warning";
  return "info";
}

export function ThemeAuditLogs() {
  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Governance"
        title="Audit Logs"
        description="Inspect security and workspace events with filters, severity badges, and export actions."
        actions={
          <Button variant="outline">
            <Download size={16} />
            Export
          </Button>
        }
      />

      <div className="grid gap-4 md:grid-cols-3">
        <StatCard label="Events today" value="9,421" hint="99.9% captured" icon={ScrollText} />
        <StatCard label="Warnings" value="18" hint="Open review" icon={ShieldAlert} tone="warning" />
        <StatCard label="Retention" value="365d" hint="Workspace policy" icon={ScrollText} tone="success" />
      </div>

      <Card title="Event stream">
        <div className="grid gap-3 pb-5 md:grid-cols-[1fr_180px_180px_auto]">
          <Input icon={<Search size={16} />} placeholder="Search events or actors..." />
          <Select>
            <option>All modules</option>
            <option>Users</option>
            <option>Roles</option>
            <option>Authentication</option>
          </Select>
          <Select>
            <option>All severities</option>
            <option>Info</option>
            <option>Warning</option>
            <option>Critical</option>
          </Select>
          <Button variant="outline">Filter</Button>
        </div>

        <Table
          columns={[
            { header: "Event", accessor: "event" },
            { header: "Actor", accessor: "actor" },
            { header: "Module", accessor: "module" },
            { header: "Time", accessor: "time" },
            { header: "Severity", render: (event) => <Badge tone={getSeverityTone(event.severity)}>{event.severity}</Badge> }
          ]}
          data={themeAuditLogs}
        />
      </Card>
    </div>
  );
}

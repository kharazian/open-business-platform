import { KeyRound, Search, ShieldAlert, ShieldCheck } from "lucide-react";
import { themePermissions } from "../mockData";
import { Badge } from "../../components/ui/Badge";
import { Button } from "../../components/ui/Button";
import { Card } from "../../components/ui/Card";
import { Input } from "../../components/ui/Input";
import { PageHeader } from "../../components/ui/PageHeader";
import { Select } from "../../components/ui/Select";
import { StatCard } from "../../components/ui/StatCard";
import { Table } from "../../components/ui/Table";
import { ThemeHeaderAction } from "../components/ThemeHeaderAction";

function getRiskTone(risk: string) {
  if (risk === "High") return "danger";
  if (risk === "Medium") return "warning";
  return "success";
}

export function ThemePermissions() {
  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Access control"
        title="Permissions"
        description="Catalog module permissions by risk, access level, and assigned roles."
        actions={
          <>
            <ThemeHeaderAction icon={ShieldAlert} variant="outline">Review risk</ThemeHeaderAction>
            <ThemeHeaderAction icon={KeyRound}>Add permission</ThemeHeaderAction>
          </>
        }
      />

      <div className="grid gap-4 md:grid-cols-3">
        <StatCard label="Permission keys" value="84" hint="Across modules" icon={KeyRound} />
        <StatCard label="High risk" value="9" hint="Needs review" icon={ShieldAlert} tone="warning" />
        <StatCard label="Covered modules" value="6" hint="Core MVP" icon={ShieldCheck} tone="success" />
      </div>

      <Card title="Permission catalog">
        <div className="grid gap-3 pb-5 md:grid-cols-[1fr_180px_180px_auto]">
          <Input icon={<Search size={16} />} placeholder="Search permissions..." />
          <Select>
            <option>All modules</option>
            <option>Users</option>
            <option>Roles</option>
            <option>Reports</option>
          </Select>
          <Select>
            <option>All risks</option>
            <option>High</option>
            <option>Medium</option>
            <option>Low</option>
          </Select>
          <Button variant="outline">Apply</Button>
        </div>

        <Table
          columns={[
            { header: "Permission", accessor: "key" },
            { header: "Module", accessor: "module" },
            { header: "Level", render: (permission) => <Badge tone="indigo">{permission.level}</Badge> },
            { header: "Roles", render: (permission) => permission.assignedRoles.toLocaleString() },
            { header: "Risk", render: (permission) => <Badge tone={getRiskTone(permission.risk)}>{permission.risk}</Badge> }
          ]}
          data={themePermissions}
        />
      </Card>
    </div>
  );
}

import { Plus, Search, ShieldCheck, Users } from "lucide-react";
import { themeRoles } from "../mockData";
import { Badge } from "../../components/ui/Badge";
import { Button } from "../../components/ui/Button";
import { Card } from "../../components/ui/Card";
import { Input } from "../../components/ui/Input";
import { PageHeader } from "../../components/ui/PageHeader";
import { Select } from "../../components/ui/Select";
import { StatCard } from "../../components/ui/StatCard";
import { Table } from "../../components/ui/Table";
import { ThemeHeaderAction } from "../components/ThemeHeaderAction";

export function ThemeRoles() {
  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Access control"
        title="Roles"
        description="Review role coverage, permission counts, and assignment health with reusable table patterns."
        actions={<ThemeHeaderAction icon={Plus}>New role</ThemeHeaderAction>}
      />

      <div className="grid gap-4 md:grid-cols-3">
        <StatCard label="Active roles" value="12" hint="4 shown here" icon={ShieldCheck} />
        <StatCard label="Assigned users" value="110" hint="+8 this month" icon={Users} tone="success" />
        <StatCard label="System roles" value="3" hint="Protected" icon={ShieldCheck} tone="warning" />
      </div>

      <Card title="Role directory">
        <div className="grid gap-3 pb-5 md:grid-cols-[1fr_180px_auto]">
          <Input icon={<Search size={16} />} placeholder="Search roles..." />
          <Select>
            <option>All statuses</option>
            <option>System</option>
            <option>Active</option>
            <option>Default</option>
          </Select>
          <Button variant="outline">Filter</Button>
        </div>

        <Table
          columns={[
            { header: "Role", accessor: "name" },
            { header: "Users", render: (role) => role.users.toLocaleString() },
            { header: "Permissions", render: (role) => role.permissions.toLocaleString() },
            { header: "Updated", accessor: "updated" },
            {
              header: "Status",
              render: (role) => <Badge tone={role.status === "System" ? "indigo" : "success"}>{role.status}</Badge>
            },
            {
              header: "Actions",
              render: () => (
                <div className="flex gap-2">
                  <Button size="sm" variant="outline">Edit</Button>
                  <Button size="sm" variant="ghost">View</Button>
                </div>
              )
            }
          ]}
          data={themeRoles}
        />
      </Card>
    </div>
  );
}

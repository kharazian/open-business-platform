import { useState } from "react";
import { Plus, Search } from "lucide-react";
import { themeUsers } from "../mockData";
import { Badge } from "../../components/ui/Badge";
import { Button } from "../../components/ui/Button";
import { Card } from "../../components/ui/Card";
import { EmptyState } from "../../components/ui/EmptyState";
import { Input } from "../../components/ui/Input";
import { Modal } from "../../components/ui/Modal";
import { PageHeader } from "../../components/ui/PageHeader";
import { Select } from "../../components/ui/Select";
import { Table } from "../../components/ui/Table";
import { ThemeHeaderAction } from "../components/ThemeHeaderAction";

export function ThemeUsers() {
  const [open, setOpen] = useState(false);

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Identity"
        title="Users"
        description="Search, filter, and manage people with role and status badges."
        actions={<ThemeHeaderAction icon={Plus} onClick={() => setOpen(true)}>Add user</ThemeHeaderAction>}
      />

      <Card>
        <div className="grid gap-3 pb-5 md:grid-cols-[1fr_180px_180px_auto]">
          <Input icon={<Search size={16} />} placeholder="Search users..." />
          <Select>
            <option>All roles</option>
            <option>Admin</option>
            <option>Manager</option>
            <option>Member</option>
          </Select>
          <Select>
            <option>All statuses</option>
            <option>Active</option>
            <option>Invited</option>
            <option>Suspended</option>
          </Select>
          <Button variant="outline">Filter</Button>
        </div>

        <Table
          columns={[
            { header: "Name", accessor: "name" },
            { header: "Email", accessor: "email" },
            { header: "Team", accessor: "team" },
            { header: "Role", render: (user) => <Badge tone="indigo">{user.role}</Badge> },
            {
              header: "Status",
              render: (user) => (
                <Badge tone={user.status === "Active" ? "success" : user.status === "Invited" ? "warning" : "danger"}>
                  {user.status}
                </Badge>
              ),
            },
            {
              header: "Actions",
              render: () => (
                <div className="flex gap-2">
                  <Button size="sm" variant="outline">Edit</Button>
                  <Button size="sm" variant="ghost">View</Button>
                </div>
              ),
            },
          ]}
          data={themeUsers}
        />
      </Card>

      <Card title="Empty state example" description="Useful when filters return no users or a module has not been configured.">
        <EmptyState
          title="No matching users"
          description="Clear filters or invite a teammate to populate this table."
          action={<Button size="sm" onClick={() => setOpen(true)}>Invite teammate</Button>}
        />
      </Card>

      <Modal open={open} title="Add user" onClose={() => setOpen(false)} footer={<Button onClick={() => setOpen(false)}>Send invite</Button>}>
        <div className="grid gap-4">
          <Input label="Full name" placeholder="Jane Cooper" />
          <Input label="Email" placeholder="jane@company.com" />
          <Select label="Role">
            <option>Member</option>
            <option>Manager</option>
            <option>Admin</option>
          </Select>
        </div>
      </Modal>
    </div>
  );
}

import { useState } from "react";
import { Plus } from "lucide-react";
import { Badge } from "../components/ui/Badge";
import { Button } from "../components/ui/Button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "../components/ui/Card";
import { Input } from "../components/ui/Input";
import { Modal } from "../components/ui/Modal";
import { Select } from "../components/ui/Select";
import { Table, type TableColumn } from "../components/ui/Table";
import { users } from "../lib/data";

type UserRow = (typeof users)[number];

const columns: Array<TableColumn<UserRow>> = [
  { header: "Name", accessor: "name" },
  { header: "Email", accessor: "email" },
  { header: "Role", accessor: "role" },
  {
    header: "Status",
    accessor: "status",
    render: (row) => (
      <Badge variant={row.status === "Active" ? "success" : row.status === "Invited" ? "warning" : "danger"}>{row.status}</Badge>
    )
  }
];

export function Users() {
  const [modalOpen, setModalOpen] = useState(false);

  return (
    <div className="grid gap-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <Badge variant="info">Directory</Badge>
          <h1 className="mt-3 text-3xl font-bold text-foreground">Users</h1>
          <p className="mt-2 text-muted-foreground">Manage user access, invitations, and account state.</p>
        </div>
        <Button onClick={() => setModalOpen(true)}>
          <Plus className="size-4" />
          Invite user
        </Button>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>User directory</CardTitle>
          <CardDescription>Sample user records for the first admin module.</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="mb-4 grid gap-3 md:grid-cols-[1fr_14rem]">
            <Input aria-label="Search users" placeholder="Search by name or email" />
            <Select
              aria-label="Filter users"
              options={[
                { label: "All statuses", value: "all" },
                { label: "Active", value: "active" },
                { label: "Invited", value: "invited" },
                { label: "Suspended", value: "suspended" }
              ]}
            />
          </div>
          <Table columns={columns} rows={users} />
        </CardContent>
      </Card>

      <Modal
        open={modalOpen}
        onClose={() => setModalOpen(false)}
        title="Invite user"
        description="This is a sample modal for the future user invitation workflow."
        footer={
          <>
            <Button variant="outline" onClick={() => setModalOpen(false)}>
              Cancel
            </Button>
            <Button onClick={() => setModalOpen(false)}>Send invite</Button>
          </>
        }
      >
        <div className="grid gap-4">
          <Input label="Full name" placeholder="Jane Cooper" />
          <Input label="Email" placeholder="jane@company.test" type="email" />
          <Select
            label="Role"
            options={[
              { label: "Admin", value: "admin" },
              { label: "Manager", value: "manager" },
              { label: "Viewer", value: "viewer" }
            ]}
          />
        </div>
      </Modal>
    </div>
  );
}

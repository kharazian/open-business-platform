import { ClipboardList, Save } from "lucide-react";
import { themeFormFields } from "../mockData";
import { Badge } from "../../components/ui/Badge";
import { Button } from "../../components/ui/Button";
import { Card } from "../../components/ui/Card";
import { Checkbox } from "../../components/ui/Checkbox";
import { Input } from "../../components/ui/Input";
import { PageHeader } from "../../components/ui/PageHeader";
import { Radio } from "../../components/ui/Radio";
import { Select } from "../../components/ui/Select";
import { Switch } from "../../components/ui/Switch";
import { Table } from "../../components/ui/Table";
import { Textarea } from "../../components/ui/Textarea";
import { ThemeHeaderAction } from "../components/ThemeHeaderAction";

export function ThemeForms() {
  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Foundation"
        title="Forms"
        description="Reusable form layouts for settings, access requests, filters, and workspace configuration screens."
        actions={<ThemeHeaderAction icon={Save}>Save draft</ThemeHeaderAction>}
      />

      <div className="grid gap-6 xl:grid-cols-[1.1fr_0.9fr]">
        <Card title="Workspace request" description="A balanced two-column form layout for common admin workflows.">
          <div className="grid gap-4 md:grid-cols-2">
            <Input label="Workspace name" placeholder="Northwind Operations" />
            <Input label="Support email" type="email" placeholder="support@company.test" />
            <Select label="Default module">
              <option>Dashboard</option>
              <option>Users</option>
              <option>Reports</option>
            </Select>
            <Select label="Audit retention">
              <option>90 days</option>
              <option>180 days</option>
              <option>365 days</option>
            </Select>
            <div className="md:col-span-2">
              <Textarea label="Request notes" placeholder="Describe the workspace policy or access need..." />
            </div>
          </div>
          <div className="mt-5 flex justify-end gap-2">
            <Button variant="outline">Cancel</Button>
            <Button>Submit request</Button>
          </div>
        </Card>

        <Card title="Control states" description="Compact controls for preferences and access policy forms.">
          <div className="grid gap-4">
            <Checkbox label="Require manager approval" description="Route access requests to the role owner." defaultChecked />
            <Switch label="Enable audit notifications" description="Send digest updates for important events." defaultChecked />
            <div className="grid gap-3 sm:grid-cols-2">
              <Radio label="Compact form" name="theme-form-density" />
              <Radio label="Comfortable form" name="theme-form-density" defaultChecked />
            </div>
            <Input label="Validation example" placeholder="admin@company.test" error="Use a company email address." />
          </div>
        </Card>
      </div>

      <Card title="Form field catalog">
        <Table
          columns={[
            { header: "Field", accessor: "name" },
            { header: "Type", render: (field) => <Badge tone="indigo">{field.type}</Badge> },
            { header: "Owner", accessor: "owner" },
            { header: "Required", render: (field) => <Badge tone={field.required ? "success" : "default"}>{field.required ? "Yes" : "No"}</Badge> }
          ]}
          data={themeFormFields}
        />
      </Card>

      <Card title="Form guidance">
        <div className="flex items-start gap-3">
          <span className="grid size-10 shrink-0 place-items-center rounded-xl bg-primary-soft text-primary">
            <ClipboardList size={20} />
          </span>
          <div>
            <p className="font-bold text-foreground">Keep business forms predictable</p>
            <p className="mt-1 text-sm leading-6 text-muted-foreground">
              Use simple sections, clear labels, inline validation, and consistent action placement across module workflows.
            </p>
          </div>
        </div>
      </Card>
    </div>
  );
}

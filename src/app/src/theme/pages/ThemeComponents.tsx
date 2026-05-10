import { useState } from "react";
import { componentRows } from "../mockData";
import { themePalettes } from "../../config/themePalettes";
import { cn } from "../../lib/cn";
import { useThemeAppearance } from "../../context/ThemeAppearanceContext";
import { Alert } from "../../components/ui/Alert";
import { Avatar } from "../../components/ui/Avatar";
import { Badge } from "../../components/ui/Badge";
import { Button } from "../../components/ui/Button";
import { Card } from "../../components/ui/Card";
import { Checkbox } from "../../components/ui/Checkbox";
import { DensitySwitcher } from "../../components/layout/DensitySwitcher";
import { Dropdown } from "../../components/ui/Dropdown";
import { EmptyState } from "../../components/ui/EmptyState";
import { Input } from "../../components/ui/Input";
import { Modal } from "../../components/ui/Modal";
import { ModeToggle } from "../../components/layout/ModeToggle";
import { PageHeader } from "../../components/ui/PageHeader";
import { PaletteSwitcher } from "../../components/layout/PaletteSwitcher";
import { Progress } from "../../components/ui/Progress";
import { Radio } from "../../components/ui/Radio";
import { Select } from "../../components/ui/Select";
import { Skeleton } from "../../components/ui/Skeleton";
import { StatCard } from "../../components/ui/StatCard";
import { Switch } from "../../components/ui/Switch";
import { Table } from "../../components/ui/Table";
import { Tabs } from "../../components/ui/Tabs";
import { Textarea } from "../../components/ui/Textarea";
import { Timeline } from "../../components/ui/Timeline";
import { BarChart3, ShieldCheck, Users } from "lucide-react";

export function ThemeComponents() {
  const [modalOpen, setModalOpen] = useState(false);
  const [tab, setTab] = useState("Overview");
  const { paletteId } = useThemeAppearance();

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Design system"
        title="Components"
        description="A mini component showcase for the reusable Tailwind building blocks used by the theme."
      />

      <div className="grid gap-6 xl:grid-cols-2">
        <Card title="Palette ranges" description="Six explicit Tailwind palette ranges. Pick one to see buttons, navigation, badges, and highlights change.">
          <PaletteSwitcher />
          <div className="mt-5 grid gap-3 sm:grid-cols-2">
            {themePalettes.map((palette) => (
              <div className={cn("rounded-2xl border p-3", paletteId === palette.id ? palette.primaryBorder : "border-border")} key={palette.id}>
                <div className={cn("h-12 rounded-xl bg-gradient-to-r", palette.gradientFrom, palette.gradientTo)} />
                <p className="mt-3 text-sm font-bold text-foreground">{palette.name}</p>
                <p className="mt-1 text-xs text-muted-foreground">{palette.description}</p>
              </div>
            ))}
          </div>
        </Card>

        <Card title="Appearance controls" description="Density and color mode examples for the playground shell.">
          <div className="grid gap-4">
            <DensitySwitcher />
            <ModeToggle />
            <Alert title="Scoped appearance">
              Palette, density, layout, and mode are stored for the theme area and restored when you return.
            </Alert>
          </div>
        </Card>

        <Card title="Buttons and badges">
          <div className="flex flex-wrap gap-3">
            <Button>Primary</Button>
            <Button variant="secondary">Secondary</Button>
            <Button variant="outline">Outline</Button>
            <Button variant="ghost">Ghost</Button>
            <Button variant="danger">Danger</Button>
          </div>
          <div className="mt-5 flex flex-wrap gap-2">
            <Badge>Default</Badge>
            <Badge tone="success">Success</Badge>
            <Badge tone="warning">Warning</Badge>
            <Badge tone="danger">Danger</Badge>
            <Badge tone="indigo">Indigo</Badge>
          </div>
        </Card>

        <Card title="Forms">
          <div className="grid gap-4">
            <Input label="Project name" placeholder="Open Business Platform" help="Label, help text, focus, and disabled states." />
            <Input label="Email" placeholder="invalid email" error="Use a valid company email." />
            <Select label="Plan" help="Select inherits palette focus styles.">
              <option>Starter</option>
              <option>Team</option>
              <option>Enterprise</option>
            </Select>
            <Textarea label="Notes" placeholder="Describe the workspace..." />
            <Checkbox label="Checkbox option" description="Good for multi-select configuration." defaultChecked />
            <div className="grid gap-3 sm:grid-cols-2">
              <Radio label="Monthly" name="billing-cycle" defaultChecked />
              <Radio label="Annual" name="billing-cycle" />
            </div>
            <Switch label="Toggle switch" description="Use for binary settings." defaultChecked />
          </div>
        </Card>

        <Card title="Tabs and dropdown">
          <Tabs tabs={["Overview", "Usage", "Access"]} activeTab={tab} onChange={setTab} />
          <div className="mt-5 flex items-center justify-between rounded-xl bg-muted/45 p-4">
            <p className="text-sm text-muted-foreground">Current tab: {tab}</p>
            <Dropdown
              trigger={<span>Actions</span>}
              items={[
                { label: "Edit", onClick: () => undefined },
                { label: "Duplicate", onClick: () => undefined },
                { label: "Archive", onClick: () => undefined },
              ]}
            />
          </div>
        </Card>

        <Card title="Avatar and empty state">
          <div className="flex flex-wrap items-center gap-3">
            <Avatar name="Maya Chen" />
            <Avatar name="Omar Patel" />
            <Avatar name="Lina Brooks" />
          </div>
          <div className="mt-5">
            <EmptyState title="No workflows yet" description="Create your first workflow to see it here." action={<Button size="sm">Create workflow</Button>} />
          </div>
        </Card>

        <Card title="Stats, progress, and loading">
          <div className="grid gap-4">
            <div className="grid gap-4 sm:grid-cols-3">
              <StatCard label="Users" value="1.2k" hint="+12%" icon={Users} />
              <StatCard label="Reports" value="38" hint="Ready" icon={BarChart3} tone="info" />
              <StatCard label="Security" value="94%" hint="Healthy" icon={ShieldCheck} tone="success" />
            </div>
            <Progress label="Onboarding completion" value={72} />
            <div className="grid gap-3">
              <Skeleton className="h-4 w-2/3" />
              <Skeleton className="h-24" />
            </div>
          </div>
        </Card>

        <Card title="Timeline and toast preview">
          <Timeline
            items={[
              { title: "Role updated", description: "Admin permissions were reviewed.", time: "10 minutes ago" },
              { title: "Report exported", description: "Revenue overview was downloaded.", time: "1 hour ago" },
              { title: "User invited", description: "A new finance reviewer joined.", time: "Yesterday" }
            ]}
          />
          <div className="mt-5 rounded-2xl border border-border bg-card p-4 shadow-lifted">
            <p className="text-sm font-bold text-foreground">Toast preview</p>
            <p className="mt-1 text-sm text-muted-foreground">Workspace settings saved successfully.</p>
          </div>
        </Card>
      </div>

      <Card title="Table and modal">
        <div className="mb-4 flex justify-end">
          <Button variant="outline" onClick={() => setModalOpen(true)}>Open modal</Button>
        </div>
        <Table
          columns={[
            { header: "Component", accessor: "component" },
            { header: "Status", render: (row) => <Badge tone={row.status === "Stable" ? "success" : "warning"}>{row.status}</Badge> },
            { header: "Usage", accessor: "usage" },
          ]}
          data={componentRows}
        />
      </Card>

      <Modal
        open={modalOpen}
        title="Reusable modal"
        onClose={() => setModalOpen(false)}
        footer={<Button onClick={() => setModalOpen(false)}>Done</Button>}
      >
        <p className="text-sm text-muted-foreground">
          This modal uses the same border, shadow, typography, and action styling as the rest of the theme.
        </p>
      </Modal>
    </div>
  );
}

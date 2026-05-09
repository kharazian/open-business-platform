import { Mail, ShieldCheck } from "lucide-react";
import { themeActivities } from "../mockData";
import { Avatar } from "../../components/ui/Avatar";
import { Badge } from "../../components/ui/Badge";
import { Button } from "../../components/ui/Button";
import { Card } from "../../components/ui/Card";
import { Input } from "../../components/ui/Input";
import { PageHeader } from "../../components/ui/PageHeader";
import { Progress } from "../../components/ui/Progress";
import { Timeline } from "../../components/ui/Timeline";

export function ThemeProfile() {
  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Account"
        title="Profile"
        description="A profile workspace with account details, editable fields, and activity history."
      />

      <div className="grid gap-6 xl:grid-cols-[0.7fr_1.3fr]">
        <Card>
          <div className="flex flex-col items-center text-center">
            <Avatar name="Maya Chen" size="lg" />
            <h2 className="mt-4 text-xl font-bold text-slate-950 dark:text-white">Maya Chen</h2>
            <p className="text-sm text-slate-500 dark:text-slate-400">Operations administrator</p>
            <div className="mt-4 flex flex-wrap justify-center gap-2">
              <Badge tone="success">Active</Badge>
              <Badge tone="indigo">Admin</Badge>
            </div>
          </div>
        </Card>

        <Card title="Profile details">
          <div className="grid gap-4 md:grid-cols-2">
            <Input label="First name" defaultValue="Maya" />
            <Input label="Last name" defaultValue="Chen" />
            <Input label="Email" defaultValue="maya@northwind.io" icon={<Mail size={16} />} />
            <Input label="Title" defaultValue="Operations administrator" />
          </div>
          <div className="mt-5 flex justify-end">
            <Button>Update profile</Button>
          </div>
        </Card>
      </div>

      <div className="grid gap-6 lg:grid-cols-2">
        <Card title="Activity">
          <Timeline
            items={themeActivities.map((activity, index) => ({
              title: activity,
              description: "Workspace activity event from the audit stream.",
              time: `${index + 1}h ago`
            }))}
          />
        </Card>

        <Card title="Account details" description="Description-list styling for profile metadata.">
          <dl className="grid gap-4 text-sm">
            {[
              ["Workspace", "Northwind Operations"],
              ["Last sign in", "Today at 9:42 AM"],
              ["Member since", "January 2026"],
              ["Session policy", "MFA required"],
            ].map(([label, value]) => (
              <div key={label} className="flex justify-between gap-4 border-b border-slate-100 pb-3 dark:border-slate-800">
                <dt className="text-slate-500 dark:text-slate-400">{label}</dt>
                <dd className="font-medium text-slate-900 dark:text-white">{value}</dd>
              </div>
            ))}
          </dl>
        </Card>
      </div>

      <Card title="Security summary">
        <div className="grid gap-5 md:grid-cols-[1fr_220px] md:items-center">
          <div className="flex items-center gap-3">
            <span className="grid size-11 place-items-center rounded-xl bg-success-soft text-success">
              <ShieldCheck size={20} />
            </span>
            <div>
              <p className="font-bold text-foreground">Strong account posture</p>
              <p className="text-sm text-muted-foreground">MFA, recent password rotation, and verified recovery email are enabled.</p>
            </div>
          </div>
          <Progress label="Security score" value={92} />
        </div>
      </Card>
    </div>
  );
}

import { Bell, CheckCheck, MailOpen, Search, Send } from "lucide-react";
import { themeNotifications } from "../mockData";
import { Avatar } from "../../components/ui/Avatar";
import { Badge } from "../../components/ui/Badge";
import { Button } from "../../components/ui/Button";
import { Card } from "../../components/ui/Card";
import { Input } from "../../components/ui/Input";
import { PageHeader } from "../../components/ui/PageHeader";
import { Select } from "../../components/ui/Select";
import { StatCard } from "../../components/ui/StatCard";
import { Table } from "../../components/ui/Table";

function getPriorityTone(priority: string) {
  if (priority === "Critical") return "danger";
  if (priority === "High") return "warning";
  return "info";
}

function getStatusTone(status: string) {
  return status === "Unread" ? "success" : "default";
}

export function ThemeNotifications() {
  const unreadCount = themeNotifications.filter((notification) => notification.status === "Unread").length;
  const deliveryRules = [
    { label: "Security alerts", value: "Instant", tone: "danger" },
    { label: "Access requests", value: "Instant", tone: "warning" },
    { label: "Report updates", value: "Daily digest", tone: "info" },
    { label: "Product news", value: "Weekly", tone: "default" }
  ] as const;

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Communication"
        title="Notifications"
        description="Review workspace messages, alert priority, read states, and delivery channels."
        actions={
          <Button>
            <Send size={16} />
            Compose
          </Button>
        }
      />

      <div className="grid gap-4 md:grid-cols-3">
        <StatCard label="Unread" value={String(unreadCount)} hint="Needs attention" icon={Bell} tone="warning" />
        <StatCard label="Channels" value="4" hint="Workspace, reports, access, governance" icon={MailOpen} tone="info" />
        <StatCard label="Resolved" value="86%" hint="Last 7 days" icon={CheckCheck} tone="success" />
      </div>

      <div className="grid gap-6 xl:grid-cols-[minmax(0,1fr)_340px]">
        <Card title="Notification inbox" description="A table layout for searchable notifications and alert management.">
          <div className="grid gap-3 pb-5 md:grid-cols-[1fr_170px_150px_auto]">
            <Input icon={<Search size={16} />} placeholder="Search notifications..." />
            <Select>
              <option>All channels</option>
              <option>Workspace</option>
              <option>Reports</option>
              <option>Access</option>
              <option>Governance</option>
            </Select>
            <Select>
              <option>All states</option>
              <option>Unread</option>
              <option>Read</option>
            </Select>
            <Button variant="outline">Filter</Button>
          </div>

          <Table
            columns={[
              {
                header: "Message",
                render: (notification) => (
                  <div className="flex min-w-64 gap-3">
                    <Avatar name={notification.sender} size="sm" />
                    <div>
                      <p className="font-bold text-foreground">{notification.title}</p>
                      <p className="mt-1 max-w-md text-sm text-muted-foreground">{notification.summary}</p>
                    </div>
                  </div>
                )
              },
              { header: "Channel", accessor: "channel" },
              { header: "Time", accessor: "time" },
              { header: "Priority", render: (notification) => <Badge tone={getPriorityTone(notification.priority)}>{notification.priority}</Badge> },
              { header: "Status", render: (notification) => <Badge tone={getStatusTone(notification.status)}>{notification.status}</Badge> }
            ]}
            data={themeNotifications}
          />
        </Card>

        <Card title="Delivery rules" description="Compact settings preview for notification preferences.">
          <div className="space-y-3">
            {deliveryRules.map((rule) => (
              <div className="flex items-center justify-between gap-3 rounded-xl border border-border bg-muted/30 px-4 py-3" key={rule.label}>
                <div>
                  <p className="font-bold text-foreground">{rule.label}</p>
                  <p className="text-sm text-muted-foreground">Workspace default</p>
                </div>
                <Badge tone={rule.tone}>{rule.value}</Badge>
              </div>
            ))}
          </div>
        </Card>
      </div>
    </div>
  );
}

import { BadgeDollarSign, CreditCard, Download, ReceiptText, Search, WalletCards } from "lucide-react";
import { themeInvoices } from "../mockData";
import { Badge } from "../../components/ui/Badge";
import { Button } from "../../components/ui/Button";
import { Card } from "../../components/ui/Card";
import { Input } from "../../components/ui/Input";
import { PageHeader } from "../../components/ui/PageHeader";
import { Select } from "../../components/ui/Select";
import { StatCard } from "../../components/ui/StatCard";
import { Table } from "../../components/ui/Table";
import { cn } from "../../lib/cn";
import { useThemeAppearance } from "../../context/ThemeAppearanceContext";

function getInvoiceTone(status: string) {
  if (status === "Paid") return "success";
  if (status === "Open") return "info";
  if (status === "Overdue") return "danger";
  return "warning";
}

export function ThemeBilling() {
  const { palette } = useThemeAppearance();

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Finance"
        title="Billing"
        description="Manage plan details, payment methods, invoice status, and billing history."
        actions={
          <Button variant="outline">
            <Download size={16} />
            Export invoices
          </Button>
        }
      />

      <div className="grid gap-4 md:grid-cols-3">
        <StatCard label="Monthly revenue" value="$48.2k" hint="+8.2% from last month" icon={BadgeDollarSign} />
        <StatCard label="Open invoices" value="12" hint="$18.4k outstanding" icon={ReceiptText} tone="warning" />
        <StatCard label="Payment success" value="98.4%" hint="Last 30 days" icon={WalletCards} tone="success" />
      </div>

      <div className="grid gap-6 xl:grid-cols-[360px_minmax(0,1fr)]">
        <Card title="Current plan" description="Subscription summary for a workspace billing screen.">
          <div className={cn("rounded-xl p-5", palette.softBg)}>
            <div className="flex items-start justify-between gap-4">
              <div>
                <p className={cn("text-sm font-bold", palette.softText)}>Business plan</p>
                <p className="mt-2 text-3xl font-black text-foreground">$240</p>
                <p className="mt-1 text-sm text-muted-foreground">Per workspace / month</p>
              </div>
              <span className={cn("grid size-11 place-items-center rounded-xl", palette.primaryBg, "text-white")}>
                <CreditCard size={20} />
              </span>
            </div>
          </div>

          <div className="mt-5 space-y-3">
            {[
              ["Seats included", "50"],
              ["Audit retention", "365 days"],
              ["Next renewal", "June 1"],
              ["Payment method", "Visa ending 4242"]
            ].map(([label, value]) => (
              <div className="flex items-center justify-between gap-3 border-b border-border pb-3 last:border-0 last:pb-0" key={label}>
                <span className="text-sm text-muted-foreground">{label}</span>
                <span className="text-sm font-bold text-foreground">{value}</span>
              </div>
            ))}
          </div>
        </Card>

        <Card title="Invoice history" description="Responsive invoice table using the same shared table component.">
          <div className="grid gap-3 pb-5 md:grid-cols-[1fr_170px_170px_auto]">
            <Input icon={<Search size={16} />} placeholder="Search invoices..." />
            <Select>
              <option>All plans</option>
              <option>Starter</option>
              <option>Business</option>
              <option>Enterprise</option>
            </Select>
            <Select>
              <option>All statuses</option>
              <option>Paid</option>
              <option>Open</option>
              <option>Overdue</option>
            </Select>
            <Button variant="outline">Filter</Button>
          </div>

          <Table
            columns={[
              { header: "Invoice", accessor: "number" },
              { header: "Customer", accessor: "customer" },
              { header: "Plan", accessor: "plan" },
              { header: "Amount", accessor: "amount" },
              { header: "Issued", accessor: "issued" },
              { header: "Status", render: (invoice) => <Badge tone={getInvoiceTone(invoice.status)}>{invoice.status}</Badge> }
            ]}
            data={themeInvoices}
          />
        </Card>
      </div>
    </div>
  );
}

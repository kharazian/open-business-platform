import { Calendar, Download, FileText, Send } from "lucide-react";
import { themeReports } from "../mockData";
import { Badge } from "../../components/ui/Badge";
import { Button } from "../../components/ui/Button";
import { Card } from "../../components/ui/Card";
import { Input } from "../../components/ui/Input";
import { PageHeader } from "../../components/ui/PageHeader";
import { Select } from "../../components/ui/Select";
import { Table } from "../../components/ui/Table";
import { ThemeHeaderAction } from "../components/ThemeHeaderAction";
import { useThemeAppearance } from "../../context/ThemeAppearanceContext";
import { cn } from "../../lib/cn";

export function ThemeReports() {
  const { palette } = useThemeAppearance();
  const exportSummary = [
    ["Ready reports", "18", "success"],
    ["Exports", "642", "info"],
    ["Scheduled", "9", "warning"]
  ] as const;

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Analytics"
        title="Reports"
        description="A report workspace with filters, export controls, cards, and responsive table content."
        actions={
          <>
            <ThemeHeaderAction icon={Download} variant="outline">CSV</ThemeHeaderAction>
            <ThemeHeaderAction icon={Download}>PDF</ThemeHeaderAction>
          </>
        }
      />

      <div className="grid gap-6 xl:grid-cols-[minmax(0,1fr)_340px]">
        <Card title="Review queue" description="Prioritized report cards for analytics teams.">
          <div className="grid gap-3">
            {themeReports.map((report, index) => (
              <div className="grid gap-4 rounded-xl border border-border bg-muted/25 p-4 md:grid-cols-[minmax(0,1fr)_160px_auto] md:items-center" key={report.name}>
                <div className="flex min-w-0 gap-3">
                  <span className={cn("grid size-11 shrink-0 place-items-center rounded-xl", index === 0 ? palette.primaryBg : palette.softBg, index === 0 ? "text-white" : palette.softText)}>
                    <FileText size={20} />
                  </span>
                  <div className="min-w-0">
                    <p className="font-bold text-foreground">{report.name}</p>
                    <p className="mt-1 text-sm text-muted-foreground">
                      {report.owner} / {report.type} / {report.updated}
                    </p>
                  </div>
                </div>
                <Badge tone={report.status === "Ready" ? "success" : "warning"}>{report.status}</Badge>
                <Button variant={index === 0 ? "primary" : "outline"}>
                  <Send size={16} />
                  Send
                </Button>
              </div>
            ))}
          </div>
        </Card>

        <Card title="Export summary" description="Operational totals for the current reporting period.">
          <div className="space-y-4">
            {exportSummary.map(([label, value, tone]) => (
              <div className="rounded-xl border border-border bg-muted/25 p-4" key={label}>
                <div className="flex items-center justify-between gap-3">
                  <p className="text-sm font-bold text-muted-foreground">{label}</p>
                  <Badge tone={tone}>{value}</Badge>
                </div>
                <div className="mt-4 h-2 overflow-hidden rounded-full bg-muted">
                  <div className={cn("h-full rounded-full", palette.primaryBg)} style={{ width: label === "Exports" ? "78%" : label === "Ready reports" ? "64%" : "42%" }} />
                </div>
              </div>
            ))}
          </div>
        </Card>
      </div>

      <Card title="Report library">
        <div className="grid gap-3 pb-5 md:grid-cols-[1fr_180px_180px_auto]">
          <Input icon={<Calendar size={16} />} placeholder="May 1 - May 31" />
          <Select>
            <option>All owners</option>
            <option>Finance</option>
            <option>Security</option>
          </Select>
          <Select>
            <option>All statuses</option>
            <option>Ready</option>
            <option>Draft</option>
          </Select>
          <Button variant="outline">Apply</Button>
        </div>

        <Table
          columns={[
            { header: "Report", accessor: "name" },
            { header: "Owner", accessor: "owner" },
            { header: "Type", accessor: "type" },
            { header: "Updated", accessor: "updated" },
            { header: "Status", render: (report) => <Badge tone={report.status === "Ready" ? "success" : "warning"}>{report.status}</Badge> },
          ]}
          data={themeReports}
        />
      </Card>
    </div>
  );
}

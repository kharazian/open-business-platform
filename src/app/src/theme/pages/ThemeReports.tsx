import { Calendar, Download, FileText } from "lucide-react";
import { themeReports } from "../mockData";
import { Badge } from "../../components/ui/Badge";
import { Button } from "../../components/ui/Button";
import { Card } from "../../components/ui/Card";
import { Input } from "../../components/ui/Input";
import { PageHeader } from "../../components/ui/PageHeader";
import { Select } from "../../components/ui/Select";
import { StatCard } from "../../components/ui/StatCard";
import { Table } from "../../components/ui/Table";
import { useThemeAppearance } from "../../context/ThemeAppearanceContext";
import { cn } from "../../lib/cn";

export function ThemeReports() {
  const { palette } = useThemeAppearance();

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Analytics"
        title="Reports"
        description="A report workspace with filters, export controls, cards, and responsive table content."
        actions={
          <>
            <Button variant="outline"><Download size={16} /> CSV</Button>
            <Button><Download size={16} /> PDF</Button>
          </>
        }
      />

      <div className="grid gap-4 md:grid-cols-3">
        <StatCard label="Ready reports" value="18" hint="+4 this week" icon={FileText} />
        <StatCard label="Exports" value="642" hint="CSV and PDF" icon={Download} tone="success" />
        <StatCard label="Scheduled" value="9" hint="Next 7 days" icon={Calendar} tone="warning" />
      </div>

      <div className="grid gap-4 md:grid-cols-3">
        {themeReports.slice(0, 3).map((report) => (
          <Card key={report.name}>
            <div className="flex items-start justify-between gap-4">
              <div>
                <p className="text-sm font-semibold text-foreground">{report.name}</p>
                <p className="mt-1 text-sm text-muted-foreground">
                  {report.owner} / {report.type}
                </p>
              </div>
              <span className={cn("grid size-10 place-items-center rounded-xl", palette.softBg, palette.softText)}>
                <FileText size={20} />
              </span>
            </div>
          </Card>
        ))}
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

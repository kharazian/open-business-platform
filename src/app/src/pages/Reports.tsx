import { Download, FileText, TrendingUp } from "lucide-react";
import { Badge } from "../components/ui/Badge";
import { Button } from "../components/ui/Button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "../components/ui/Card";
import { PageHeader } from "../components/ui/PageHeader";
import { Table, type TableColumn } from "../components/ui/Table";
import { reportRows } from "../lib/data";

type ReportRow = (typeof reportRows)[number];

const columns: Array<TableColumn<ReportRow>> = [
  { header: "Report", accessor: "name" },
  { header: "Owner", accessor: "owner" },
  { header: "Frequency", accessor: "frequency" },
  {
    header: "Status",
    accessor: "status",
    render: (row) => <Badge variant={row.status === "Ready" ? "success" : "warning"}>{row.status}</Badge>
  }
];

export function Reports() {
  return (
    <div className="grid gap-6">
      <PageHeader
        eyebrow="Analytics"
        title="Reports"
        description="Track scheduled reporting and exports across modules."
        actions={
          <Button variant="outline">
            <Download className="size-4" />
            Export
          </Button>
        }
      />

      <section className="grid gap-4 md:grid-cols-3">
        {[
          { label: "Reports ready", value: "18", icon: FileText },
          { label: "Monthly exports", value: "42", icon: Download },
          { label: "Trend health", value: "Stable", icon: TrendingUp }
        ].map((item) => {
          const Icon = item.icon;

          return (
            <Card className="p-5" key={item.label}>
              <span className="grid size-10 place-items-center rounded-lg bg-primary-soft text-primary">
                <Icon className="size-5" />
              </span>
              <p className="mt-4 text-sm font-bold text-muted-foreground">{item.label}</p>
              <p className="mt-2 text-3xl font-bold text-foreground">{item.value}</p>
            </Card>
          );
        })}
      </section>

      <Card>
        <CardHeader>
          <CardTitle>Report library</CardTitle>
          <CardDescription>Sample reporting catalog for the dashboard shell.</CardDescription>
        </CardHeader>
        <CardContent>
          <Table columns={columns} rows={reportRows} />
        </CardContent>
      </Card>
    </div>
  );
}

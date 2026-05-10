import { Download, FileText, FolderOpen, HardDrive, Search, Share2, Upload } from "lucide-react";
import { themeDocuments } from "../mockData";
import { Badge } from "../../components/ui/Badge";
import { Button } from "../../components/ui/Button";
import { Card } from "../../components/ui/Card";
import { Input } from "../../components/ui/Input";
import { PageHeader } from "../../components/ui/PageHeader";
import { Progress } from "../../components/ui/Progress";
import { Select } from "../../components/ui/Select";
import { StatCard } from "../../components/ui/StatCard";
import { Table } from "../../components/ui/Table";
import { cn } from "../../lib/cn";
import { useThemeAppearance } from "../../context/ThemeAppearanceContext";

const folders = [
  { name: "Governance", files: 42, size: "18.4 MB" },
  { name: "People", files: 28, size: "9.8 MB" },
  { name: "Audit Logs", files: 86, size: "124 MB" },
  { name: "Finance", files: 19, size: "31.2 MB" }
];

function getDocumentTone(status: string) {
  if (status === "Shared") return "success";
  if (status === "Archived") return "warning";
  return "default";
}

export function ThemeDocuments() {
  const { palette } = useThemeAppearance();

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Files"
        title="Documents"
        description="Manage workspace folders, file sharing states, storage usage, and recent documents."
        actions={
          <>
            <Button variant="outline">
              <Download size={16} />
              Export
            </Button>
            <Button>
              <Upload size={16} />
              Upload
            </Button>
          </>
        }
      />

      <div className="grid gap-4 md:grid-cols-3">
        <StatCard label="Documents" value="175" hint="Across 4 folders" icon={FileText} />
        <StatCard label="Shared files" value="64" hint="Visible to teams" icon={Share2} tone="success" />
        <StatCard label="Storage used" value="73%" hint="183 GB of 250 GB" icon={HardDrive} tone="warning" />
      </div>

      <div className="grid gap-6 xl:grid-cols-[minmax(0,1fr)_340px]">
        <Card title="Folders" description="Reusable folder cards for a file manager style page.">
          <div className="grid gap-4 md:grid-cols-2">
            {folders.map((folder) => (
              <div className="rounded-xl border border-border bg-muted/20 p-4" key={folder.name}>
                <div className="flex items-start justify-between gap-4">
                  <span className={cn("grid size-11 place-items-center rounded-xl", palette.softBg, palette.softText)}>
                    <FolderOpen size={20} />
                  </span>
                  <Badge tone="info">{folder.files} files</Badge>
                </div>
                <h2 className="mt-4 text-base font-black text-foreground">{folder.name}</h2>
                <p className="mt-1 text-sm text-muted-foreground">{folder.size}</p>
              </div>
            ))}
          </div>
        </Card>

        <Card title="Storage" description="Workspace storage allocation preview.">
          <div className="space-y-5">
            <Progress value={73} label="Total usage" />
            <div className="space-y-3">
              {[
                ["Documents", "82 GB"],
                ["Exports", "64 GB"],
                ["Images", "24 GB"],
                ["Archives", "13 GB"]
              ].map(([label, value]) => (
                <div className="flex items-center justify-between gap-3 border-b border-border pb-3 last:border-0 last:pb-0" key={label}>
                  <span className="text-sm text-muted-foreground">{label}</span>
                  <span className="text-sm font-bold text-foreground">{value}</span>
                </div>
              ))}
            </div>
          </div>
        </Card>
      </div>

      <Card title="Recent documents">
        <div className="grid gap-3 pb-5 md:grid-cols-[1fr_170px_170px_auto]">
          <Input icon={<Search size={16} />} placeholder="Search documents..." />
          <Select>
            <option>All folders</option>
            <option>Governance</option>
            <option>People</option>
            <option>Audit Logs</option>
            <option>Finance</option>
          </Select>
          <Select>
            <option>All types</option>
            <option>PDF</option>
            <option>DOC</option>
            <option>CSV</option>
            <option>XLS</option>
          </Select>
          <Button variant="outline">Filter</Button>
        </div>

        <Table
          columns={[
            { header: "Name", accessor: "name" },
            { header: "Owner", accessor: "owner" },
            { header: "Folder", accessor: "folder" },
            { header: "Type", accessor: "type" },
            { header: "Size", accessor: "size" },
            { header: "Updated", accessor: "updated" },
            { header: "Status", render: (document) => <Badge tone={getDocumentTone(document.status)}>{document.status}</Badge> }
          ]}
          data={themeDocuments}
        />
      </Card>
    </div>
  );
}

import { useMemo, useState } from "react";
import { Download, FileSearch, RefreshCw, ShieldAlert } from "lucide-react";
import { Alert } from "../../components/ui/Alert";
import { Badge } from "../../components/ui/Badge";
import { Button } from "../../components/ui/Button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "../../components/ui/Card";
import { EmptyState } from "../../components/ui/EmptyState";
import { Select } from "../../components/ui/Select";
import { Table } from "../../components/ui/Table";
import { analyzeCreatorExport } from "./api";
import type { CreatorAnalysisConstruct, CreatorAnalysisReport, CreatorAnalysisStatus } from "./types";

const statuses: CreatorAnalysisStatus[] = ["supported", "manual_review", "unsupported", "unsafe", "unknown"];

export function CreatorAnalysisPanel() {
  const [file, setFile] = useState<File | null>(null);
  const [inputKey, setInputKey] = useState(0);
  const [report, setReport] = useState<CreatorAnalysisReport | null>(null);
  const [status, setStatus] = useState<CreatorAnalysisStatus | "">("");
  const [type, setType] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const constructTypes = useMemo(() => [...new Set(report?.constructs.map((item) => item.type) ?? [])].sort(), [report]);
  const filtered = useMemo(() => (report?.constructs ?? []).filter((item) => (!status || item.status === status) && (!type || item.type === type)), [report, status, type]);

  async function analyze() {
    if (!file) { setError("Choose a .ds or .txt UTF-8 text export first."); return; }
    if (!/\.(ds|txt)$/i.test(file.name)) { resetFile(); setError("Choose a .ds or .txt UTF-8 text export."); return; }
    if (file.size > 1024 * 1024) { resetFile(); setError("Creator analysis source must not exceed 1 MiB."); return; }
    const selected = file;
    resetFile();
    setLoading(true); setError(null); setReport(null); setStatus(""); setType("");
    try { setReport(await analyzeCreatorExport(selected)); }
    catch (caught) { setError(caught instanceof Error ? caught.message : "Creator export analysis failed."); }
    finally { setLoading(false); }
  }

  function resetFile() { setFile(null); setInputKey((current) => current + 1); }
  function reset() { resetFile(); setReport(null); setError(null); setStatus(""); setType(""); }

  function download() {
    if (!report) return;
    const blob = new Blob([JSON.stringify(report, null, 2)], { type: "application/json" });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement("a");
    anchor.href = url; anchor.download = "creator-analysis-report.json"; anchor.click();
    URL.revokeObjectURL(url);
  }

  return <div className="space-y-4">
    <Alert title="Analysis only — no import is performed">
      The source is inspected in memory for this request, is never executed or retained, and cannot create or change platform data. Reports always return canImport: false.
    </Alert>
    <Card>
      <CardHeader><CardTitle>Analyze Creator export</CardTitle><CardDescription>Select one UTF-8 .ds or .txt source up to 1 MiB. ZIP files, URLs, record imports, scripts, and connections are not accepted.</CardDescription></CardHeader>
      <CardContent className="space-y-4">
        {error ? <Alert title="Analysis unavailable">{error}</Alert> : null}
        <label className="block">
          <span className="mb-2 block text-sm font-bold text-foreground">Creator source</span>
          <input key={inputKey} type="file" accept=".ds,.txt,text/plain" disabled={loading} onChange={(event) => setFile(event.target.files?.[0] ?? null)} className="block w-full rounded-xl border border-border bg-card/90 px-3 py-2 text-sm text-foreground file:mr-3 file:rounded-lg file:border-0 file:bg-primary/10 file:px-3 file:py-1.5 file:font-bold file:text-primary" />
        </label>
        <div className="flex flex-wrap gap-2">
          <Button disabled={loading || !file} onClick={() => void analyze()}><FileSearch className="size-4" />{loading ? "Analyzing..." : "Analyze safely"}</Button>
          <Button variant="outline" disabled={loading && !report} onClick={reset}><RefreshCw className="size-4" />Reset</Button>
          {report ? <Button variant="secondary" onClick={download}><Download className="size-4" />Download sanitized JSON</Button> : null}
        </div>
      </CardContent>
    </Card>
    {loading ? <Card><CardContent className="p-6 text-sm font-semibold text-muted-foreground">Analyzing the bounded source without executing it...</CardContent></Card> : null}
    {report ? <>
      {!report.complete || report.truncated ? <Alert title="Partial compatibility report">The analyzer reached malformed or bounded input. Review the reported counts and unresolved findings.</Alert> : null}
      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-5">
        {statuses.map((item) => <Metric key={item} label={label(item)} value={report.summary.byStatus[item] ?? 0} />)}
      </div>
      <Card>
        <CardHeader><CardTitle>Migration readiness</CardTitle><CardDescription>{report.analyzerVersion} inspected {report.source.byteCount.toLocaleString()} bytes across {report.source.lineCount.toLocaleString()} lines. Import remains disabled.</CardDescription></CardHeader>
        <CardContent className="space-y-4">
          <div className="grid gap-3 md:grid-cols-2">
            <Select label="Status" value={status} onChange={(event) => setStatus(event.target.value as CreatorAnalysisStatus | "")}><option value="">All statuses</option>{statuses.map((item) => <option value={item} key={item}>{label(item)}</option>)}</Select>
            <Select label="Construct type" value={type} onChange={(event) => setType(event.target.value)}><option value="">All types</option>{constructTypes.map((item) => <option value={item} key={item}>{item}</option>)}</Select>
          </div>
          {filtered.length === 0 ? <EmptyState title="No matching constructs" description="Change the filters or analyze another source." /> : <Table data={filtered} columns={constructColumns} />}
        </CardContent>
      </Card>
      <Card>
        <CardHeader><CardTitle>Credential signals</CardTitle><CardDescription>Only safe categories and counts are returned. Matched values and source lines are suppressed.</CardDescription></CardHeader>
        <CardContent>{report.credentialSignals.length === 0 ? <p className="text-sm text-muted-foreground">No credential signals were detected.</p> : <div className="flex flex-wrap gap-2">{report.credentialSignals.map((item) => <Badge key={item.category} variant="danger"><ShieldAlert className="size-3" />{item.category}: {item.count}</Badge>)}</div>}</CardContent>
      </Card>
      <Card>
        <CardHeader><CardTitle>Findings</CardTitle><CardDescription>{report.summary.findingCount.toLocaleString()} platform-authored compatibility findings. No source snippets are shown.</CardDescription></CardHeader>
        <CardContent>{report.findings.length === 0 ? <EmptyState title="No findings" description="The recognized constructs are direct compatibility candidates." /> : <Table data={report.findings} columns={[
          { header: "Severity", render: (finding) => <Badge variant={finding.severity === "error" ? "danger" : finding.severity === "warning" ? "warning" : "default"}>{finding.severity}</Badge> },
          { header: "Reason", render: (finding) => finding.reasonCode },
          { header: "Guidance", render: (finding) => finding.message }
        ]} />}</CardContent>
      </Card>
    </> : !loading ? <EmptyState title="No compatibility report" description="Choose a bounded local text export to generate a sanitized, analysis-only report." /> : null}
  </div>;
}

const constructColumns = [
  { header: "Construct", render: (item: CreatorAnalysisConstruct) => <div><p className="font-bold">{item.displayName}</p><p className="text-xs text-muted-foreground">{item.type} · line {item.lineStart}</p></div> },
  { header: "Status", render: (item: CreatorAnalysisConstruct) => <Badge variant={item.status === "unsafe" ? "danger" : item.status === "manual_review" || item.status === "unsupported" ? "warning" : "default"}>{label(item.status)}</Badge> },
  { header: "Candidate", render: (item: CreatorAnalysisConstruct) => item.proposedType ? `${item.proposedModule ?? "platform"} / ${item.proposedType}` : "Manual review" }
];

function Metric({ label: metricLabel, value }: { label: string; value: number }) {
  return <Card><CardContent className="p-4"><p className="text-xs font-semibold text-muted-foreground">{metricLabel}</p><p className="mt-1 text-2xl font-black">{value.toLocaleString()}</p></CardContent></Card>;
}
function label(value: string) { return value.replaceAll("_", " ").replace(/\b\w/g, (character) => character.toUpperCase()); }

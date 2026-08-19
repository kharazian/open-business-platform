import { useEffect, useState } from "react";
import { Download, Play, Plus, RefreshCw, RotateCcw, Trash2 } from "lucide-react";
import { Alert } from "../../../components/ui/Alert";
import { Badge } from "../../../components/ui/Badge";
import { Button } from "../../../components/ui/Button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "../../../components/ui/Card";
import { Checkbox } from "../../../components/ui/Checkbox";
import { EmptyState } from "../../../components/ui/EmptyState";
import { Input } from "../../../components/ui/Input";
import { Select } from "../../../components/ui/Select";
import { Table } from "../../../components/ui/Table";
import { Textarea } from "../../../components/ui/Textarea";
import {
  createProcessingJob, deleteProcessingJob, downloadExternalExportArtifact, getProcessingJob, listProcessingJobRuns,
  listProcessingJobs, queueProcessingJob, retryProcessingJobRun, setProcessingJobEnabled, updateProcessingJob
} from "../api";
import type {
  ProcessingJobDetailDto, ProcessingJobKind, ProcessingJobRunDto, ProcessingScheduleKind, ProcessingJobSummaryDto
} from "../types";

const initial = {
  name: "", kind: "record_export" as ProcessingJobKind, formId: "", reportId: "", integrationKey: "",
  sourceType: "form_records" as "form_records" | "list_report", format: "csv" as "csv" | "json", search: "",
  maxRows: 1000, scheduled: false, scheduleKind: "daily" as ProcessingScheduleKind,
  startAt: new Date(Date.now() + 3_600_000).toISOString().slice(0, 16), interval: 1,
  retryEnabled: false, maxAttempts: 3, delaySeconds: 300,
  mappingJson: JSON.stringify({ fieldMappings: [{ csvHeader: "email", targetFieldId: "email" }] }, null, 2)
};

export function ProcessingJobsPanel() {
  const [jobs, setJobs] = useState<ProcessingJobSummaryDto[]>([]);
  const [selected, setSelected] = useState<ProcessingJobSummaryDto | null>(null);
  const [runs, setRuns] = useState<ProcessingJobRunDto[]>([]);
  const [form, setForm] = useState(initial);
  const [editing, setEditing] = useState<ProcessingJobDetailDto | null>(null);
  const [csv, setCsv] = useState("");
  const [fileName, setFileName] = useState("records.csv");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  useEffect(() => { void loadJobs(); }, []);

  async function loadJobs() {
    setError(null);
    try {
      const result = await listProcessingJobs();
      setJobs(result.items);
      const current = selected ? result.items.find((item) => item.id === selected.id) ?? null : result.items[0] ?? null;
      setSelected(current);
      if (current) await loadRuns(current.id);
      else setRuns([]);
    } catch (caught) { setError(message(caught)); }
  }

  async function loadRuns(id: string) {
    try { setRuns((await listProcessingJobRuns(id)).items); }
    catch (caught) { setError(message(caught)); }
  }

  async function create() {
    setBusy(true); setError(null); setNotice(null);
    try {
      const isImport = form.kind === "csv_record_import";
      const mapping = isImport ? JSON.parse(form.mappingJson) as { fieldMappings: Array<{ csvHeader: string; targetFieldId: string }> } : null;
      const schedule = !isImport && form.scheduled ? {
        kind: form.scheduleKind, timeZone: Intl.DateTimeFormat().resolvedOptions().timeZone || "UTC",
        startAt: new Date(form.startAt).toISOString(), interval: form.interval
      } : null;
      const request = {
        name: form.name, kind: form.kind,
        config: {
          formId: form.formId, integrationKey: form.integrationKey,
          sourceType: isImport ? null : form.sourceType, format: isImport ? null : form.format,
          reportId: !isImport && form.sourceType === "list_report" ? form.reportId : null,
          search: !isImport && form.search.trim() ? form.search : null, maxRows: form.maxRows, mapping
        },
        schedule, retryPolicy: { isEnabled: !isImport && form.retryEnabled, maxAttempts: isImport ? 1 : form.maxAttempts, delaySeconds: form.delaySeconds },
        isEnabled: Boolean(schedule)
      };
      const saved = editing
        ? await updateProcessingJob(editing.id, { name: request.name, config: request.config, schedule: request.schedule, retryPolicy: request.retryPolicy, concurrencyStamp: editing.concurrencyStamp })
        : await createProcessingJob(request);
      setForm(initial); setEditing(null); setNotice(editing ? "Processing job updated." : "Processing job created."); await loadJobs();
      setSelected(saved); await loadRuns(saved.id);
    } catch (caught) { setError(message(caught)); }
    finally { setBusy(false); }
  }

  async function toggle(job: ProcessingJobSummaryDto) {
    setBusy(true); setError(null);
    try { await setProcessingJobEnabled(job, !job.isEnabled); setNotice(job.isEnabled ? "Schedule disabled." : "Schedule enabled."); await loadJobs(); }
    catch (caught) { setError(message(caught)); } finally { setBusy(false); }
  }

  async function edit(job: ProcessingJobSummaryDto) {
    setBusy(true); setError(null);
    try {
      const detail = await getProcessingJob(job.id);
      setEditing(detail);
      setForm({
        ...initial, name: detail.name, kind: detail.kind, formId: detail.config.formId,
        reportId: detail.config.reportId ?? "", integrationKey: detail.config.integrationKey,
        sourceType: detail.config.sourceType ?? "form_records", format: detail.config.format ?? "csv",
        search: detail.config.search ?? "", maxRows: detail.config.maxRows ?? 1000,
        scheduled: Boolean(detail.schedule), scheduleKind: detail.schedule?.kind ?? "daily",
        startAt: detail.schedule ? new Date(detail.schedule.startAt).toISOString().slice(0, 16) : initial.startAt,
        interval: detail.schedule?.interval ?? 1, retryEnabled: detail.retryPolicy.isEnabled,
        maxAttempts: detail.retryPolicy.maxAttempts, delaySeconds: detail.retryPolicy.delaySeconds,
        mappingJson: detail.config.mapping ? JSON.stringify(detail.config.mapping, null, 2) : initial.mappingJson
      });
    } catch (caught) { setError(message(caught)); } finally { setBusy(false); }
  }

  async function remove(job: ProcessingJobSummaryDto) {
    if (!window.confirm(`Delete ${job.name}?`)) return;
    setBusy(true); setError(null);
    try { await deleteProcessingJob(job); setSelected(null); setNotice("Processing job deleted."); await loadJobs(); }
    catch (caught) { setError(message(caught)); } finally { setBusy(false); }
  }

  async function run(job: ProcessingJobSummaryDto) {
    setBusy(true); setError(null);
    try {
      await queueProcessingJob(job.id, job.kind === "csv_record_import" ? fileName : null, job.kind === "csv_record_import" ? csv : null);
      setCsv(""); setNotice("Run queued."); await loadRuns(job.id);
    } catch (caught) { setError(message(caught)); } finally { setBusy(false); }
  }

  async function retry(run: ProcessingJobRunDto) {
    if (!selected) return;
    setBusy(true); setError(null);
    try { await retryProcessingJobRun(selected.id, run.id); setNotice("Retry queued."); await loadRuns(selected.id); }
    catch (caught) { setError(message(caught)); } finally { setBusy(false); }
  }

  async function download(run: ProcessingJobRunDto) {
    if (!run.externalExportJobId) return;
    try {
      const blob = await downloadExternalExportArtifact(run.externalExportJobId);
      const url = URL.createObjectURL(blob); const anchor = document.createElement("a");
      anchor.href = url; anchor.download = `processing-export-${run.id}`; anchor.click(); URL.revokeObjectURL(url);
    } catch (caught) { setError(message(caught)); }
  }

  return <div className="space-y-4">
    {error ? <Alert title="Processing job operation failed">{error}</Alert> : null}
    {notice ? <Alert title="Processing jobs">{notice}</Alert> : null}
    <div className="grid gap-4 xl:grid-cols-[minmax(300px,430px)_1fr]">
      <Card><CardHeader><CardTitle>{editing ? "Edit processing job" : "Create processing job"}</CardTitle><CardDescription>Queue bounded imports or run protected exports manually or on a schedule.</CardDescription></CardHeader>
        <CardContent className="space-y-3">
          <Input label="Name" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
          <Select disabled={Boolean(editing)} label="Type" value={form.kind} onChange={(e) => setForm({ ...form, kind: e.target.value as ProcessingJobKind, scheduled: false })}>
            <option value="record_export">Record export</option><option value="csv_record_import">CSV record import</option>
          </Select>
          <Input label="Form ID" value={form.formId} onChange={(e) => setForm({ ...form, formId: e.target.value })} />
          <Input label="Integration key" value={form.integrationKey} onChange={(e) => setForm({ ...form, integrationKey: e.target.value })} />
          {form.kind === "csv_record_import" ? <Textarea label="Field mapping JSON" rows={6} value={form.mappingJson} onChange={(e) => setForm({ ...form, mappingJson: e.target.value })} /> : <>
            <Select label="Source" value={form.sourceType} onChange={(e) => setForm({ ...form, sourceType: e.target.value as "form_records" | "list_report" })}><option value="form_records">Form records</option><option value="list_report">List report</option></Select>
            {form.sourceType === "list_report" ? <Input label="Report ID" value={form.reportId} onChange={(e) => setForm({ ...form, reportId: e.target.value })} /> : null}
            <Select label="Format" value={form.format} onChange={(e) => setForm({ ...form, format: e.target.value as "csv" | "json" })}><option value="csv">CSV</option><option value="json">JSON</option></Select>
            <Input label="Search (optional)" value={form.search} onChange={(e) => setForm({ ...form, search: e.target.value })} />
            <Input label="Maximum rows" type="number" min={1} max={5000} value={form.maxRows} onChange={(e) => setForm({ ...form, maxRows: Number(e.target.value) })} />
            <Checkbox checked={form.scheduled} label="Enable schedule" onChange={(e) => setForm({ ...form, scheduled: e.target.checked })} />
            {form.scheduled ? <><Select label="Schedule" value={form.scheduleKind} onChange={(e) => setForm({ ...form, scheduleKind: e.target.value as ProcessingScheduleKind })}><option value="once">Once</option><option value="daily">Daily</option><option value="weekly">Weekly</option><option value="monthly">Monthly</option></Select><Input label="Starts" type="datetime-local" value={form.startAt} onChange={(e) => setForm({ ...form, startAt: e.target.value })} /><Input label="Interval" type="number" min={1} max={366} value={form.interval} onChange={(e) => setForm({ ...form, interval: Number(e.target.value) })} /></> : null}
            <Checkbox checked={form.retryEnabled} label="Retry failed exports" onChange={(e) => setForm({ ...form, retryEnabled: e.target.checked })} />
            {form.retryEnabled ? <><Input label="Maximum attempts" type="number" min={1} max={5} value={form.maxAttempts} onChange={(e) => setForm({ ...form, maxAttempts: Number(e.target.value) })} /><Input label="Retry delay (seconds)" type="number" min={30} max={86400} value={form.delaySeconds} onChange={(e) => setForm({ ...form, delaySeconds: Number(e.target.value) })} /></> : null}
          </>}
          <div className="flex gap-2"><Button disabled={busy || !form.name.trim() || !form.formId || !form.integrationKey.trim()} onClick={() => void create()}><Plus className="size-4" />{editing ? "Save changes" : "Create job"}</Button>{editing ? <Button variant="ghost" onClick={() => { setEditing(null); setForm(initial); }}>Cancel</Button> : null}</div>
        </CardContent></Card>
      <div className="space-y-3">
        <div className="flex justify-end"><Button variant="outline" onClick={() => void loadJobs()}><RefreshCw className="size-4" />Refresh</Button></div>
        {jobs.length === 0 ? <EmptyState title="No processing jobs" description="Create a bounded import or export job." /> : <Table data={jobs} columns={[
          { header: "Name", render: (job) => <button className="font-bold text-primary" onClick={() => { setSelected(job); void loadRuns(job.id); }}>{job.name}</button> },
          { header: "Type", accessor: "kind" }, { header: "Status", render: (job) => job.isEnabled ? <Badge variant="success">Scheduled</Badge> : <Badge>Manual</Badge> },
          { header: "Next run", render: (job) => job.nextRunAt ? new Date(job.nextRunAt).toLocaleString() : "-" },
          { header: "Actions", render: (job) => <div className="flex gap-2"><Button size="sm" variant="outline" disabled={busy} onClick={() => void edit(job)}>Edit</Button><Button size="sm" variant="outline" disabled={busy || job.kind === "csv_record_import"} onClick={() => void toggle(job)}>{job.isEnabled ? "Disable" : "Enable"}</Button><Button size="sm" variant="danger" disabled={busy} onClick={() => void remove(job)}><Trash2 className="size-4" /></Button></div> }
        ]} />}
      </div>
    </div>
    {selected ? <Card><CardHeader><CardTitle>{selected.name} runs</CardTitle><CardDescription>Raw CSV input is never shown and is cleared after processing.</CardDescription></CardHeader><CardContent className="space-y-4">
      {selected.kind === "csv_record_import" ? <div className="grid gap-3 md:grid-cols-2"><Input label="File name" value={fileName} onChange={(e) => setFileName(e.target.value)} /><Textarea label="CSV content (maximum 1 MB)" value={csv} onChange={(e) => setCsv(e.target.value)} rows={5} /></div> : null}
      <Button disabled={busy || (selected.kind === "csv_record_import" && !csv.trim())} onClick={() => void run(selected)}><Play className="size-4" />Queue manual run</Button>
      {runs.length === 0 ? <EmptyState title="No runs" description="Queue a manual run or wait for the schedule." /> : <Table data={runs} columns={[
        { header: "Created", render: (run) => new Date(run.createdAt).toLocaleString() }, { header: "Source", accessor: "source" },
        { header: "Status", render: (run) => <Badge variant={run.status === "succeeded" ? "success" : run.status === "failed" ? "danger" : "warning"}>{run.status}</Badge> },
        { header: "Attempt", render: (run) => `${run.attempt}/${run.maxAttempts}` }, { header: "Error", render: (run) => run.errorCode ? `${run.errorCode}: ${run.errorMessage ?? ""}` : "-" },
        { header: "Result", render: (run) => <div className="flex gap-2">{run.externalExportJobId && run.status === "succeeded" ? <Button size="sm" variant="outline" onClick={() => void download(run)}><Download className="size-4" />Artifact</Button> : null}{run.status === "failed" && selected.kind === "record_export" && run.attempt < run.maxAttempts ? <Button size="sm" variant="outline" onClick={() => void retry(run)}><RotateCcw className="size-4" />Retry</Button> : null}{run.recordImportJobId ? <span className="text-xs">Import {run.recordImportJobId}</span> : null}</div> }
      ]} />}
    </CardContent></Card> : null}
  </div>;
}

function message(error: unknown) { return error instanceof Error ? error.message : "The operation failed."; }

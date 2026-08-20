import { useEffect, useState } from "react";
import { RefreshCw } from "lucide-react";
import { Alert } from "../../../components/ui/Alert";
import { Badge } from "../../../components/ui/Badge";
import { Button } from "../../../components/ui/Button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "../../../components/ui/Card";
import { EmptyState } from "../../../components/ui/EmptyState";
import { Input } from "../../../components/ui/Input";
import { Select } from "../../../components/ui/Select";
import { Table } from "../../../components/ui/Table";
import { getProcessingOperationsSummary, listProcessingOperationalLogs } from "../api";
import type { ProcessingJobSummaryDto, ProcessingOperationalLogDto, ProcessingOperationalLogFilters, ProcessingOperationsSummaryDto } from "../types";

const emptyFilters: ProcessingOperationalLogFilters = {};

export function ProcessingOperationsPanel({ jobs }: { jobs: ProcessingJobSummaryDto[] }) {
  const [summary, setSummary] = useState<ProcessingOperationsSummaryDto | null>(null);
  const [logs, setLogs] = useState<ProcessingOperationalLogDto[]>([]);
  const [filters, setFilters] = useState<ProcessingOperationalLogFilters>(emptyFilters);
  const [page, setPage] = useState(1);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => { void load(1, emptyFilters); }, []);

  async function load(targetPage = page, targetFilters = filters) {
    setLoading(true); setError(null);
    try {
      const [nextSummary, result] = await Promise.all([
        getProcessingOperationsSummary(), listProcessingOperationalLogs(targetFilters, targetPage)
      ]);
      if (result.items.length === 0 && result.totalCount > 0 && targetPage > 1) { await load(targetPage - 1, targetFilters); return; }
      setSummary(nextSummary); setLogs(result.items); setPage(result.page); setTotal(result.totalCount);
    } catch (caught) { setError(caught instanceof Error ? caught.message : "Processing operations could not be loaded."); }
    finally { setLoading(false); }
  }

  return <div className="space-y-4">
    <Card><CardHeader><CardTitle>Processing health</CardTitle><CardDescription>Bounded workspace activity for the last 24 hours.</CardDescription></CardHeader>
      <CardContent className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        <Metric label="Pending / running" value={`${summary?.pending ?? 0} / ${summary?.running ?? 0}`} />
        <Metric label="Succeeded" value={summary?.succeeded ?? 0} />
        <Metric label="Failed" value={summary?.failed ?? 0} />
        <Metric label="Retries / exhausted" value={`${summary?.retryScheduled ?? 0} / ${summary?.retryExhausted ?? 0}`} />
        <Metric label="Schedule skips" value={summary?.scheduleSkipped ?? 0} />
        <Metric label="Imports / exports" value={`${summary?.byKind.csv_record_import ?? 0} / ${summary?.byKind.record_export ?? 0}`} />
      </CardContent>
    </Card>
    <Card><CardHeader><CardTitle>Operational logs</CardTitle><CardDescription>Payload-safe execution diagnostics, separate from audit and integration history.</CardDescription></CardHeader>
      <CardContent className="space-y-4">
        {error ? <Alert title="Operational logs unavailable">{error}</Alert> : null}
        <div className="grid gap-3 md:grid-cols-3 lg:grid-cols-6">
          <Select label="Job" value={filters.definitionId ?? ""} onChange={(e) => setFilters({ ...filters, definitionId: e.target.value })}><option value="">All jobs</option>{jobs.map((job) => <option key={job.id} value={job.id}>{job.name}</option>)}</Select>
          <Select label="Type" value={filters.kind ?? ""} onChange={(e) => setFilters({ ...filters, kind: e.target.value as ProcessingOperationalLogFilters["kind"] })}><option value="">All types</option><option value="record_export">Record export</option><option value="csv_record_import">CSV import</option></Select>
          <Select label="Severity" value={filters.severity ?? ""} onChange={(e) => setFilters({ ...filters, severity: e.target.value as ProcessingOperationalLogFilters["severity"] })}><option value="">All severities</option><option value="info">Info</option><option value="warning">Warning</option><option value="error">Error</option></Select>
          <Select label="Event" value={filters.eventCode ?? ""} onChange={(e) => setFilters({ ...filters, eventCode: e.target.value })}><option value="">All events</option><option value="run_queued">Run queued</option><option value="run_started">Run started</option><option value="run_succeeded">Run succeeded</option><option value="run_failed">Run failed</option><option value="retry_scheduled">Retry scheduled</option><option value="retry_exhausted">Retry exhausted</option><option value="import_recovery_unsafe">Import recovery unsafe</option><option value="schedule_skipped_active_run">Schedule skipped</option></Select>
          <Input label="Run ID" value={filters.runId ?? ""} onChange={(e) => setFilters({ ...filters, runId: e.target.value })} />
          <Input label="Error code" value={filters.errorCode ?? ""} onChange={(e) => setFilters({ ...filters, errorCode: e.target.value })} />
        </div>
        <div className="flex flex-wrap gap-2"><Button disabled={loading} onClick={() => void load(1, filters)}>Apply filters</Button><Button variant="outline" disabled={loading} onClick={() => { setFilters(emptyFilters); void load(1, emptyFilters); }}><RefreshCw className="size-4" />Reset</Button></div>
        {loading ? <p className="text-sm font-semibold text-muted-foreground">Loading operational logs...</p> : null}
        {!loading && logs.length === 0 ? <EmptyState title="No operational events" description="Queued and completed processing activity will appear here." /> : logs.length > 0 ? <Table data={logs} columns={[
          { header: "Time", render: (log) => new Date(log.occurredAt).toLocaleString() },
          { header: "Job", render: (log) => log.definitionName },
          { header: "Severity", render: (log) => <Badge variant={log.severity === "error" ? "danger" : log.severity === "warning" ? "warning" : "default"}>{log.severity}</Badge> },
          { header: "Event", render: (log) => <div><p className="font-semibold">{log.eventCode}</p><p className="text-xs text-muted-foreground">{log.message}</p></div> },
          { header: "Attempt", render: (log) => log.attempt ? `${log.attempt}/${log.maxAttempts ?? log.attempt}` : "-" },
          { header: "Error", render: (log) => log.errorCode ?? "-" }
        ]} /> : null}
        {total > 25 ? <div className="flex items-center justify-between"><Button size="sm" variant="outline" disabled={loading || page === 1} onClick={() => void load(page - 1)}>Previous</Button><span className="text-xs font-semibold text-muted-foreground">Page {page} of {Math.ceil(total / 25)}</span><Button size="sm" variant="outline" disabled={loading || page * 25 >= total} onClick={() => void load(page + 1)}>Next</Button></div> : null}
      </CardContent>
    </Card>
  </div>;
}

function Metric({ label, value }: { label: string; value: string | number }) {
  return <div className="rounded-xl border border-border bg-muted/30 p-3"><p className="text-xs font-semibold text-muted-foreground">{label}</p><p className="mt-1 text-2xl font-black text-foreground">{value}</p></div>;
}

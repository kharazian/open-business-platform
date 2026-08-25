import { useEffect, useMemo, useState } from "react";
import { ArchiveRestore, RefreshCw, Search, Trash2 } from "lucide-react";
import { Alert } from "../../../components/ui/Alert";
import { Badge } from "../../../components/ui/Badge";
import { Button } from "../../../components/ui/Button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "../../../components/ui/Card";
import { EmptyState } from "../../../components/ui/EmptyState";
import { Input } from "../../../components/ui/Input";
import { Modal } from "../../../components/ui/Modal";
import { DashboardApiError, listArchivedDashboards, permanentlyDeleteDashboard, restoreArchivedDashboard } from "../api";
import type { ArchivedDashboard, DashboardDetail } from "../types";

export function DashboardRecycleBinModal({ onClose, onRestored, open }: {
  onClose: () => void;
  onRestored: (dashboard: DashboardDetail) => void;
  open: boolean;
}) {
  const [items, setItems] = useState<ArchivedDashboard[]>([]);
  const [loading, setLoading] = useState(false);
  const [busyId, setBusyId] = useState("");
  const [confirmingId, setConfirmingId] = useState("");
  const [confirmationName, setConfirmationName] = useState("");
  const [query, setQuery] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const visibleItems = useMemo(() => {
    const normalized = query.trim().toLowerCase();
    return normalized ? items.filter((item) => `${item.name} ${item.description ?? ""} ${item.archivedByName ?? ""}`.toLowerCase().includes(normalized)) : items;
  }, [items, query]);

  useEffect(() => { if (open) void load(); }, [open]);

  async function load() {
    setLoading(true); setError(null);
    try { setItems(await listArchivedDashboards()); }
    catch (caught) { setError(getMessage(caught)); }
    finally { setLoading(false); }
  }

  async function restore(item: ArchivedDashboard) {
    setBusyId(item.id); setError(null); setNotice(null);
    try {
      const dashboard = await restoreArchivedDashboard(item.id, item.concurrencyStamp);
      setItems((current) => current.filter((candidate) => candidate.id !== item.id));
      setNotice(`“${item.name}” restored as a draft.`);
      onRestored(dashboard);
    } catch (caught) { setError(getMessage(caught)); }
    finally { setBusyId(""); }
  }

  async function removePermanently(item: ArchivedDashboard) {
    setBusyId(item.id); setError(null); setNotice(null);
    try {
      await permanentlyDeleteDashboard(item.id, item.concurrencyStamp, confirmationName);
      setItems((current) => current.filter((candidate) => candidate.id !== item.id));
      setConfirmingId(""); setConfirmationName("");
      setNotice(`“${item.name}” permanently deleted. Its dashboard revisions cannot be recovered.`);
    } catch (caught) { setError(getMessage(caught)); }
    finally { setBusyId(""); }
  }

  return <Modal description="Restore archived dashboards or permanently delete them after the workspace waiting period." onClose={onClose} open={open} panelClassName="max-h-[90vh] max-w-4xl overflow-y-auto" title="Dashboard recycle bin">
    <div className="grid gap-4">
      <div className="flex flex-wrap items-end gap-3"><div className="min-w-64 flex-1"><Input aria-label="Search archived dashboards" onChange={(event) => setQuery(event.target.value)} placeholder="Search archived dashboards…" value={query} /></div><Button disabled={loading} onClick={() => void load()} variant="outline"><RefreshCw className={`size-4 ${loading ? "animate-spin" : ""}`} />Refresh</Button></div>
      <p className="text-xs font-semibold text-muted-foreground"><Search className="mr-1 inline size-3" />{visibleItems.length} of {items.length} archived dashboards</p>
      {error ? <Alert title="Recycle bin">{error}</Alert> : null}
      {notice ? <div className="rounded-xl border border-success/40 bg-success/10 px-4 py-3 text-sm font-semibold text-success">{notice}</div> : null}
      {loading && items.length === 0 ? <p className="py-8 text-center text-sm font-semibold text-muted-foreground">Loading archived dashboards…</p> : visibleItems.length === 0 ? <EmptyState description={items.length ? "Try a different dashboard name or archive actor." : "Archived dashboards will appear here."} title={items.length ? "No archived dashboards match" : "Recycle bin is empty"} /> : <div className="grid gap-3">{visibleItems.map((item) => {
        const availableAt = new Date(item.permanentDeleteAvailableAt);
        const confirming = confirmingId === item.id;
        return <Card key={item.id}><CardHeader><div className="flex flex-wrap items-start justify-between gap-3"><div className="min-w-0"><CardTitle>{item.name}</CardTitle><CardDescription>{item.description ?? "Archived dashboard"}</CardDescription></div><Badge tone="warning">Archived</Badge></div></CardHeader><CardContent className="grid gap-3"><div className="flex flex-wrap gap-x-4 gap-y-1 text-xs font-semibold text-muted-foreground"><span>{item.widgetCount} widgets</span><span>Archived {new Date(item.archivedAt).toLocaleString()}</span><span>By {item.archivedByName ?? "system administrator"}</span></div><div className="flex flex-wrap gap-2"><Button disabled={busyId === item.id} onClick={() => void restore(item)} variant="outline"><ArchiveRestore className="size-4" />Restore draft</Button><Button disabled={busyId === item.id || !item.canDeletePermanently} onClick={() => { setConfirmingId(item.id); setConfirmationName(""); }} variant="danger"><Trash2 className="size-4" />Delete permanently</Button></div>{!item.canDeletePermanently ? <p className="text-xs font-semibold text-warning">Permanent deletion becomes available {availableAt.toLocaleString()}.</p> : null}{confirming ? <div className="grid gap-3 rounded-xl border border-danger/40 bg-danger/5 p-3"><Alert title="This cannot be undone">All saved revisions for this dashboard will also be deleted. The audit event remains.</Alert><Input label={`Type “${item.name}” to confirm`} onChange={(event) => setConfirmationName(event.target.value)} value={confirmationName} /><div className="flex flex-wrap justify-end gap-2"><Button onClick={() => { setConfirmingId(""); setConfirmationName(""); }} variant="outline">Cancel</Button><Button disabled={confirmationName !== item.name || busyId === item.id} onClick={() => void removePermanently(item)} variant="danger">Permanently delete</Button></div></div> : null}</CardContent></Card>;
      })}</div>}
    </div>
  </Modal>;
}

function getMessage(error: unknown): string {
  if (error instanceof DashboardApiError && error.errors.length) return `${error.message} ${error.errors.map((item) => item.message).join(" ")}`;
  return error instanceof Error ? error.message : "Recycle-bin request failed.";
}

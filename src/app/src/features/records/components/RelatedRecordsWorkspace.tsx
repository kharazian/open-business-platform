import { useEffect, useState } from "react";
import { ChevronLeft, ChevronRight, ExternalLink, Network, RefreshCw } from "lucide-react";
import { Link } from "react-router-dom";
import { Alert } from "../../../components/ui/Alert";
import { Badge } from "../../../components/ui/Badge";
import { Button } from "../../../components/ui/Button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "../../../components/ui/Card";
import { Skeleton } from "../../../components/ui/Skeleton";
import {
  listRelatedRecordPanels,
  listRelatedRecordRows,
  type PagedResult,
  type RelatedRecordPanel,
  type RelatedRecordRows
} from "../../forms/api";
import { getRelatedPageCount } from "../relatedRecords";

const panelPageSize = 10;
const rowPageSize = 10;

export function RelatedRecordsWorkspace({ recordId, refreshKey }: { recordId: string; refreshKey: number }) {
  const [panels, setPanels] = useState<PagedResult<RelatedRecordPanel> | null>(null);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [loadVersion, setLoadVersion] = useState(0);

  useEffect(() => {
    setPage(1);
    void loadPanels(1);
  }, [recordId, refreshKey]);

  async function loadPanels(nextPage: number) {
    setLoading(true);
    setError(null);
    try {
      setPanels(await listRelatedRecordPanels(recordId, nextPage, panelPageSize));
      setPage(nextPage);
      setLoadVersion((current) => current + 1);
    } catch (caught) {
      setError(getErrorMessage(caught));
    } finally {
      setLoading(false);
    }
  }

  const pageCount = getRelatedPageCount(panels?.totalCount ?? 0, panelPageSize);

  return (
    <section className="grid gap-4" data-print-hide="true" aria-label="Related records">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h2 className="flex items-center gap-2 text-lg font-bold text-foreground">
            <Network className="size-5" />
            Related records
          </h2>
          <p className="mt-1 text-sm leading-6 text-muted-foreground">Records that refer to this record through an accessible lookup field.</p>
        </div>
        <Button disabled={loading} onClick={() => void loadPanels(page)} size="sm" variant="outline">
          <RefreshCw className="size-4" />
          Refresh
        </Button>
      </div>

      {error ? <Alert title="Related records">{error}</Alert> : null}
      {loading && !panels ? (
        <div className="grid gap-4 lg:grid-cols-2">
          <Skeleton className="h-60" />
          <Skeleton className="h-60" />
        </div>
      ) : panels && panels.items.length > 0 ? (
        <>
          <div className="grid items-start gap-4 xl:grid-cols-2">
            {panels.items.map((panel) => (
              <RelatedRecordPanelCard key={`${panel.sourceFormId}:${panel.sourceFieldId}`} loadVersion={loadVersion} panel={panel} recordId={recordId} />
            ))}
          </div>
          {pageCount > 1 ? (
            <Pagination
              label={`Panel page ${page} of ${pageCount}`}
              nextDisabled={loading || page >= pageCount}
              onNext={() => void loadPanels(page + 1)}
              onPrevious={() => void loadPanels(page - 1)}
              previousDisabled={loading || page <= 1}
            />
          ) : null}
        </>
      ) : panels ? (
        <div className="rounded-xl border border-dashed border-border bg-muted/40 p-8 text-center">
          <Network className="mx-auto size-8 text-muted-foreground" />
          <h3 className="mt-3 font-bold text-foreground">No related records</h3>
          <p className="mt-1 text-sm text-muted-foreground">No accessible forms currently define a lookup to this record type.</p>
        </div>
      ) : null}
    </section>
  );
}

function RelatedRecordPanelCard({ loadVersion, panel, recordId }: { loadVersion: number; panel: RelatedRecordPanel; recordId: string }) {
  const [rows, setRows] = useState<RelatedRecordRows | null>(null);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setRows(null);
    setPage(1);
    void loadRows(1);
  }, [recordId, panel.sourceFormId, panel.sourceFieldId, loadVersion]);

  async function loadRows(nextPage: number) {
    setLoading(true);
    setError(null);
    try {
      setRows(await listRelatedRecordRows(recordId, panel.sourceFormId, panel.sourceFieldId, nextPage, rowPageSize));
      setPage(nextPage);
    } catch (caught) {
      setError(getErrorMessage(caught));
    } finally {
      setLoading(false);
    }
  }

  const effectivePanel = rows?.panel ?? panel;
  const pageCount = getRelatedPageCount(effectivePanel.totalCount, rows?.pageSize ?? rowPageSize);

  return (
    <Card className="overflow-hidden p-0">
      <CardHeader className="flex flex-row items-start justify-between gap-3">
        <div className="min-w-0">
          <CardTitle className="truncate">{panel.sourceFormName}</CardTitle>
          <CardDescription>
            {panel.sourceFieldLabel} · {effectivePanel.totalCount} {effectivePanel.totalCount === 1 ? "record" : "records"}
          </CardDescription>
        </div>
        <Button aria-label={`Refresh ${panel.sourceFormName}`} disabled={loading} onClick={() => void loadRows(page)} size="icon" variant="ghost">
          <RefreshCw className="size-4" />
        </Button>
      </CardHeader>
      <CardContent className="grid gap-3 p-0">
        {error ? <div className="p-4"><Alert title={panel.sourceFormName}>{error}</Alert></div> : null}
        {loading && !rows ? (
          <div className="p-4"><Skeleton className="h-32" /></div>
        ) : rows && rows.items.length > 0 ? (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[34rem] text-left text-sm">
              <thead className="border-b border-border bg-card-muted text-xs uppercase tracking-normal text-muted-foreground">
                <tr>
                  <th className="px-4 py-3 font-bold">Record</th>
                  {effectivePanel.columns.map((column) => <th className="px-4 py-3 font-bold" key={column.fieldId}>{column.label}</th>)}
                  <th className="px-4 py-3 font-bold">Status</th>
                  <th className="px-4 py-3 font-bold">Created</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                {rows.items.map((row) => (
                  <tr key={row.recordId}>
                    <td className="px-4 py-3">
                      <Link className="inline-flex items-center gap-1 font-bold text-primary hover:underline" to={`/records/${encodeURIComponent(row.recordId)}`}>
                        {shortId(row.recordId)} <ExternalLink className="size-3.5" />
                      </Link>
                    </td>
                    {effectivePanel.columns.map((column) => (
                      <td className="max-w-64 truncate px-4 py-3 text-foreground" key={column.fieldId} title={row.cells[column.fieldId] ?? ""}>
                        {row.cells[column.fieldId] || <span className="text-muted-foreground">—</span>}
                      </td>
                    ))}
                    <td className="px-4 py-3"><Badge variant={row.status === "active" ? "success" : "default"}>{row.status}</Badge></td>
                    <td className="whitespace-nowrap px-4 py-3 text-muted-foreground">{formatDate(row.createdAt)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : rows ? (
          <p className="px-5 py-8 text-center text-sm font-semibold text-muted-foreground">No accessible records refer to this record.</p>
        ) : null}
        {rows && pageCount > 1 ? (
          <div className="border-t border-border px-4 py-3">
            <Pagination
              label={`Record page ${page} of ${pageCount}`}
              nextDisabled={loading || page >= pageCount}
              onNext={() => void loadRows(page + 1)}
              onPrevious={() => void loadRows(page - 1)}
              previousDisabled={loading || page <= 1}
            />
          </div>
        ) : null}
      </CardContent>
    </Card>
  );
}

function Pagination({ label, nextDisabled, onNext, onPrevious, previousDisabled }: {
  label: string;
  nextDisabled: boolean;
  onNext: () => void;
  onPrevious: () => void;
  previousDisabled: boolean;
}) {
  return (
    <div className="flex items-center justify-between gap-3">
      <Button aria-label="Previous page" disabled={previousDisabled} onClick={onPrevious} size="sm" variant="outline"><ChevronLeft className="size-4" /> Previous</Button>
      <span className="text-xs font-bold text-muted-foreground">{label}</span>
      <Button aria-label="Next page" disabled={nextDisabled} onClick={onNext} size="sm" variant="outline">Next <ChevronRight className="size-4" /></Button>
    </div>
  );
}

function shortId(value: string): string {
  return value.length > 8 ? value.slice(0, 8) : value;
}

function formatDate(value: string): string {
  return new Intl.DateTimeFormat("en", { month: "short", day: "numeric", year: "numeric" }).format(new Date(value));
}

function getErrorMessage(error: unknown): string {
  return error instanceof Error ? error.message : "Related records request failed.";
}

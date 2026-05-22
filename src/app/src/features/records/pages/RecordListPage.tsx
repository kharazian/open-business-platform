import { type FormEvent, type ReactNode, useEffect, useMemo, useState } from "react";
import { ArrowLeft, Eye, FileText, Printer, RefreshCw, Search } from "lucide-react";
import { Link, useParams } from "react-router-dom";
import { Alert } from "../../../components/ui/Alert";
import { Badge } from "../../../components/ui/Badge";
import { Button } from "../../../components/ui/Button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "../../../components/ui/Card";
import { EmptyState } from "../../../components/ui/EmptyState";
import { Input } from "../../../components/ui/Input";
import { PageHeader } from "../../../components/ui/PageHeader";
import { Skeleton } from "../../../components/ui/Skeleton";
import { Table, type TableColumn } from "../../../components/ui/Table";
import { cn } from "../../../lib/cn";
import { listRecords, type FormRecordListItem } from "../../forms/api";
import type { FormRecordValue } from "../../forms/types";
import { getRecordListPrintDescription, requestBrowserPrint } from "../recordPrint";

const pageSize = 25;

const recordColumns: Array<TableColumn<FormRecordListItem>> = [
  {
    header: "Record",
    render: (record) => (
      <div className="min-w-0">
        <p className="font-bold text-foreground">{shortId(record.id)}</p>
        <p className="mt-1 text-xs text-muted-foreground">{record.id}</p>
      </div>
    )
  },
  {
    header: "Values",
    render: (record) => <ValuePreview values={record.values} />
  },
  {
    header: "Version",
    render: (record) => <span className="font-semibold">{shortId(record.formVersionId)}</span>
  },
  {
    header: "Status",
    render: (record) => <Badge variant={record.status === "active" ? "success" : "default"}>{record.status}</Badge>
  },
  {
    header: "Created",
    render: (record) => formatDateTime(record.createdAt)
  },
  {
    header: "Actions",
    render: (record) => (
      <div data-print-hide="true">
        <RecordDetailLink recordId={record.id} />
      </div>
    )
  }
];

export function RecordListPage() {
  const { formId } = useParams();
  const resolvedFormId = formId ?? "";
  const [records, setRecords] = useState<FormRecordListItem[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [query, setQuery] = useState("");
  const [submittedQuery, setSubmittedQuery] = useState("");
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const totalPages = useMemo(() => Math.max(1, Math.ceil(totalCount / pageSize)), [totalCount]);

  useEffect(() => {
    void refreshRecords(1, "");
  }, [resolvedFormId]);

  async function refreshRecords(targetPage = page, search = submittedQuery) {
    if (!resolvedFormId) {
      setError("Form id is required.");
      setLoading(false);
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const result = await listRecords(resolvedFormId, { page: targetPage, pageSize, search });
      setRecords(result.items);
      setTotalCount(result.totalCount);
      setPage(targetPage);
      setSubmittedQuery(search);
    } catch (caught) {
      setError(getErrorMessage(caught));
    } finally {
      setLoading(false);
    }
  }

  function handleSearch(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    void refreshRecords(1, query);
  }

  function clearSearch() {
    setQuery("");
    void refreshRecords(1, "");
  }

  return (
    <div className="grid gap-6 print-area">
      <PrintHeader
        description={getRecordListPrintDescription(totalCount, page, totalPages, submittedQuery)}
        title="Submitted records"
      />
      <div data-print-hide="true">
        <PageHeader
          eyebrow="Records"
          title="Submitted records"
          description="Browse submitted records for this form."
          actions={
            <div className="flex flex-wrap gap-2">
              <Button disabled={loading || records.length === 0} onClick={() => requestBrowserPrint()} variant="outline">
                <Printer className="size-4" />
                Print
              </Button>
              <Button disabled={loading} onClick={() => void refreshRecords()} variant="outline">
                <RefreshCw className="size-4" />
                Refresh
              </Button>
              <LinkButton to="/forms">
                <ArrowLeft className="size-4" />
                Forms
              </LinkButton>
            </div>
          }
        />
      </div>

      {error ? (
        <div data-print-hide="true">
          <Alert title="Records">{error}</Alert>
        </div>
      ) : null}

      <Card className="print-card">
        <CardHeader>
          <CardTitle>Records</CardTitle>
          <CardDescription>{totalCount} total submitted record{totalCount === 1 ? "" : "s"}</CardDescription>
        </CardHeader>
        <CardContent>
          <form className="mb-4 grid gap-3 md:grid-cols-[minmax(0,1fr)_auto_auto]" data-print-hide="true" onSubmit={handleSearch}>
            <Input
              aria-label="Search records"
              icon={<Search className="size-4" />}
              onChange={(event) => setQuery(event.target.value)}
              placeholder="Search values, status, or version"
              value={query}
            />
            <Button disabled={loading} type="submit">
              <Search className="size-4" />
              Search
            </Button>
            <Button disabled={loading || (!query && !submittedQuery)} onClick={clearSearch} variant="outline">
              Clear
            </Button>
          </form>

          {loading ? (
            <LoadingRecords />
          ) : records.length > 0 ? (
            <>
              <div className="hidden md:block print:block">
                <Table columns={recordColumns} rows={records} />
              </div>
              <div className="grid gap-3 md:hidden print:hidden">
                {records.map((record) => (
                  <MobileRecordSummary key={record.id} record={record} />
                ))}
              </div>
              <RecordPager
                loading={loading}
                onPageChange={(targetPage) => void refreshRecords(targetPage)}
                page={page}
                totalPages={totalPages}
              />
            </>
          ) : (
            <EmptyState
              title={submittedQuery ? "No records found" : "No records yet"}
              description={
                submittedQuery
                  ? "No submitted records match the current search."
                  : "Records will appear here after users submit the published form."
              }
              action={
                submittedQuery ? (
                  <Button onClick={clearSearch} variant="outline">
                    Clear search
                  </Button>
                ) : (
                  <LinkButton to="/forms">Forms</LinkButton>
                )
              }
            />
          )}
        </CardContent>
      </Card>
    </div>
  );
}

function MobileRecordSummary({ record }: { record: FormRecordListItem }) {
  return (
    <div className="rounded-xl border border-border bg-card/80 p-4">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <div className="flex items-center gap-2">
            <FileText className="size-4 shrink-0 text-muted-foreground" />
            <p className="truncate font-bold text-foreground">{shortId(record.id)}</p>
          </div>
          <p className="mt-2 text-sm leading-5 text-muted-foreground">{formatDateTime(record.createdAt)}</p>
        </div>
        <Badge className="shrink-0" variant={record.status === "active" ? "success" : "default"}>
          {record.status}
        </Badge>
      </div>
      <div className="mt-4">
        <ValuePreview values={record.values} />
      </div>
      <RecordDetailLink className="mt-4 w-full" recordId={record.id} />
    </div>
  );
}

function RecordDetailLink({ className, recordId }: { className?: string; recordId: string }) {
  return (
    <Link
      className={cn(
        "inline-flex min-h-8 items-center justify-center gap-2 rounded-xl border border-border bg-card/90 px-3 text-sm font-bold text-foreground transition hover:bg-muted",
        className
      )}
      to={`/records/${recordId}`}
    >
      <Eye className="size-4" />
      Detail
    </Link>
  );
}

function LinkButton({ children, to }: { children: ReactNode; to: string }) {
  return (
    <Link
      className="control-transition inline-flex min-h-10 items-center justify-center gap-2 rounded-xl border border-border bg-card/90 px-4 text-sm font-bold text-foreground hover:bg-muted"
      to={to}
    >
      {children}
    </Link>
  );
}

function PrintHeader({ description, title }: { description: string; title: string }) {
  return (
    <section className="print-only">
      <p className="text-xs font-bold uppercase tracking-normal text-muted-foreground">Records</p>
      <h1 className="mt-1 text-2xl font-bold text-foreground">{title}</h1>
      <p className="mt-1 text-sm text-muted-foreground">{description}</p>
    </section>
  );
}

function RecordPager({
  loading,
  onPageChange,
  page,
  totalPages
}: {
  loading: boolean;
  onPageChange: (page: number) => void;
  page: number;
  totalPages: number;
}) {
  return (
    <div className="mt-4 flex flex-wrap items-center justify-between gap-3 text-sm" data-print-hide="true">
      <span className="font-semibold text-muted-foreground">
        Page {page} of {totalPages}
      </span>
      <div className="flex gap-2">
        <Button disabled={loading || page <= 1} onClick={() => onPageChange(page - 1)} variant="outline">
          Previous
        </Button>
        <Button disabled={loading || page >= totalPages} onClick={() => onPageChange(page + 1)} variant="outline">
          Next
        </Button>
      </div>
    </div>
  );
}

function ValuePreview({ values }: { values: Record<string, FormRecordValue> }) {
  const entries = Object.entries(values).slice(0, 3);

  if (entries.length === 0) {
    return <span className="text-muted-foreground">No values</span>;
  }

  return (
    <dl className="grid gap-1 text-sm">
      {entries.map(([key, value]) => (
        <div className="grid gap-1 sm:grid-cols-[8rem_minmax(0,1fr)]" key={key}>
          <dt className="truncate font-bold text-muted-foreground">{key}</dt>
          <dd className="min-w-0 truncate text-foreground">{formatRecordValue(value)}</dd>
        </div>
      ))}
    </dl>
  );
}

function LoadingRecords() {
  return (
    <div className="grid gap-3">
      <Skeleton className="h-12" />
      <Skeleton className="h-12" />
      <Skeleton className="h-12" />
    </div>
  );
}

function shortId(value: string): string {
  return value.length > 8 ? value.slice(0, 8) : value;
}

function formatDateTime(value: string): string {
  return new Intl.DateTimeFormat("en", {
    month: "short",
    day: "numeric",
    year: "numeric",
    hour: "numeric",
    minute: "2-digit"
  }).format(new Date(value));
}

function formatRecordValue(value: FormRecordValue | undefined): string {
  if (value === null || value === undefined || value === "") return "Empty";
  if (typeof value === "boolean") return value ? "Yes" : "No";
  return String(value);
}

function getErrorMessage(error: unknown): string {
  return error instanceof Error ? error.message : "Records request failed.";
}

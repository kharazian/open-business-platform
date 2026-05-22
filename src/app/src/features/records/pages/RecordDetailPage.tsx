import { type ReactNode, useEffect, useMemo, useState } from "react";
import { ArrowLeft, FileText, RefreshCw } from "lucide-react";
import { Link, useParams } from "react-router-dom";
import { Alert } from "../../../components/ui/Alert";
import { Badge } from "../../../components/ui/Badge";
import { Button } from "../../../components/ui/Button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "../../../components/ui/Card";
import { EmptyState } from "../../../components/ui/EmptyState";
import { PageHeader } from "../../../components/ui/PageHeader";
import { Skeleton } from "../../../components/ui/Skeleton";
import { getRecord, type FormRecordDetail } from "../../forms/api";
import type { FormField, FormRecordValue } from "../../forms/types";

export function RecordDetailPage() {
  const { recordId } = useParams();
  const resolvedRecordId = recordId ?? "";
  const [record, setRecord] = useState<FormRecordDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    void refreshRecord();
  }, [resolvedRecordId]);

  const fieldsById = useMemo(() => {
    return new Map(record?.schema.fields.map((field) => [field.id, field]) ?? []);
  }, [record]);

  async function refreshRecord() {
    if (!resolvedRecordId) {
      setError("Record id is required.");
      setLoading(false);
      return;
    }

    setLoading(true);
    setError(null);

    try {
      setRecord(await getRecord(resolvedRecordId));
    } catch (caught) {
      setError(getErrorMessage(caught));
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="grid gap-6">
      <PageHeader
        eyebrow="Record detail"
        title={record ? shortId(record.id) : "Record"}
        description={record ? `Submitted ${formatDateTime(record.createdAt)}` : "Submitted record values and form version."}
        actions={
          <div className="flex flex-wrap gap-2">
            <Button disabled={loading} onClick={() => void refreshRecord()} variant="outline">
              <RefreshCw className="size-4" />
              Refresh
            </Button>
            {record ? (
              <LinkButton to={`/forms/${record.formId}/records`}>
                <ArrowLeft className="size-4" />
                Records
              </LinkButton>
            ) : (
              <LinkButton to="/forms">
                <ArrowLeft className="size-4" />
                Forms
              </LinkButton>
            )}
          </div>
        }
      />

      {error ? <Alert title="Record detail">{error}</Alert> : null}

      {loading ? (
        <LoadingRecord />
      ) : record ? (
        <>
          <section className="grid gap-4 md:grid-cols-3">
            <SummaryTile label="Status" value={<Badge variant={record.status === "active" ? "success" : "default"}>{record.status}</Badge>} />
            <SummaryTile label="Form version" value={shortId(record.formVersionId)} />
            <SummaryTile label="Created" value={formatDateTime(record.createdAt)} />
          </section>

          <Card>
            <CardHeader>
              <CardTitle>Values</CardTitle>
              <CardDescription>Values captured against the stored form version schema.</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="grid gap-3">
                {record.schema.fields.map((field) => (
                  <ValueRow field={field} key={field.id} value={record.values[field.id]} />
                ))}
                {Object.keys(record.values)
                  .filter((fieldId) => !fieldsById.has(fieldId))
                  .map((fieldId) => (
                    <ValueRow key={fieldId} label={fieldId} value={record.values[fieldId]} />
                  ))}
              </div>
            </CardContent>
          </Card>
        </>
      ) : (
        <EmptyState title="Record not found" description="The requested record could not be loaded." action={<LinkButton to="/forms">Forms</LinkButton>} />
      )}
    </div>
  );
}

function ValueRow({ field, label, value }: { field?: FormField; label?: string; value: FormRecordValue | undefined }) {
  return (
    <div className="grid gap-2 rounded-xl border border-border bg-card/70 p-4 md:grid-cols-[14rem_minmax(0,1fr)]">
      <div className="min-w-0">
        <p className="truncate font-bold text-foreground">{field?.label ?? label}</p>
        {field ? <p className="mt-1 text-xs font-semibold uppercase tracking-normal text-muted-foreground">{field.type}</p> : null}
      </div>
      <p className="min-w-0 whitespace-pre-wrap break-words text-sm leading-6 text-foreground">{formatRecordValue(value)}</p>
    </div>
  );
}

function SummaryTile({ label, value }: { label: string; value: ReactNode }) {
  return (
    <Card className="p-5">
      <p className="text-sm font-bold text-muted-foreground">{label}</p>
      <div className="mt-3 text-base font-bold text-foreground">{value}</div>
    </Card>
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

function LoadingRecord() {
  return (
    <div className="grid gap-4">
      <section className="grid gap-4 md:grid-cols-3">
        <Skeleton className="h-24" />
        <Skeleton className="h-24" />
        <Skeleton className="h-24" />
      </section>
      <Skeleton className="h-64" />
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
  return error instanceof Error ? error.message : "Record request failed.";
}

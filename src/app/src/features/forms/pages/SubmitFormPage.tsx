import { type ReactNode, useEffect, useState } from "react";
import { ArrowLeft, CheckCircle2, FileText, RefreshCw } from "lucide-react";
import { Link, useParams } from "react-router-dom";
import { Alert } from "../../../components/ui/Alert";
import { Badge } from "../../../components/ui/Badge";
import { Button } from "../../../components/ui/Button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "../../../components/ui/Card";
import { EmptyState } from "../../../components/ui/EmptyState";
import { PageHeader } from "../../../components/ui/PageHeader";
import { Skeleton } from "../../../components/ui/Skeleton";
import { cn } from "../../../lib/cn";
import { FormRenderer } from "../components/FormRenderer";
import {
  FormsApiError,
  getPublishedFormForSubmission,
  submitRecord,
  type FormRecord,
  type PublishedFormForSubmission
} from "../api";
import {
  clearSubmissionFieldErrors,
  createPublishedFormSubmissionValues,
  getSubmissionSuccessLinks
} from "../submission";
import type { FormRecordValue, FormRecordValues, ValidationError } from "../types";
import { validateRecordValues } from "../validation";

export function SubmitFormPage() {
  const { formId } = useParams();
  const resolvedFormId = formId ?? "";
  const [form, setForm] = useState<PublishedFormForSubmission | null>(null);
  const [values, setValues] = useState<FormRecordValues>({});
  const [validationErrors, setValidationErrors] = useState<ValidationError[]>([]);
  const [record, setRecord] = useState<FormRecord | null>(null);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    void refreshForm();
  }, [resolvedFormId]);

  async function refreshForm() {
    if (!resolvedFormId) {
      setError("Form id is required.");
      setLoading(false);
      return;
    }

    setLoading(true);
    setError(null);
    setRecord(null);

    try {
      const loadedForm = await getPublishedFormForSubmission(resolvedFormId);
      setForm(loadedForm);
      setValues(createPublishedFormSubmissionValues(loadedForm));
      setValidationErrors([]);
    } catch (caught) {
      setForm(null);
      setError(getErrorMessage(caught));
    } finally {
      setLoading(false);
    }
  }

  function handleValueChange(fieldId: string, value: FormRecordValue) {
    setValues((currentValues) => ({ ...currentValues, [fieldId]: value }));
    setValidationErrors((currentErrors) => clearSubmissionFieldErrors(currentErrors, fieldId));
    setError(null);
  }

  async function handleSubmit() {
    if (!form || submitting) {
      return;
    }

    const validation = validateRecordValues(form.schema, values);
    setValidationErrors(validation.errors);

    if (!validation.valid) {
      return;
    }

    setSubmitting(true);
    setError(null);

    try {
      const createdRecord = await submitRecord(form.id, { values });
      setRecord(createdRecord);
      setValidationErrors([]);
    } catch (caught) {
      if (caught instanceof FormsApiError && caught.errors.length > 0) {
        setValidationErrors(caught.errors);
      }

      setError(getErrorMessage(caught));
    } finally {
      setSubmitting(false);
    }
  }

  const title = loading ? "Loading form..." : form?.name ?? "Submit form";

  return (
    <div className="grid gap-6">
      <PageHeader
        eyebrow="Submit form"
        title={title}
        description={form?.description ?? "Enter values for the current published version."}
        actions={
          <div className="flex flex-wrap gap-2">
            <Button disabled={loading || submitting} onClick={() => void refreshForm()} variant="outline">
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

      {error ? <Alert title="Submit form">{error}</Alert> : null}

      {loading ? (
        <LoadingSubmissionForm />
      ) : form && record ? (
        <SubmissionSuccess form={form} record={record} />
      ) : form ? (
        <Card>
          <CardHeader>
            <div className="flex flex-wrap items-start justify-between gap-3">
              <div>
                <CardTitle>Version {form.currentVersionNumber}</CardTitle>
                <CardDescription>Values are saved against this published version.</CardDescription>
              </div>
              <Badge>Published</Badge>
            </div>
          </CardHeader>
          <CardContent>
            {validationErrors.length > 0 ? (
              <Alert title="Validation">Fix the highlighted fields before submitting.</Alert>
            ) : null}
            <div className={validationErrors.length > 0 ? "mt-4" : undefined}>
              <FormRenderer
                errors={validationErrors}
                onChange={handleValueChange}
                onSubmit={() => void handleSubmit()}
                schema={form.schema}
                submitLabel={submitting ? "Submitting..." : "Submit record"}
                values={values}
              />
            </div>
          </CardContent>
        </Card>
      ) : (
        <EmptyState title="Form unavailable" description="This form is not available for submission." action={<LinkButton to="/forms">Forms</LinkButton>} />
      )}
    </div>
  );
}

function SubmissionSuccess({ form, record }: { form: PublishedFormForSubmission; record: FormRecord }) {
  const links = getSubmissionSuccessLinks(record);

  return (
    <Card>
      <CardHeader>
        <div className="flex flex-wrap items-start gap-3">
          <div className="grid size-10 place-items-center rounded-xl bg-success/10 text-success">
            <CheckCircle2 className="size-5" />
          </div>
          <div>
            <CardTitle>Record submitted</CardTitle>
            <CardDescription>{form.name} version {form.currentVersionNumber}</CardDescription>
          </div>
        </div>
      </CardHeader>
      <CardContent>
        <dl className="grid gap-3 text-sm md:grid-cols-2">
          <div className="rounded-xl border border-border bg-card/70 p-4">
            <dt className="font-bold text-muted-foreground">Record</dt>
            <dd className="mt-1 font-bold text-foreground">{shortId(record.id)}</dd>
          </div>
          <div className="rounded-xl border border-border bg-card/70 p-4">
            <dt className="font-bold text-muted-foreground">Version</dt>
            <dd className="mt-1 font-bold text-foreground">{shortId(record.formVersionId)}</dd>
          </div>
        </dl>
        <div className="mt-4 flex flex-wrap gap-2">
          <LinkButton to={links.recordPath}>
            <FileText className="size-4" />
            Record detail
          </LinkButton>
          <LinkButton to={links.recordsPath}>Records</LinkButton>
          <LinkButton to={links.formsPath}>Forms</LinkButton>
        </div>
      </CardContent>
    </Card>
  );
}

function LinkButton({ children, className, to }: { children: ReactNode; className?: string; to: string }) {
  return (
    <Link
      className={cn(
        "control-transition inline-flex min-h-10 items-center justify-center gap-2 rounded-xl border border-border bg-card/90 px-4 text-sm font-bold text-foreground hover:bg-muted",
        className
      )}
      to={to}
    >
      {children}
    </Link>
  );
}

function LoadingSubmissionForm() {
  return (
    <div className="grid gap-4">
      <Skeleton className="h-20" />
      <Skeleton className="h-80" />
    </div>
  );
}

function shortId(value: string): string {
  return value.length > 8 ? value.slice(0, 8) : value;
}

function getErrorMessage(error: unknown): string {
  return error instanceof Error ? error.message : "Form submission failed.";
}

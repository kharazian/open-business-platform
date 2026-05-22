import { useEffect, useMemo, useState } from "react";
import { FileText, Plus, RefreshCw, Save } from "lucide-react";
import { Alert } from "../../../components/ui/Alert";
import { Badge } from "../../../components/ui/Badge";
import { Button } from "../../../components/ui/Button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "../../../components/ui/Card";
import { Checkbox } from "../../../components/ui/Checkbox";
import { EmptyState } from "../../../components/ui/EmptyState";
import { Input } from "../../../components/ui/Input";
import { PageHeader } from "../../../components/ui/PageHeader";
import { Select } from "../../../components/ui/Select";
import { Table, type TableColumn } from "../../../components/ui/Table";
import { getForm, listForms, type FormDetail } from "../../forms/api";
import { getFormStatusLabel, type FormSummary } from "../../forms/drafts";
import { createListReportConfig, getReportFieldOptions } from "../builder";
import { createListReport, listReports } from "../api";
import {
  type ListReportFilter,
  type ListReportSummary,
  type ReportFilterOperator,
  type ReportSortDirection
} from "../types";

const filterOperatorOptions = [
  { label: "Equals", value: "equals" },
  { label: "Contains", value: "contains" },
  { label: "Is empty", value: "is_empty" },
  { label: "Is not empty", value: "is_not_empty" }
];
const sortDirectionOptions = [
  { label: "Ascending", value: "asc" },
  { label: "Descending", value: "desc" }
];

const reportColumns: Array<TableColumn<ListReportSummary>> = [
  { header: "Report", accessor: "name" },
  { header: "Form", accessor: "formName" },
  {
    header: "Config",
    render: (report) => `${report.columnCount} columns, ${report.filterCount} filters, ${report.sortCount} sorts`
  },
  {
    header: "Updated",
    render: (report) => formatDate(report.updatedAt ?? report.createdAt)
  }
];

export function ReportsPage() {
  const [forms, setForms] = useState<FormSummary[]>([]);
  const [selectedFormId, setSelectedFormId] = useState("");
  const [formDetail, setFormDetail] = useState<FormDetail | null>(null);
  const [reports, setReports] = useState<ListReportSummary[]>([]);
  const [reportName, setReportName] = useState("");
  const [selectedFieldIds, setSelectedFieldIds] = useState<string[]>([]);
  const [filterFieldId, setFilterFieldId] = useState("");
  const [filterOperator, setFilterOperator] = useState<ReportFilterOperator>("equals");
  const [filterValue, setFilterValue] = useState("");
  const [sortFieldId, setSortFieldId] = useState("created_at");
  const [sortDirection, setSortDirection] = useState<ReportSortDirection>("desc");
  const [loadingForms, setLoadingForms] = useState(true);
  const [loadingFormDetail, setLoadingFormDetail] = useState(false);
  const [loadingReports, setLoadingReports] = useState(false);
  const [savingReport, setSavingReport] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [reportNameError, setReportNameError] = useState<string | undefined>();

  useEffect(() => {
    let active = true;
    setLoadingForms(true);
    setError(null);

    listForms()
      .then((items) => {
        if (!active) return;
        setForms(items);
        setSelectedFormId((current) => current || items[0]?.id || "");
      })
      .catch((caught: unknown) => {
        if (!active) return;
        setError(getErrorMessage(caught));
      })
      .finally(() => {
        if (active) setLoadingForms(false);
      });

    return () => {
      active = false;
    };
  }, []);

  useEffect(() => {
    if (!selectedFormId) {
      setFormDetail(null);
      setReports([]);
      return;
    }

    let active = true;
    setLoadingFormDetail(true);
    setLoadingReports(true);
    setError(null);
    setNotice(null);

    Promise.all([getForm(selectedFormId), listReports(selectedFormId)])
      .then(([form, reportItems]) => {
        if (!active) return;
        setFormDetail(form);
        setReports(reportItems);
        setReportName((current) => current || `${form.name} list`);
      })
      .catch((caught: unknown) => {
        if (!active) return;
        setError(getErrorMessage(caught));
        setFormDetail(null);
        setReports([]);
      })
      .finally(() => {
        if (!active) return;
        setLoadingFormDetail(false);
        setLoadingReports(false);
      });

    return () => {
      active = false;
    };
  }, [selectedFormId]);

  const fieldOptions = useMemo(() => (formDetail ? getReportFieldOptions(formDetail.draftSchema) : []), [formDetail]);

  useEffect(() => {
    if (fieldOptions.length === 0) {
      setSelectedFieldIds([]);
      return;
    }

    setSelectedFieldIds((current) => {
      const validCurrent = current.filter((fieldId) => fieldOptions.some((field) => field.id === fieldId));
      return validCurrent.length > 0 ? validCurrent : fieldOptions.slice(0, Math.min(5, fieldOptions.length)).map((field) => field.id);
    });
  }, [fieldOptions]);

  const selectedForm = forms.find((form) => form.id === selectedFormId) ?? null;
  const fieldSelectOptions = [{ label: "No field", value: "" }, ...fieldOptions.map((field) => ({ label: field.label, value: field.id }))];
  const sortFieldSelectOptions = fieldOptions.map((field) => ({ label: field.label, value: field.id }));
  const previewConfig = createListReportConfig({
    fieldOptions,
    selectedFieldIds,
    filters: buildFilters(),
    sort: buildSort()
  });

  async function handleRefresh() {
    if (!selectedFormId) return;
    setLoadingReports(true);
    setError(null);
    setNotice(null);

    try {
      setReports(await listReports(selectedFormId));
    } catch (caught) {
      setError(getErrorMessage(caught));
    } finally {
      setLoadingReports(false);
    }
  }

  async function handleCreateReport() {
    const name = reportName.trim();

    setError(null);
    setNotice(null);

    if (!name) {
      setReportNameError("Report name is required.");
      return;
    }

    setSavingReport(true);

    try {
      await createListReport(selectedFormId, { name, config: previewConfig });
      setReports(await listReports(selectedFormId));
      setNotice("List report saved.");
      setReportName(`${formDetail?.name ?? "Form"} list`);
      setReportNameError(undefined);
    } catch (caught) {
      setError(getErrorMessage(caught));
    } finally {
      setSavingReport(false);
    }
  }

  function handleToggleField(fieldId: string, selected: boolean) {
    setNotice(null);
    setSelectedFieldIds((current) => {
      if (selected) {
        return current.includes(fieldId) ? current : [...current, fieldId];
      }

      return current.filter((currentFieldId) => currentFieldId !== fieldId);
    });
  }

  function buildFilters(): ListReportFilter[] {
    if (!filterFieldId) {
      return [];
    }

    return [{ fieldId: filterFieldId, operator: filterOperator, value: filterRequiresValue(filterOperator) ? filterValue : null }];
  }

  function buildSort() {
    return sortFieldId ? [{ fieldId: sortFieldId, direction: sortDirection }] : [];
  }

  return (
    <div className="grid gap-6">
      <PageHeader
        eyebrow="Reports"
        title="List report builder"
        description="Create saved list report definitions from form fields."
        actions={
          <div className="flex flex-wrap gap-2">
            <Button disabled={!selectedFormId || loadingReports} onClick={handleRefresh} variant="outline">
              <RefreshCw className="size-4" />
              Refresh
            </Button>
            <Button disabled={!selectedFormId || fieldOptions.length === 0 || savingReport} onClick={handleCreateReport}>
              <Save className="size-4" />
              {savingReport ? "Saving..." : "Save report"}
            </Button>
          </div>
        }
      />

      {error ? <Alert title="Reports">{error}</Alert> : null}
      {notice ? <div className="rounded-xl border border-success/40 bg-success/10 px-4 py-3 text-sm font-semibold text-success">{notice}</div> : null}

      <section className="grid gap-4 xl:grid-cols-[20rem_minmax(0,1fr)]">
        <Card className="self-start">
          <CardHeader>
            <CardTitle>Form</CardTitle>
            <CardDescription>Report source.</CardDescription>
          </CardHeader>
          <CardContent className="grid gap-4">
            <Select
              disabled={loadingForms || forms.length === 0}
              label="Form"
              onChange={(event) => setSelectedFormId(event.target.value)}
              value={selectedFormId}
            >
              {forms.map((form) => (
                <option key={form.id} value={form.id}>
                  {form.name}
                </option>
              ))}
            </Select>
            {selectedForm ? (
              <div className="rounded-xl border border-border bg-muted/30 p-4">
                <div className="flex items-start justify-between gap-3">
                  <div>
                    <p className="font-bold text-foreground">{selectedForm.name}</p>
                    {selectedForm.description ? <p className="mt-1 text-sm leading-5 text-muted-foreground">{selectedForm.description}</p> : null}
                  </div>
                  <Badge>{getFormStatusLabel(selectedForm.status)}</Badge>
                </div>
                <dl className="mt-4 grid grid-cols-2 gap-3 text-sm">
                  <div>
                    <dt className="font-bold text-muted-foreground">Fields</dt>
                    <dd className="mt-1 text-foreground">{fieldOptions.length}</dd>
                  </div>
                  <div>
                    <dt className="font-bold text-muted-foreground">Reports</dt>
                    <dd className="mt-1 text-foreground">{reports.length}</dd>
                  </div>
                </dl>
              </div>
            ) : (
              <EmptyState title="No form selected" description="Create a form before building reports." />
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <div className="flex items-start justify-between gap-3">
              <div>
                <CardTitle>Builder</CardTitle>
                <CardDescription>Columns, filter, and sort.</CardDescription>
              </div>
              <Badge>{selectedFieldIds.length} columns</Badge>
            </div>
          </CardHeader>
          <CardContent>
            {loadingFormDetail ? (
              <EmptyState title="Loading form" description="Fetching report fields." />
            ) : fieldOptions.length > 0 ? (
              <div className="grid gap-5">
                <Input
                  error={reportNameError}
                  label="Report name"
                  onChange={(event) => {
                    setReportName(event.target.value);
                    if (reportNameError) setReportNameError(undefined);
                  }}
                  value={reportName}
                />
                <div className="grid gap-3">
                  <div className="flex items-center justify-between gap-3">
                    <p className="text-sm font-bold text-foreground">Columns</p>
                    <Badge>{previewConfig.columns.length} visible</Badge>
                  </div>
                  <div className="grid gap-2 md:grid-cols-2 xl:grid-cols-3">
                    {fieldOptions.map((field) => (
                      <Checkbox
                        checked={selectedFieldIds.includes(field.id)}
                        description={field.source === "system" ? "System field" : "Form field"}
                        key={field.id}
                        label={field.label}
                        onChange={(event) => handleToggleField(field.id, event.target.checked)}
                      />
                    ))}
                  </div>
                </div>
                <div className="grid gap-4 lg:grid-cols-3">
                  <Select label="Filter field" onChange={(event) => setFilterFieldId(event.target.value)} value={filterFieldId}>
                    {fieldSelectOptions.map((option) => (
                      <option key={option.value} value={option.value}>
                        {option.label}
                      </option>
                    ))}
                  </Select>
                  <Select
                    disabled={!filterFieldId}
                    label="Operator"
                    onChange={(event) => setFilterOperator(event.target.value as ReportFilterOperator)}
                    options={filterOperatorOptions}
                    value={filterOperator}
                  />
                  <Input
                    disabled={!filterFieldId || !filterRequiresValue(filterOperator)}
                    label="Filter value"
                    onChange={(event) => setFilterValue(event.target.value)}
                    value={filterValue}
                  />
                </div>
                <div className="grid gap-4 lg:grid-cols-2">
                  <Select label="Sort field" onChange={(event) => setSortFieldId(event.target.value)} value={sortFieldId}>
                    {sortFieldSelectOptions.map((option) => (
                      <option key={option.value} value={option.value}>
                        {option.label}
                      </option>
                    ))}
                  </Select>
                  <Select
                    label="Sort direction"
                    onChange={(event) => setSortDirection(event.target.value as ReportSortDirection)}
                    options={sortDirectionOptions}
                    value={sortDirection}
                  />
                </div>
              </div>
            ) : (
              <EmptyState title="No report fields" description="Save fields on this form before creating list reports." />
            )}
          </CardContent>
        </Card>
      </section>

      <Card>
        <CardHeader>
          <div className="flex items-start justify-between gap-3">
            <div>
              <CardTitle>Saved list reports</CardTitle>
              <CardDescription>Definitions available for the selected form.</CardDescription>
            </div>
            <FileText className="size-5 text-muted-foreground" />
          </div>
        </CardHeader>
        <CardContent>
          {reports.length > 0 ? (
            <Table columns={reportColumns} rows={reports} />
          ) : (
            <EmptyState
              action={
                <Button disabled={!selectedFormId || fieldOptions.length === 0} onClick={handleCreateReport} variant="outline">
                  <Plus className="size-4" />
                  Save first report
                </Button>
              }
              description={loadingReports ? "Fetching report definitions." : "Save a list report definition for this form."}
              title={loadingReports ? "Loading reports" : "No reports"}
            />
          )}
        </CardContent>
      </Card>
    </div>
  );
}

function filterRequiresValue(operator: ReportFilterOperator): boolean {
  return operator === "equals" || operator === "contains";
}

function formatDate(value: string | null | undefined): string {
  if (!value) return "Never";

  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short"
  }).format(new Date(value));
}

function getErrorMessage(error: unknown): string {
  return error instanceof Error ? error.message : "Reports request failed.";
}

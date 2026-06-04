import { type FormEvent, useEffect, useMemo, useState } from "react";
import { ArrowDown, ArrowUp, ChevronLeft, ChevronRight, Download, FileText, Play, Plus, Printer, RefreshCw, Save, Search } from "lucide-react";
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
import { listForms, type FormDetail } from "../../forms/api";
import { getFormStatusLabel, type FormSummary } from "../../forms/drafts";
import { PrintDocumentFooter, PrintDocumentHeader } from "../../printing/components/PrintDocument";
import { getGeneratedAtPrintMetadata, requestBrowserPrint } from "../../printing/printLayout";
import { createListReportConfig, getReportFieldOptions } from "../builder";
import { createListReport, downloadListReportCsv, executeListReport, listReports } from "../api";
import { getReportTablePrintDescription } from "../reportPrint";
import { loadReportWorkspace } from "../workspace";
import {
  type ListReportExecution,
  type ListReportExecutionRow,
  type ListReportFilter,
  type ListReportSummary,
  type ReportFilterOperator,
  type ReportSortDirection
} from "../types";

const reportPageSize = 10;

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

export function ReportsPage() {
  const [forms, setForms] = useState<FormSummary[]>([]);
  const [selectedFormId, setSelectedFormId] = useState("");
  const [formDetail, setFormDetail] = useState<FormDetail | null>(null);
  const [reports, setReports] = useState<ListReportSummary[]>([]);
  const [selectedReportId, setSelectedReportId] = useState("");
  const [reportExecution, setReportExecution] = useState<ListReportExecution | null>(null);
  const [reportSearch, setReportSearch] = useState("");
  const [executedReportSearch, setExecutedReportSearch] = useState("");
  const [reportPage, setReportPage] = useState(1);
  const [reportName, setReportName] = useState("");
  const [selectedFieldIds, setSelectedFieldIds] = useState<string[]>([]);
  const [columnLabels, setColumnLabels] = useState<Record<string, string>>({});
  const [filterFieldId, setFilterFieldId] = useState("");
  const [filterOperator, setFilterOperator] = useState<ReportFilterOperator>("equals");
  const [filterValue, setFilterValue] = useState("");
  const [sortFieldId, setSortFieldId] = useState("created_at");
  const [sortDirection, setSortDirection] = useState<ReportSortDirection>("desc");
  const [loadingForms, setLoadingForms] = useState(true);
  const [loadingFormDetail, setLoadingFormDetail] = useState(false);
  const [loadingReports, setLoadingReports] = useState(false);
  const [savingReport, setSavingReport] = useState(false);
  const [runningReport, setRunningReport] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [viewerError, setViewerError] = useState<string | null>(null);
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
      setSelectedReportId("");
      setReportExecution(null);
      return;
    }

    let active = true;
    setLoadingFormDetail(true);
    setLoadingReports(true);
    setError(null);
    setViewerError(null);
    setNotice(null);
    setSelectedReportId("");
    setReportExecution(null);
    setReportSearch("");
    setExecutedReportSearch("");
    setReportPage(1);

    loadReportWorkspace(selectedFormId)
      .then((workspace) => {
        if (!active) return;
        setFormDetail(workspace.formDetail);
        setReports(workspace.reports);
        setReportName((current) => current || `${workspace.formDetail?.name ?? forms.find((form) => form.id === selectedFormId)?.name ?? "Form"} list`);
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
  }, [forms, selectedFormId]);

  const fieldOptions = useMemo(() => (formDetail ? getReportFieldOptions(formDetail.draftSchema) : []), [formDetail]);

  useEffect(() => {
    if (fieldOptions.length === 0) {
      setSelectedFieldIds([]);
      setColumnLabels({});
      return;
    }

    setSelectedFieldIds((current) => {
      const validCurrent = current.filter((fieldId) => fieldOptions.some((field) => field.id === fieldId));
      return validCurrent.length > 0 ? validCurrent : fieldOptions.slice(0, Math.min(5, fieldOptions.length)).map((field) => field.id);
    });

    setColumnLabels((current) => {
      const nextLabels: Record<string, string> = {};

      for (const field of fieldOptions) {
        nextLabels[field.id] = current[field.id] ?? field.label;
      }

      return nextLabels;
    });
  }, [fieldOptions]);

  const selectedForm = forms.find((form) => form.id === selectedFormId) ?? null;
  const fieldSelectOptions = [{ label: "No field", value: "" }, ...fieldOptions.map((field) => ({ label: field.label, value: field.id }))];
  const sortFieldSelectOptions = fieldOptions.map((field) => ({ label: field.label, value: field.id }));
  const previewConfig = createListReportConfig({
    fieldOptions,
    selectedFieldIds,
    columnLabels,
    filters: buildFilters(),
    sort: buildSort()
  });
  const selectedReport = reports.find((report) => report.id === selectedReportId) ?? null;
  const totalReportPages = reportExecution ? Math.max(1, Math.ceil(reportExecution.totalCount / reportExecution.pageSize)) : 1;
  const reportPrintDescription = reportExecution
    ? getReportTablePrintDescription(reportExecution.totalCount, reportExecution.page, totalReportPages, executedReportSearch)
    : "";
  const executionColumns = useMemo<Array<TableColumn<ListReportExecutionRow>>>(
    () =>
      reportExecution?.columns.map((column) => ({
        header: column.label,
        render: (row) => {
          const value = row.cells[column.fieldId]?.displayValue?.trim();
          return value ? value : <span className="text-muted-foreground">-</span>;
        }
      })) ?? [],
    [reportExecution]
  );
  const reportColumns = useMemo<Array<TableColumn<ListReportSummary>>>(
    () => [
      { header: "Report", accessor: "name" },
      { header: "Form", accessor: "formName" },
      {
        header: "Config",
        render: (report) => `${report.columnCount} columns, ${report.filterCount} filters, ${report.sortCount} sorts`
      },
      {
        header: "Updated",
        render: (report) => formatDate(report.updatedAt ?? report.createdAt)
      },
      {
        header: "Run",
        render: (report) => (
          <Button
            disabled={runningReport && selectedReportId === report.id}
            onClick={() => handleRunReport(report.id, 1)}
            size="sm"
            variant={selectedReportId === report.id ? "secondary" : "outline"}
          >
            <Play className="size-4" />
            {runningReport && selectedReportId === report.id ? "Running..." : "Run"}
          </Button>
        )
      }
    ],
    [runningReport, selectedReportId]
  );

  async function handleRefresh() {
    if (!selectedFormId) return;
    setLoadingReports(true);
    setError(null);
    setNotice(null);

    try {
      const refreshedReports = await listReports(selectedFormId);
      setReports(refreshedReports);

      if (selectedReportId && !refreshedReports.some((report) => report.id === selectedReportId)) {
        setSelectedReportId("");
        setReportExecution(null);
        setExecutedReportSearch("");
      }
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
      const createdReport = await createListReport(selectedFormId, { name, config: previewConfig });
      setReports(await listReports(selectedFormId));
      setNotice("List report saved.");
      setReportName(`${formDetail?.name ?? "Form"} list`);
      setReportNameError(undefined);
      await handleRunReport(createdReport.id, 1);
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

    if (selected) {
      const field = fieldOptions.find((option) => option.id === fieldId);
      if (field) {
        setColumnLabels((current) => ({ ...current, [fieldId]: current[fieldId] ?? field.label }));
      }
    }
  }

  function handleMoveSelectedField(fieldId: string, direction: -1 | 1) {
    setNotice(null);
    setSelectedFieldIds((current) => {
      const index = current.indexOf(fieldId);
      const targetIndex = index + direction;

      if (index < 0 || targetIndex < 0 || targetIndex >= current.length) {
        return current;
      }

      const nextFieldIds = [...current];
      const [field] = nextFieldIds.splice(index, 1);
      nextFieldIds.splice(targetIndex, 0, field);
      return nextFieldIds;
    });
  }

  function handleColumnLabelChange(fieldId: string, label: string) {
    setNotice(null);
    setColumnLabels((current) => ({ ...current, [fieldId]: label }));
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

  async function handleRunReport(reportId: string, page: number) {
    if (!selectedFormId || !reportId) {
      return;
    }

    setSelectedReportId(reportId);
    setReportPage(page);
    setRunningReport(true);
    setViewerError(null);

    try {
      const search = reportSearch;
      setReportExecution(await executeListReport(selectedFormId, reportId, { page, pageSize: reportPageSize, search }));
      setExecutedReportSearch(search);
    } catch (caught) {
      setViewerError(getErrorMessage(caught));
      setReportExecution(null);
    } finally {
      setRunningReport(false);
    }
  }

  function handleSearchSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    void handleRunReport(selectedReportId, 1);
  }

  return (
    <div className="grid gap-6 print-area">
      {reportExecution ? (
        <PrintDocumentHeader
          description={reportPrintDescription}
          eyebrow="Report table"
          metadata={[reportExecution.formName, getGeneratedAtPrintMetadata()]}
          title={reportExecution.reportName}
        />
      ) : null}

      <div data-print-hide="true">
        <PageHeader
          eyebrow="Reports"
          title="List report definitions"
          description="Create saved V2 list report definitions from form fields."
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
      </div>

      {error ? (
        <div data-print-hide="true">
          <Alert title="Reports">{error}</Alert>
        </div>
      ) : null}
      {notice ? (
        <div className="rounded-xl border border-success/40 bg-success/10 px-4 py-3 text-sm font-semibold text-success" data-print-hide="true">
          {notice}
        </div>
      ) : null}

      <section className="grid gap-4 xl:grid-cols-[20rem_minmax(0,1fr)]" data-print-hide="true">
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
                {selectedFieldIds.length > 0 ? (
                  <div className="grid gap-3">
                    <div className="flex items-center justify-between gap-3">
                      <p className="text-sm font-bold text-foreground">Selected columns</p>
                      <Badge>{selectedFieldIds.length} ordered</Badge>
                    </div>
                    <div className="grid gap-2">
                      {selectedFieldIds.map((fieldId, index) => {
                        const field = fieldOptions.find((option) => option.id === fieldId);

                        if (!field) {
                          return null;
                        }

                        return (
                          <div className="grid gap-2 rounded-xl border border-border bg-muted/20 p-3 md:grid-cols-[auto_minmax(0,1fr)_auto]" key={fieldId}>
                            <Badge>{index + 1}</Badge>
                            <Input
                              label={field.label}
                              onChange={(event) => handleColumnLabelChange(fieldId, event.target.value)}
                              value={columnLabels[fieldId] ?? field.label}
                            />
                            <div className="flex items-end gap-2">
                              <Button
                                aria-label={`Move ${field.label} up`}
                                disabled={index === 0}
                                onClick={() => handleMoveSelectedField(fieldId, -1)}
                                size="icon"
                                title={`Move ${field.label} up`}
                                variant="outline"
                              >
                                <ArrowUp className="size-4" />
                              </Button>
                              <Button
                                aria-label={`Move ${field.label} down`}
                                disabled={index === selectedFieldIds.length - 1}
                                onClick={() => handleMoveSelectedField(fieldId, 1)}
                                size="icon"
                                title={`Move ${field.label} down`}
                                variant="outline"
                              >
                                <ArrowDown className="size-4" />
                              </Button>
                            </div>
                          </div>
                        );
                      })}
                    </div>
                  </div>
                ) : null}
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

      <Card data-print-hide="true">
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

      <Card className="print-card">
        <CardHeader data-print-hide="true">
          <div className="flex flex-wrap items-start justify-between gap-3">
            <div>
              <CardTitle>Report viewer</CardTitle>
              <CardDescription>{selectedReport ? selectedReport.name : "Run a saved list report."}</CardDescription>
            </div>
            <div className="flex flex-wrap gap-2">
              <Badge>{reportExecution ? `${reportExecution.totalCount} rows` : "Not run"}</Badge>
              <Button disabled={!reportExecution || runningReport} onClick={() => requestBrowserPrint()} variant="outline">
                <Printer className="size-4" />
                Print
              </Button>
              <Button
                disabled={!reportExecution || runningReport}
                onClick={() => {
                  if (reportExecution) {
                    downloadListReportCsv(reportExecution.formId, reportExecution.reportId, { search: executedReportSearch });
                  }
                }}
                variant="outline"
              >
                <Download className="size-4" />
                Export CSV
              </Button>
            </div>
          </div>
        </CardHeader>
        <CardContent className="grid gap-4">
          <form className="grid gap-3 lg:grid-cols-[minmax(0,1fr)_auto]" data-print-hide="true" onSubmit={handleSearchSubmit}>
            <Input
              disabled={!selectedReportId || runningReport}
              icon={<Search className="size-4" />}
              label="Search"
              onChange={(event) => setReportSearch(event.target.value)}
              placeholder="Search visible report columns"
              value={reportSearch}
            />
            <div className="flex flex-wrap items-end gap-2">
              <Button disabled={!selectedReportId || runningReport} type="submit" variant="outline">
                <Search className="size-4" />
                Search
              </Button>
              <Button disabled={!selectedReportId || runningReport} onClick={() => handleRunReport(selectedReportId, reportPage)} variant="outline">
                <RefreshCw className="size-4" />
                Refresh
              </Button>
            </div>
          </form>

          {viewerError ? <Alert title="Report viewer">{viewerError}</Alert> : null}

          {runningReport ? (
            <div className="rounded-xl border border-dashed border-border bg-muted/40 p-8 text-center">
              <RefreshCw className="mx-auto size-8 animate-spin text-muted-foreground" />
              <p className="mt-3 text-sm font-bold text-foreground">Running report</p>
            </div>
          ) : reportExecution && reportExecution.rows.length > 0 ? (
            <div className="grid gap-4">
              <Table columns={executionColumns} rows={reportExecution.rows} />
              <div className="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-border bg-muted/30 px-4 py-3 text-sm">
                <span className="font-semibold text-muted-foreground">
                  Page {reportExecution.page} of {totalReportPages}
                </span>
                <div className="flex gap-2">
                  <Button
                    disabled={reportExecution.page <= 1 || runningReport}
                    onClick={() => handleRunReport(reportExecution.reportId, reportExecution.page - 1)}
                    size="sm"
                    variant="outline"
                  >
                    <ChevronLeft className="size-4" />
                    Previous
                  </Button>
                  <Button
                    disabled={reportExecution.page >= totalReportPages || runningReport}
                    onClick={() => handleRunReport(reportExecution.reportId, reportExecution.page + 1)}
                    size="sm"
                    variant="outline"
                  >
                    Next
                    <ChevronRight className="size-4" />
                  </Button>
                </div>
              </div>
            </div>
          ) : reportExecution ? (
            <EmptyState
              action={
                <Button disabled={runningReport} onClick={() => handleRunReport(reportExecution.reportId, 1)} variant="outline">
                  <RefreshCw className="size-4" />
                  Refresh
                </Button>
              }
              description="The saved filters and search did not match any records."
              title="No matching rows"
            />
          ) : (
            <EmptyState
              action={
                selectedReport ? (
                  <Button disabled={runningReport} onClick={() => handleRunReport(selectedReport.id, 1)} variant="outline">
                    <Play className="size-4" />
                    Run report
                  </Button>
                ) : (
                  <Button disabled variant="outline">
                    <Play className="size-4" />
                    Run report
                  </Button>
                )
              }
              description="Choose Run from a saved list report."
              title="No report result"
            />
          )}
        </CardContent>
      </Card>
      {reportExecution ? <PrintDocumentFooter /> : null}
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

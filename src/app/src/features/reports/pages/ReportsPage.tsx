import { type FormEvent, type ReactNode, useEffect, useMemo, useState } from "react";
import { ArrowDown, ArrowUp, ChevronLeft, ChevronRight, Download, Edit3, Eye, FileDown, FileText, ListFilter, Play, Plus, Printer, RefreshCw, Save, Search, Trash2, X } from "lucide-react";
import { Link } from "react-router-dom";
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
import { deleteRecord, listForms, type FormDetail } from "../../forms/api";
import { getFormStatusLabel, type FormSummary } from "../../forms/drafts";
import { PrintDocumentFooter, PrintDocumentHeader } from "../../printing/components/PrintDocument";
import { PrintTemplateDocument } from "../../printing/components/PrintTemplateDocument";
import { downloadReportPrintTemplatePdf, getPrintTemplate, getPrintTemplateVersion, listPrintTemplates } from "../../printing/api";
import { getGeneratedAtPrintMetadata, requestBrowserPrint } from "../../printing/printLayout";
import { getPrintTemplatePdfButtonLabel, resolvePrintTemplateRenderTarget } from "../../printing/templateRenderer";
import type { PrintTemplateRenderDetail, PrintTemplateSummary, ReportTemplateExecution } from "../../printing/types";
import { getRecordCreatePath, getRecordDetailPath, getRecordEditPath } from "../../records/recordEditor";
import { createListReportConfig, getReportFieldOptions } from "../builder";
import { createListReport, downloadListReportCsv, executeListReport, listReports, type ListReportRuntimeOptions } from "../api";
import { getReportTablePrintDescription } from "../reportPrint";
import { loadReportWorkspace } from "../workspace";
import {
  type ListReportExecution,
  type ExecuteListReportOptions,
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
  const [executedReportRuntimeOptions, setExecutedReportRuntimeOptions] = useState<ListReportRuntimeOptions>({});
  const [reportPage, setReportPage] = useState(1);
  const [reportSortFieldId, setReportSortFieldId] = useState<string | undefined>();
  const [reportSortDirection, setReportSortDirection] = useState<ReportSortDirection>("asc");
  const [reportColumnFilters, setReportColumnFilters] = useState<Record<string, string>>({});
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
  const [deletingReportRecordId, setDeletingReportRecordId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [viewerError, setViewerError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [reportNameError, setReportNameError] = useState<string | undefined>();
  const [reportPrintTemplates, setReportPrintTemplates] = useState<PrintTemplateSummary[]>([]);
  const [selectedPrintTemplateId, setSelectedPrintTemplateId] = useState("");
  const [selectedPrintTemplate, setSelectedPrintTemplate] = useState<PrintTemplateRenderDetail | null>(null);
  const [printTemplateLoading, setPrintTemplateLoading] = useState(false);
  const [printTemplateDownloading, setPrintTemplateDownloading] = useState(false);
  const [printTemplateError, setPrintTemplateError] = useState<string | null>(null);

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
    if (!selectedPrintTemplateId) {
      setSelectedPrintTemplate(null);
      return;
    }

    const templateSummary = reportPrintTemplates.find((template) => template.id === selectedPrintTemplateId);

    if (!templateSummary) {
      setSelectedPrintTemplate(null);
      return;
    }

    const renderTarget = resolvePrintTemplateRenderTarget(templateSummary);

    let active = true;
    setPrintTemplateLoading(true);
    setPrintTemplateError(null);

    const request = renderTarget.source === "version"
      ? getPrintTemplateVersion(renderTarget.versionId)
      : getPrintTemplate(renderTarget.templateId);

    request
      .then((template) => {
        if (active) setSelectedPrintTemplate(template);
      })
      .catch((caught: unknown) => {
        if (!active) return;
        setSelectedPrintTemplate(null);
        setPrintTemplateError(getErrorMessage(caught));
      })
      .finally(() => {
        if (active) setPrintTemplateLoading(false);
      });

    return () => {
      active = false;
    };
  }, [reportPrintTemplates, selectedPrintTemplateId]);

  useEffect(() => {
    if (!selectedFormId) {
      setFormDetail(null);
      setReports([]);
      setSelectedReportId("");
      setReportExecution(null);
      setReportPrintTemplates([]);
      setSelectedPrintTemplateId("");
      setSelectedPrintTemplate(null);
      setExecutedReportRuntimeOptions({});
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
    setReportPrintTemplates([]);
    setSelectedPrintTemplateId("");
    setSelectedPrintTemplate(null);
    setPrintTemplateError(null);
    setReportSearch("");
    setExecutedReportSearch("");
    setExecutedReportRuntimeOptions({});
    setReportPage(1);
    setReportSortFieldId(undefined);
    setReportSortDirection("asc");
    setReportColumnFilters({});

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
  const hasReportColumnFilters = Object.values(reportColumnFilters).some((value) => value.trim().length > 0);
  const selectedPrintTemplateSummary = useMemo(
    () => reportPrintTemplates.find((template) => template.id === selectedPrintTemplateId) ?? null,
    [reportPrintTemplates, selectedPrintTemplateId]
  );
  const selectedPrintRenderTarget = selectedPrintTemplateSummary ? resolvePrintTemplateRenderTarget(selectedPrintTemplateSummary) : null;
  const selectedServerPdfVersionId = selectedPrintRenderTarget?.source === "version" ? selectedPrintRenderTarget.versionId : null;
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
        setReportPrintTemplates([]);
        setSelectedPrintTemplateId("");
        setSelectedPrintTemplate(null);
        setExecutedReportSearch("");
        setExecutedReportRuntimeOptions({});
        setReportSortFieldId(undefined);
        setReportSortDirection("asc");
        setReportColumnFilters({});
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

  async function handleRunReport(reportId: string, page: number, overrides: Partial<ExecuteListReportOptions> = {}) {
    if (!selectedFormId || !reportId) {
      return;
    }

    setSelectedReportId(reportId);
    setReportPage(page);
    setRunningReport(true);
    setViewerError(null);
    setPrintTemplateError(null);

    try {
      const search = overrides.search ?? reportSearch;
      const effectiveSortFieldId = overrides.sortFieldId ?? reportSortFieldId;
      const effectiveSortDirection = overrides.sortDirection ?? reportSortDirection;
      const effectiveFilters = overrides.filters ?? reportColumnFilters;
      const execution = await executeListReport(selectedFormId, reportId, {
        page,
        pageSize: reportPageSize,
        search,
        sortFieldId: effectiveSortFieldId,
        sortDirection: effectiveSortDirection,
        filters: effectiveFilters
      });
      const templates = await loadReportPrintTemplates(execution.formId, execution.reportId);
      setReportExecution(execution);
      setReportPrintTemplates(templates);
      setSelectedPrintTemplateId((current) => templates.some((template) => template.id === current) ? current : "");
      setExecutedReportSearch(search);
      setExecutedReportRuntimeOptions({
        search,
        sortFieldId: effectiveSortFieldId,
        sortDirection: effectiveSortDirection,
        filters: effectiveFilters
      });
    } catch (caught) {
      setViewerError(getErrorMessage(caught));
      setReportExecution(null);
      setReportPrintTemplates([]);
      setSelectedPrintTemplateId("");
      setSelectedPrintTemplate(null);
      setExecutedReportRuntimeOptions({});
    } finally {
      setRunningReport(false);
    }
  }

  async function loadReportPrintTemplates(formId: string, reportId: string): Promise<PrintTemplateSummary[]> {
    try {
      return await listPrintTemplates(formId, { type: "report", reportId });
    } catch (caught) {
      setPrintTemplateError(getErrorMessage(caught));
      return [];
    }
  }

  function handleSearchSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    void handleRunReport(selectedReportId, 1);
  }

  function toggleReportSort(fieldId: string) {
    const nextDirection = reportSortFieldId === fieldId && reportSortDirection === "asc" ? "desc" : "asc";
    setReportSortFieldId(fieldId);
    setReportSortDirection(nextDirection);

    if (selectedReportId) {
      void handleRunReport(selectedReportId, 1, { sortFieldId: fieldId, sortDirection: nextDirection });
    }
  }

  function updateReportColumnFilter(fieldId: string, value: string) {
    setReportColumnFilters((current) => ({ ...current, [fieldId]: value }));
  }

  function applyReportColumnFilters() {
    if (!selectedReportId) {
      return;
    }

    void handleRunReport(selectedReportId, 1);
  }

  function clearReportColumnFilters() {
    setReportColumnFilters({});

    if (selectedReportId) {
      void handleRunReport(selectedReportId, 1, { filters: {} });
    }
  }

  async function handleDeleteReportRecord(recordId: string) {
    if (!selectedReportId || !reportExecution) {
      return;
    }

    if (!window.confirm("Delete this record?")) {
      return;
    }

    const targetPage = reportExecution.rows.length === 1 && reportExecution.page > 1
      ? reportExecution.page - 1
      : reportExecution.page;

    setDeletingReportRecordId(recordId);
    setViewerError(null);

    try {
      await deleteRecord(recordId);
      await handleRunReport(selectedReportId, targetPage);
    } catch (caught) {
      setViewerError(getErrorMessage(caught));
    } finally {
      setDeletingReportRecordId(null);
    }
  }

  async function handlePrintAction() {
    if (!reportExecution) return;

    if (!selectedServerPdfVersionId) {
      requestBrowserPrint();
      return;
    }

    setPrintTemplateDownloading(true);
    setPrintTemplateError(null);

    try {
      const pdf = await downloadReportPrintTemplatePdf(
        selectedServerPdfVersionId,
        reportExecution.reportId,
        {
          page: reportExecution.page,
          pageSize: reportExecution.pageSize,
          ...executedReportRuntimeOptions
        }
      );
      downloadBlob(pdf, `${slugify(reportExecution.reportName)}.pdf`);
    } catch (caught) {
      setPrintTemplateError(getErrorMessage(caught));
    } finally {
      setPrintTemplateDownloading(false);
    }
  }

  return (
    <div className="grid gap-6 print-area">
      {reportExecution ? (
        selectedPrintTemplate ? (
          <PrintTemplateDocument
            metadata={[reportExecution.formName, `${reportExecution.totalCount} rows`]}
            report={toReportTemplateExecution(reportExecution)}
            template={selectedPrintTemplate}
          />
        ) : (
          <PrintDocumentHeader
            description={reportPrintDescription}
            eyebrow="Report table"
            metadata={[reportExecution.formName, getGeneratedAtPrintMetadata()]}
            title={reportExecution.reportName}
          />
        )
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
      {printTemplateError ? (
        <div data-print-hide="true">
          <Alert title="Print template">{printTemplateError}</Alert>
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

      <Card className="print-card" data-print-hide={selectedPrintTemplate ? "true" : undefined}>
        <CardHeader data-print-hide="true">
          <div className="flex flex-wrap items-start justify-between gap-3">
            <div>
              <CardTitle>Report viewer</CardTitle>
              <CardDescription>{selectedReport ? selectedReport.name : "Run a saved list report."}</CardDescription>
            </div>
            <div className="flex flex-wrap gap-2">
              <Badge>{reportExecution ? `${reportExecution.totalCount} rows` : "Not run"}</Badge>
              {selectedFormId ? (
                <ReportActionLink to={getRecordCreatePath(selectedFormId)}>
                  <Plus className="size-4" />
                  New record
                </ReportActionLink>
              ) : null}
              {reportExecution && reportPrintTemplates.length > 0 ? (
                <Select
                  aria-label="Print template"
                  className="min-w-44"
                  disabled={runningReport || printTemplateLoading}
                  onChange={(event) => setSelectedPrintTemplateId(event.target.value)}
                  value={selectedPrintTemplateId}
                >
                  <option value="">Default layout</option>
                  {reportPrintTemplates.map((template) => (
                    <option key={template.id} value={template.id}>
                      {template.name}
                    </option>
                  ))}
                </Select>
              ) : null}
              <Button
                disabled={!reportExecution || runningReport || printTemplateLoading || printTemplateDownloading || Boolean(selectedPrintTemplateId && !selectedPrintTemplate)}
                onClick={() => void handlePrintAction()}
                variant="outline"
              >
                {selectedPrintTemplate ? <FileDown className="size-4" /> : <Printer className="size-4" />}
                {selectedServerPdfVersionId ? "Download PDF" : selectedPrintTemplate ? getPrintTemplatePdfButtonLabel(selectedPrintTemplate.config) : "Print"}
              </Button>
              <Button
                disabled={!reportExecution || runningReport}
                onClick={() => {
                  if (reportExecution) {
                    downloadListReportCsv(reportExecution.formId, reportExecution.reportId, executedReportRuntimeOptions);
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
              <Button disabled={!selectedReportId || runningReport} onClick={applyReportColumnFilters} variant="outline">
                <ListFilter className="size-4" />
                Apply filters
              </Button>
              <Button disabled={!selectedReportId || runningReport || !hasReportColumnFilters} onClick={clearReportColumnFilters} variant="outline">
                <X className="size-4" />
                Clear filters
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
              <ReportExecutionTable
                columnFilters={reportColumnFilters}
                execution={reportExecution}
                onApplyFilters={applyReportColumnFilters}
                onDeleteRecord={(recordId) => void handleDeleteReportRecord(recordId)}
                onFilterChange={updateReportColumnFilter}
                onSort={toggleReportSort}
                deletingRecordId={deletingReportRecordId}
                running={runningReport}
                sortDirection={reportSortDirection}
                sortFieldId={reportSortFieldId}
              />
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
      {reportExecution && !selectedPrintTemplate ? <PrintDocumentFooter /> : null}
    </div>
  );
}

function ReportExecutionTable({
  columnFilters,
  deletingRecordId,
  execution,
  onApplyFilters,
  onDeleteRecord,
  onFilterChange,
  onSort,
  running,
  sortDirection,
  sortFieldId
}: {
  columnFilters: Record<string, string>;
  deletingRecordId: string | null;
  execution: ListReportExecution;
  onApplyFilters: () => void;
  onDeleteRecord: (recordId: string) => void;
  onFilterChange: (fieldId: string, value: string) => void;
  onSort: (fieldId: string) => void;
  running: boolean;
  sortDirection: ReportSortDirection;
  sortFieldId?: string;
}) {
  return (
    <div className="overflow-hidden rounded-xl border border-border bg-card/80">
      <div className="overflow-x-auto">
        <table className="w-full min-w-[44rem] text-left text-sm">
          <thead className="bg-muted/60 text-xs font-bold uppercase tracking-normal text-muted-foreground">
            <tr>
              {execution.columns.map((column) => (
                <th className="px-4 py-3" key={column.fieldId} style={column.width ? { width: column.width } : undefined}>
                  <button
                    aria-label={`Sort by ${column.label}`}
                    className="inline-flex items-center gap-1 font-bold text-muted-foreground transition hover:text-foreground disabled:cursor-not-allowed disabled:opacity-60"
                    disabled={running}
                    onClick={() => onSort(column.fieldId)}
                    type="button"
                  >
                    {column.label}
                    {sortFieldId === column.fieldId ? (
                      sortDirection === "asc" ? <ArrowUp className="size-3.5" /> : <ArrowDown className="size-3.5" />
                    ) : null}
                  </button>
                </th>
              ))}
              <th className="px-4 py-3" data-print-hide="true">
                Actions
              </th>
            </tr>
            <tr>
              {execution.columns.map((column) => (
                <th className="px-4 pb-3" key={`${column.fieldId}-filter`}>
                  <input
                    aria-label={`Filter ${column.label}`}
                    className="h-8 w-full rounded-lg border border-border bg-card px-2 text-xs font-semibold text-foreground outline-none transition placeholder:text-muted-foreground/70 focus:ring-4 focus:ring-primary/20 disabled:cursor-not-allowed disabled:opacity-60"
                    disabled={running}
                    onChange={(event) => onFilterChange(column.fieldId, event.target.value)}
                    onKeyDown={(event) => {
                      if (event.key === "Enter") {
                        event.preventDefault();
                        onApplyFilters();
                      }
                    }}
                    placeholder={`Filter ${column.label}`}
                    value={columnFilters[column.fieldId] ?? ""}
                  />
                </th>
              ))}
              <th className="px-4 pb-3" data-print-hide="true" />
            </tr>
          </thead>
          <tbody>
            {execution.rows.map((row) => (
              <tr className="border-t border-border" key={row.recordId}>
                {execution.columns.map((column) => {
                  const value = row.cells[column.fieldId]?.displayValue?.trim();

                  return (
                    <td className="px-4 py-3 text-sm text-foreground" key={column.fieldId}>
                      {value ? value : <span className="text-muted-foreground">-</span>}
                    </td>
                  );
                })}
                <td className="px-4 py-3" data-print-hide="true">
                  <div className="flex flex-wrap gap-2">
                    <ReportActionLink to={getRecordDetailPath(row.recordId)}>
                      <Eye className="size-4" />
                      View
                    </ReportActionLink>
                    <ReportActionLink to={getRecordEditPath(row.recordId)}>
                      <Edit3 className="size-4" />
                      Edit
                    </ReportActionLink>
                    <Button
                      disabled={running || deletingRecordId === row.recordId}
                      onClick={() => onDeleteRecord(row.recordId)}
                      size="sm"
                      variant="danger"
                    >
                      <Trash2 className="size-4" />
                      {deletingRecordId === row.recordId ? "Deleting..." : "Delete"}
                    </Button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

function ReportActionLink({ children, to }: { children: ReactNode; to: string }) {
  return (
    <Link
      className="control-transition inline-flex min-h-8 items-center justify-center gap-2 rounded-xl border border-border bg-card/90 px-3 text-sm font-bold text-foreground hover:bg-muted"
      to={to}
    >
      {children}
    </Link>
  );
}

function toReportTemplateExecution(execution: ListReportExecution): ReportTemplateExecution {
  return {
    columns: execution.columns.map((column) => ({
      fieldId: column.fieldId,
      label: column.label
    })),
    rows: execution.rows.map((row) => ({
      id: row.recordId,
      cells: Object.fromEntries(
        Object.entries(row.cells).flatMap(([fieldId, cell]) => cell ? [[fieldId, { displayValue: cell.displayValue }]] : [])
      )
    }))
  };
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

function downloadBlob(blob: Blob, fileName: string) {
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = fileName;
  document.body.append(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
}

function slugify(value: string): string {
  const slug = value
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "");

  return slug || "report";
}

function getErrorMessage(error: unknown): string {
  return error instanceof Error ? error.message : "Reports request failed.";
}

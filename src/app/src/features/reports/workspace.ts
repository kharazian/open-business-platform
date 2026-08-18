import { getForm, type FormDetail } from "../forms/api";
import { listReportFields, listReports } from "./api";
import type { ListReportSummary, ReportFieldCatalogItem } from "./types";

export type ReportWorkspace = {
  formDetail: FormDetail | null;
  reports: ListReportSummary[];
  fieldOptions: ReportFieldCatalogItem[];
};

export type ReportWorkspaceLoaders = {
  getForm?: typeof getForm;
  listReports?: typeof listReports;
  listReportFields?: typeof listReportFields;
};

export async function loadReportWorkspace(
  formId: string,
  loaders: ReportWorkspaceLoaders = {}
): Promise<ReportWorkspace> {
  const getFormDetail = loaders.getForm ?? getForm;
  const getReports = loaders.listReports ?? listReports;
  const getFields = loaders.listReportFields ?? listReportFields;
  const [formResult, reportsResult, fieldsResult] = await Promise.allSettled([
    getFormDetail(formId),
    getReports(formId),
    getFields(formId)
  ]);

  if (reportsResult.status === "rejected") {
    throw reportsResult.reason;
  }
  if (fieldsResult.status === "rejected") {
    throw fieldsResult.reason;
  }

  return {
    formDetail: formResult.status === "fulfilled" ? formResult.value : null,
    reports: reportsResult.value,
    fieldOptions: fieldsResult.value
  };
}

import { getForm, type FormDetail } from "../forms/api";
import { listReports } from "./api";
import type { ListReportSummary } from "./types";

export type ReportWorkspace = {
  formDetail: FormDetail | null;
  reports: ListReportSummary[];
};

export type ReportWorkspaceLoaders = {
  getForm?: typeof getForm;
  listReports?: typeof listReports;
};

export async function loadReportWorkspace(
  formId: string,
  loaders: ReportWorkspaceLoaders = {}
): Promise<ReportWorkspace> {
  const getFormDetail = loaders.getForm ?? getForm;
  const getReports = loaders.listReports ?? listReports;
  const [formResult, reportsResult] = await Promise.allSettled([
    getFormDetail(formId),
    getReports(formId)
  ]);

  if (reportsResult.status === "rejected") {
    throw reportsResult.reason;
  }

  return {
    formDetail: formResult.status === "fulfilled" ? formResult.value : null,
    reports: reportsResult.value
  };
}

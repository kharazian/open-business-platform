import type { FormRecordDetail, UpdateRecordRequest } from "../forms/api";
import type { FormRecordValues } from "../forms/types";

export function createRecordEditDraft(record: FormRecordDetail): FormRecordValues {
  return { ...record.values };
}

export function createUpdateRecordRequest(record: FormRecordDetail, values: FormRecordValues): UpdateRecordRequest {
  return {
    values,
    concurrencyStamp: record.concurrencyStamp
  };
}

export function getRecordListPath(record: FormRecordDetail): string {
  return `/forms/${record.formId}/records`;
}

export function getRecordDetailPath(recordId: string): string {
  return `/records/${encodeURIComponent(recordId)}`;
}

export function getRecordEditPath(recordId: string): string {
  return `${getRecordDetailPath(recordId)}?mode=edit`;
}

export function getRecordCreatePath(formId: string): string {
  return `/forms/${encodeURIComponent(formId)}/submit`;
}

export function isRecordEditMode(searchParams: URLSearchParams): boolean {
  return searchParams.get("mode") === "edit";
}

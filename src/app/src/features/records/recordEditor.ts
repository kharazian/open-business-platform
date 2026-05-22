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

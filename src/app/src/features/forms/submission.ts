import type { FormRecord, PublishedFormForSubmission } from "./api";
import { createInitialRecordValues } from "./renderer";
import type { FormRecordValues, ValidationError } from "./types";

export function createPublishedFormSubmissionValues(form: PublishedFormForSubmission): FormRecordValues {
  return createInitialRecordValues(form.schema);
}

export function clearSubmissionFieldErrors(errors: ValidationError[], fieldId: string): ValidationError[] {
  return errors.filter((validationError) => validationError.path !== `values.${fieldId}`);
}

export function getSubmissionSuccessLinks(record: FormRecord) {
  return {
    recordPath: `/records/${record.id}`,
    recordsPath: `/forms/${record.formId}/records`,
    formsPath: "/forms"
  };
}

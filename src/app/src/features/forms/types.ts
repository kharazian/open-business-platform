export const formFieldTypes = [
  "text",
  "textarea",
  "number",
  "email",
  "phone",
  "date",
  "select",
  "checkbox",
  "radio",
  "recordLookup",
  "fileUpload",
  "currency",
  "percent",
  "rating",
  "url",
  "time",
  "datetime",
  "userPicker",
  "departmentPicker",
  "subTable",
  "address"
] as const;

export type FormFieldType = (typeof formFieldTypes)[number];

export type FormFieldOption = {
  id: string;
  label: string;
  value: string;
};

export type FormFieldValidation = {
  minLength?: number;
  maxLength?: number;
  min?: number;
  max?: number;
  pattern?: string;
};

export const addressSubfields = ["line1", "line2", "city", "region", "postalCode", "country", "latitude", "longitude"] as const;
export type AddressSubfield = (typeof addressSubfields)[number];
export type FormAddressValue = Partial<Record<AddressSubfield, string | number>>;
export type FormRecordValue = string | number | boolean | FormAddressValue | null;

export type FormFieldAddressConfig = {
  requiredSubfields?: AddressSubfield[];
};

export type FormFieldLookupConfig = {
  sourceType: "form_records";
  sourceFormId: string;
  labelFieldIds: string[];
  searchFieldIds: string[];
  filters?: FormFieldLookupFilter[];
};

export type FormFieldLookupFilter = {
  sourceFieldId: string;
  valueFromFieldId: string;
};

export type FormFieldSubTableConfig = {
  sourceType: "child_form_records";
  childFormId: string;
  parentLookupFieldId: string;
  displayColumnFieldIds: string[];
  allowInlineCreate?: boolean;
  allowInlineEdit?: boolean;
  allowInlineDelete?: boolean;
  minRows?: number;
  maxRows?: number;
};

export type FormField = {
  id: string;
  type: FormFieldType;
  label: string;
  required?: boolean;
  placeholder?: string;
  helpText?: string;
  defaultValue?: FormRecordValue;
  options?: FormFieldOption[];
  validation?: FormFieldValidation;
  lookup?: FormFieldLookupConfig;
  subTable?: FormFieldSubTableConfig;
  address?: FormFieldAddressConfig;
};

export type ResponsiveSpan = {
  mobile: number;
  tablet: number;
  desktop: number;
};

export type FormLayoutColumn = {
  id: string;
  span: ResponsiveSpan;
  fields: string[];
};

export type FormLayoutRow = {
  id: string;
  columns: FormLayoutColumn[];
};

export type FormLayoutSection = {
  id: string;
  title?: string;
  description?: string;
  rows: FormLayoutRow[];
};

export type FormLayoutPage = {
  id: string;
  title?: string;
  description?: string;
  sections: FormLayoutSection[];
};

export type FormLayout = {
  pages: FormLayoutPage[];
};

export type FormSchema = {
  schemaVersion: 1;
  fields: FormField[];
  layout: FormLayout;
};

export type FormVersion = {
  id: string;
  formId: string;
  versionNumber: number;
  schema: FormSchema;
  publishedBy?: string;
  publishedAt: string;
};

export type FormRecordValues = Record<string, FormRecordValue>;

export type ValidationError = {
  path: string;
  code: string;
  message: string;
};

export type ValidationResult = {
  valid: boolean;
  errors: ValidationError[];
};

export function isFormFieldType(value: string): value is FormFieldType {
  return (formFieldTypes as readonly string[]).includes(value);
}

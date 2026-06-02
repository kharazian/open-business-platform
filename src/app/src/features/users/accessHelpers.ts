import type {
  FieldAccessLevel,
  FormAccessAction,
  RecordAccessScope,
  ReportAccessAction,
  RoleFieldPermissionDto,
  RoleFormPermissionDto,
  RoleReportPermissionDto
} from "./types";

export function normalizeScopedFormPermissions(
  permissions: Array<Omit<RoleFormPermissionDto, "scope"> & { scope?: string }>
): RoleFormPermissionDto[] {
  return permissions
    .filter((permission) => permission.formId && permission.action)
    .map((permission) => ({
      formId: permission.formId,
      action: permission.action as FormAccessAction,
      scope: (permission.scope || "all") as RecordAccessScope
    }))
    .sort(
      (left, right) =>
        left.formId.localeCompare(right.formId)
        || left.action.localeCompare(right.action)
        || left.scope.localeCompare(right.scope)
    );
}

export function toggleReportPermission(
  permissions: RoleReportPermissionDto[],
  reportId: string,
  action: ReportAccessAction
): RoleReportPermissionDto[] {
  const exists = permissions.some((permission) => permission.reportId === reportId && permission.action === action);

  return exists
    ? permissions.filter((permission) => permission.reportId !== reportId || permission.action !== action)
    : [...permissions, { reportId, action }].sort(
        (left, right) => left.reportId.localeCompare(right.reportId) || left.action.localeCompare(right.action)
      );
}

export function toggleFieldPermission(
  permissions: RoleFieldPermissionDto[],
  formId: string,
  fieldId: string,
  access: FieldAccessLevel
): RoleFieldPermissionDto[] {
  const withoutField = permissions.filter((permission) => permission.formId !== formId || permission.fieldId !== fieldId);
  const current = permissions.find((permission) => permission.formId === formId && permission.fieldId === fieldId);

  if (current?.access === access) {
    return withoutField;
  }

  return [...withoutField, { formId, fieldId, access }].sort(
    (left, right) => left.formId.localeCompare(right.formId) || left.fieldId.localeCompare(right.fieldId)
  );
}

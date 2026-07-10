import type {
  FormAccessAction,
  ReportAccessAction,
  RoleFormPermissionDto,
  RolePermissionsDto,
  RoleReportPermissionDto
} from "../users/types";

export const reportAccessMenuPermission = "menu.reports";
export const reportAccessPlatformManagePermission = "reports.manage";
export const reportAccessFormActions = ["view", "export", "manage"] as const;

export const reportAccessActionLabels: Record<ReportAccessAction, string> = {
  view: "View",
  export: "Export",
  manage: "Manage"
};

export const reportAccessFormActionLabels: Record<(typeof reportAccessFormActions)[number], string> = {
  view: "View source records",
  export: "Export source records",
  manage: "Manage source form"
};

export function setGlobalPermission(permissions: string[], permission: string, enabled: boolean): string[] {
  const next = new Set(permissions);

  if (enabled) {
    next.add(permission);
  } else {
    next.delete(permission);
  }

  return [...next].sort();
}

export function hasReportAccessPermission(
  permissions: RoleReportPermissionDto[],
  reportId: string,
  action: ReportAccessAction
): boolean {
  return permissions.some((permission) => permission.reportId === reportId && permission.action === action);
}

export function hasFormAccessPermission(
  permissions: RoleFormPermissionDto[],
  formId: string,
  action: FormAccessAction
): boolean {
  return permissions.some((permission) => permission.formId === formId && permission.action === action);
}

export function setFormAccessPermission(
  permissions: RoleFormPermissionDto[],
  formId: string,
  action: FormAccessAction,
  enabled: boolean
): RoleFormPermissionDto[] {
  const withoutPermission = permissions.filter((permission) => permission.formId !== formId || permission.action !== action);

  if (!enabled) {
    return withoutPermission;
  }

  const existing = permissions.find((permission) => permission.formId === formId && permission.action === action);

  return [
    ...withoutPermission,
    {
      formId,
      action,
      scope: existing?.scope ?? "all"
    }
  ].sort((left, right) => left.formId.localeCompare(right.formId) || left.action.localeCompare(right.action));
}

export function setReportAccessPermission(
  permissions: RoleReportPermissionDto[],
  reportId: string,
  action: ReportAccessAction,
  enabled: boolean
): RoleReportPermissionDto[] {
  const withoutPermission = permissions.filter((permission) => permission.reportId !== reportId || permission.action !== action);

  if (!enabled) {
    return withoutPermission;
  }

  return [...withoutPermission, { reportId, action }].sort(
    (left, right) => left.reportId.localeCompare(right.reportId) || left.action.localeCompare(right.action)
  );
}

export function grantReportAccessBundle(
  draft: RolePermissionsDto,
  formId: string,
  reportId: string,
  action: ReportAccessAction,
  enabled: boolean
): RolePermissionsDto {
  if (!enabled) {
    return {
      ...draft,
      reportPermissions: setReportAccessPermission(draft.reportPermissions, reportId, action, false)
    };
  }

  let permissions = setGlobalPermission(draft.permissions, reportAccessMenuPermission, true);
  let formPermissions = draft.formPermissions;

  if (action === "view") {
    formPermissions = setFormAccessPermission(formPermissions, formId, "view", true);
  }

  if (action === "export") {
    formPermissions = setFormAccessPermission(formPermissions, formId, "view", true);
    formPermissions = setFormAccessPermission(formPermissions, formId, "export", true);
  }

  if (action === "manage") {
    permissions = setGlobalPermission(permissions, reportAccessPlatformManagePermission, true);
    formPermissions = setFormAccessPermission(formPermissions, formId, "manage", true);
  }

  return {
    ...draft,
    permissions,
    formPermissions,
    reportPermissions: setReportAccessPermission(
      action === "export"
        ? setReportAccessPermission(draft.reportPermissions, reportId, "view", true)
        : draft.reportPermissions,
      reportId,
      action,
      true
    )
  };
}

export function rolePermissionDraftChanged(original: RolePermissionsDto, current: RolePermissionsDto): boolean {
  return (
    sortedStrings(original.permissions).join("|") !== sortedStrings(current.permissions).join("|")
    || reportPermissionKey(original.reportPermissions) !== reportPermissionKey(current.reportPermissions)
    || JSON.stringify(original.formPermissions) !== JSON.stringify(current.formPermissions)
    || JSON.stringify(original.fieldPermissions) !== JSON.stringify(current.fieldPermissions)
  );
}

function sortedStrings(values: string[]): string[] {
  return [...values].sort();
}

function reportPermissionKey(permissions: RoleReportPermissionDto[]): string {
  return permissions
    .map((permission) => `${permission.reportId}:${permission.action}`)
    .sort()
    .join("|");
}

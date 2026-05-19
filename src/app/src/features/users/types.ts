import type { ActiveStateDto, AuditedEntityDto, ConcurrencyStampedDto, EntityId } from "../../types/entities";

export const userStatuses = ["active", "inactive"] as const;
export const roleStatuses = ["active", "inactive"] as const;
export const departmentStatuses = ["active", "inactive"] as const;
export const formAccessActions = ["submit", "view", "edit", "delete", "manage"] as const;
export const accessManagementTabs = [
  { label: "Users", value: "users" },
  { label: "Roles & permissions", value: "roles" }
] as const;

export const menuPermissionOptions = [
  { label: "Dashboard", value: "menu.dashboard" },
  { label: "Forms", value: "menu.forms" },
  { label: "Reports", value: "menu.reports" },
  { label: "Users & Access", value: "menu.users_access" },
  { label: "Settings", value: "menu.settings" },
  { label: "Profile", value: "menu.profile" }
] as const;

export const platformPermissionOptions = [
  { label: "Manage users", value: "users.manage" },
  { label: "Manage roles", value: "roles.manage" },
  { label: "Create forms", value: "forms.create" },
  { label: "Manage all forms", value: "forms.manage_all" }
] as const;

export type UserStatus = (typeof userStatuses)[number];
export type RoleStatus = (typeof roleStatuses)[number];
export type DepartmentStatus = (typeof departmentStatuses)[number];
export type FormAccessAction = (typeof formAccessActions)[number];

export interface UserRoleDto {
  id: EntityId;
  name: string;
}

export interface UserDepartmentDto {
  id: EntityId;
  name: string;
  isPrimary: boolean;
}

export interface UserDto extends AuditedEntityDto, ActiveStateDto, ConcurrencyStampedDto {
  name: string;
  email: string;
  externalProvider?: string | null;
  externalUserId?: string | null;
  roles: UserRoleDto[];
  departments: UserDepartmentDto[];
}

export interface CreateUserRequest {
  name: string;
  email: string;
  password: string;
  roleIds: EntityId[];
  departmentIds: EntityId[];
  isActive?: boolean;
}

export interface ResetUserPasswordRequest {
  newPassword: string;
}

export interface UpdateUserRequest {
  name: string;
  isActive: boolean;
  roleIds: EntityId[];
  departmentIds: EntityId[];
  concurrencyStamp: string;
}

export interface RoleDto extends AuditedEntityDto, ActiveStateDto, ConcurrencyStampedDto {
  name: string;
  description?: string | null;
  userCount: number;
}

export interface CreateRoleRequest {
  name: string;
  description?: string | null;
  isActive?: boolean;
}

export interface UpdateRoleRequest {
  name: string;
  description?: string | null;
  isActive: boolean;
  concurrencyStamp: string;
}

export interface RoleFormPermissionDto {
  formId: EntityId;
  action: FormAccessAction;
}

export interface RolePermissionsDto {
  roleId: EntityId;
  permissions: string[];
  formPermissions: RoleFormPermissionDto[];
}

export interface UpdateRolePermissionsRequest {
  permissions: string[];
  formPermissions: RoleFormPermissionDto[];
}

export interface FormAccessOptionDto {
  id: EntityId;
  name: string;
  status: string;
}

export interface DepartmentDto extends AuditedEntityDto, ActiveStateDto, ConcurrencyStampedDto {
  name: string;
  parentDepartmentId?: EntityId | null;
  managerUserId?: EntityId | null;
  userCount: number;
}

export interface CreateDepartmentRequest {
  name: string;
  parentDepartmentId?: EntityId | null;
  managerUserId?: EntityId | null;
  isActive?: boolean;
}

export interface UpdateDepartmentRequest {
  name: string;
  parentDepartmentId?: EntityId | null;
  managerUserId?: EntityId | null;
  isActive: boolean;
  concurrencyStamp: string;
}

export function isUserStatus(value: string): value is UserStatus {
  return (userStatuses as readonly string[]).includes(value);
}

export function isRoleStatus(value: string): value is RoleStatus {
  return (roleStatuses as readonly string[]).includes(value);
}

export function isDepartmentStatus(value: string): value is DepartmentStatus {
  return (departmentStatuses as readonly string[]).includes(value);
}

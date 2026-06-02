import type {
  CreateRoleRequest,
  CreateDepartmentRequest,
  CreateGroupRequest,
  CreateUserRequest,
  FormAccessOptionDto,
  DepartmentDto,
  GroupDto,
  ResetUserPasswordRequest,
  RoleDto,
  RolePermissionsDto,
  UpdateRolePermissionsRequest,
  UpdateRoleRequest,
  UpdateDepartmentRequest,
  UpdateGroupRequest,
  UpdateUserRequest,
  UserDto
} from "./types";

type ApiFetchResponse = {
  ok: boolean;
  status?: number;
  json: () => Promise<unknown>;
};

export type UsersAccessFetcher = (input: string, init?: RequestInit) => Promise<ApiFetchResponse>;

export class UsersAccessApiError extends Error {
  constructor(message: string) {
    super(message);
    this.name = "UsersAccessApiError";
  }
}

const defaultFetcher: UsersAccessFetcher = (input, init) => fetch(input, init);

export async function listUsers(fetcher: UsersAccessFetcher = defaultFetcher): Promise<UserDto[]> {
  return requestItems<UserDto>("/api/users", { method: "GET", credentials: "include" }, fetcher);
}

export async function createUser(request: CreateUserRequest, fetcher: UsersAccessFetcher = defaultFetcher): Promise<UserDto> {
  return requestJson<UserDto>(
    "/api/users",
    {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request)
    },
    fetcher
  );
}

export async function updateUser(userId: string, request: UpdateUserRequest, fetcher: UsersAccessFetcher = defaultFetcher): Promise<UserDto> {
  return requestJson<UserDto>(
    `/api/users/${userId}`,
    {
      method: "PUT",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request)
    },
    fetcher
  );
}

export async function resetUserPassword(
  userId: string,
  request: ResetUserPasswordRequest,
  fetcher: UsersAccessFetcher = defaultFetcher
): Promise<void> {
  await requestNoContent(
    `/api/users/${userId}/reset-password`,
    {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request)
    },
    fetcher
  );
}

export async function listRoles(fetcher: UsersAccessFetcher = defaultFetcher): Promise<RoleDto[]> {
  return requestItems<RoleDto>("/api/roles", { method: "GET", credentials: "include" }, fetcher);
}

export async function createRole(request: CreateRoleRequest, fetcher: UsersAccessFetcher = defaultFetcher): Promise<RoleDto> {
  return requestJson<RoleDto>(
    "/api/roles",
    {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request)
    },
    fetcher
  );
}

export async function updateRole(roleId: string, request: UpdateRoleRequest, fetcher: UsersAccessFetcher = defaultFetcher): Promise<RoleDto> {
  return requestJson<RoleDto>(
    `/api/roles/${roleId}`,
    {
      method: "PUT",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request)
    },
    fetcher
  );
}

export async function getRolePermissions(roleId: string, fetcher: UsersAccessFetcher = defaultFetcher): Promise<RolePermissionsDto> {
  return requestJson<RolePermissionsDto>(`/api/roles/${roleId}/permissions`, { method: "GET", credentials: "include" }, fetcher);
}

export async function updateRolePermissions(
  roleId: string,
  request: UpdateRolePermissionsRequest,
  fetcher: UsersAccessFetcher = defaultFetcher
): Promise<RolePermissionsDto> {
  return requestJson<RolePermissionsDto>(
    `/api/roles/${roleId}/permissions`,
    {
      method: "PUT",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request)
    },
    fetcher
  );
}

export async function listFormAccessOptions(fetcher: UsersAccessFetcher = defaultFetcher): Promise<FormAccessOptionDto[]> {
  return requestItems<FormAccessOptionDto>("/api/forms/access-options", { method: "GET", credentials: "include" }, fetcher);
}

export async function listDepartments(fetcher: UsersAccessFetcher = defaultFetcher): Promise<DepartmentDto[]> {
  return requestItems<DepartmentDto>("/api/departments", { method: "GET", credentials: "include" }, fetcher);
}

export async function createDepartment(request: CreateDepartmentRequest, fetcher: UsersAccessFetcher = defaultFetcher): Promise<DepartmentDto> {
  return requestJson<DepartmentDto>(
    "/api/departments",
    {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request)
    },
    fetcher
  );
}

export async function updateDepartment(
  departmentId: string,
  request: UpdateDepartmentRequest,
  fetcher: UsersAccessFetcher = defaultFetcher
): Promise<DepartmentDto> {
  return requestJson<DepartmentDto>(
    `/api/departments/${departmentId}`,
    {
      method: "PUT",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request)
    },
    fetcher
  );
}

export async function listGroups(fetcher: UsersAccessFetcher = defaultFetcher): Promise<GroupDto[]> {
  return requestItems<GroupDto>("/api/groups", { method: "GET", credentials: "include" }, fetcher);
}

export async function createGroup(request: CreateGroupRequest, fetcher: UsersAccessFetcher = defaultFetcher): Promise<GroupDto> {
  return requestJson<GroupDto>(
    "/api/groups",
    {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request)
    },
    fetcher
  );
}

export async function updateGroup(groupId: string, request: UpdateGroupRequest, fetcher: UsersAccessFetcher = defaultFetcher): Promise<GroupDto> {
  return requestJson<GroupDto>(
    `/api/groups/${groupId}`,
    {
      method: "PUT",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request)
    },
    fetcher
  );
}

async function requestItems<T>(input: string, init: RequestInit, fetcher: UsersAccessFetcher): Promise<T[]> {
  const body = await requestJson<unknown>(input, init, fetcher);

  if (!isRecord(body) || !Array.isArray(body.items)) {
    throw new UsersAccessApiError("API response did not include an items collection.");
  }

  return body.items as T[];
}

async function requestNoContent(input: string, init: RequestInit, fetcher: UsersAccessFetcher): Promise<void> {
  const response = await fetcher(input, init);

  if (!response.ok) {
    throw new UsersAccessApiError(await getErrorMessage(response));
  }
}

async function requestJson<T>(input: string, init: RequestInit, fetcher: UsersAccessFetcher): Promise<T> {
  const response = await fetcher(input, init);
  const body = await readJson(response);

  if (!response.ok) {
    throw new UsersAccessApiError(getErrorMessageFromBody(body));
  }

  return body as T;
}

async function readJson(response: ApiFetchResponse): Promise<unknown> {
  try {
    return await response.json();
  } catch {
    return null;
  }
}

async function getErrorMessage(response: ApiFetchResponse): Promise<string> {
  return getErrorMessageFromBody(await readJson(response));
}

function getErrorMessageFromBody(body: unknown): string {
  if (isRecord(body) && typeof body.message === "string" && body.message.trim().length > 0) {
    return body.message;
  }

  return "Users and access request failed.";
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

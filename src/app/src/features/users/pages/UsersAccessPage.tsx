import { type FormEvent, type ReactNode, useEffect, useMemo, useState } from "react";
import { KeyRound, Plus, RefreshCw, Save, ShieldCheck, UserCog } from "lucide-react";
import { Alert } from "../../../components/ui/Alert";
import { Badge } from "../../../components/ui/Badge";
import { Button } from "../../../components/ui/Button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "../../../components/ui/Card";
import { Checkbox } from "../../../components/ui/Checkbox";
import { EmptyState } from "../../../components/ui/EmptyState";
import { Input } from "../../../components/ui/Input";
import { Modal } from "../../../components/ui/Modal";
import { PageHeader } from "../../../components/ui/PageHeader";
import { Select } from "../../../components/ui/Select";
import { Table, type TableColumn } from "../../../components/ui/Table";
import { Tabs } from "../../../components/ui/Tabs";
import { Textarea } from "../../../components/ui/Textarea";
import { cn } from "../../../lib/cn";
import {
  createRole,
  createUser,
  getRolePermissions,
  listFormAccessOptions,
  listRoles,
  listUsers,
  resetUserPassword,
  updateRole,
  updateRolePermissions,
  updateUser
} from "../api";
import {
  accessManagementTabs,
  formAccessActions,
  menuPermissionOptions,
  platformPermissionOptions,
  type FormAccessAction,
  type FormAccessOptionDto,
  type RoleDto,
  type RoleFormPermissionDto,
  type RolePermissionsDto,
  type UserDto
} from "../types";
import {
  minimumPasswordLength,
  validateResetPassword,
  validateRoleDraft,
  validateUserDraft,
  type RoleDraftValidationErrors,
  type UserDraftValidationErrors
} from "../validation";

type AccessTab = "users" | "roles";

type UserDraft = {
  name: string;
  email: string;
  password: string;
  isActive: boolean;
  roleIds: string[];
};

type RoleDraft = {
  name: string;
  description: string;
  isActive: boolean;
};

const emptyUserDraft: UserDraft = {
  name: "",
  email: "",
  password: "",
  isActive: true,
  roleIds: []
};

const emptyRoleDraft: RoleDraft = {
  name: "",
  description: "",
  isActive: true
};

const actionLabels: Record<FormAccessAction, string> = {
  submit: "Submit",
  view: "View",
  edit: "Edit",
  delete: "Delete",
  manage: "Manage"
};

export function UsersAccessPage() {
  const [activeTab, setActiveTab] = useState<AccessTab>("users");
  const [users, setUsers] = useState<UserDto[]>([]);
  const [roles, setRoles] = useState<RoleDto[]>([]);
  const [forms, setForms] = useState<FormAccessOptionDto[]>([]);
  const [selectedRoleId, setSelectedRoleId] = useState("");
  const [selectedRolePermissions, setSelectedRolePermissions] = useState<RolePermissionsDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [savingPermissions, setSavingPermissions] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [createUserOpen, setCreateUserOpen] = useState(false);
  const [createRoleOpen, setCreateRoleOpen] = useState(false);
  const [editingUser, setEditingUser] = useState<UserDto | null>(null);
  const [editingRole, setEditingRole] = useState<RoleDto | null>(null);
  const [resetUser, setResetUser] = useState<UserDto | null>(null);
  const [userDraft, setUserDraft] = useState<UserDraft>(emptyUserDraft);
  const [roleDraft, setRoleDraft] = useState<RoleDraft>(emptyRoleDraft);
  const [resetPasswordValue, setResetPasswordValue] = useState("");
  const [userFormErrors, setUserFormErrors] = useState<UserDraftValidationErrors>({});
  const [roleFormErrors, setRoleFormErrors] = useState<RoleDraftValidationErrors>({});
  const [resetPasswordError, setResetPasswordError] = useState<string | undefined>();
  const [modalError, setModalError] = useState<string | null>(null);

  useEffect(() => {
    void refreshWorkspace();
  }, []);

  useEffect(() => {
    if (!selectedRoleId) {
      setSelectedRolePermissions(null);
      return;
    }

    let active = true;
    getRolePermissions(selectedRoleId)
      .then((permissions) => {
        if (active) setSelectedRolePermissions(permissions);
      })
      .catch((caught: unknown) => {
        if (active) setError(getErrorMessage(caught));
      });

    return () => {
      active = false;
    };
  }, [selectedRoleId]);

  const selectedRole = useMemo(
    () => roles.find((role) => role.id === selectedRoleId) ?? null,
    [roles, selectedRoleId]
  );

  const userColumns: Array<TableColumn<UserDto>> = [
    {
      header: "User",
      render: (user) => (
        <div>
          <p className="font-bold text-foreground">{user.name}</p>
          <p className="mt-1 text-sm text-muted-foreground">{user.email}</p>
        </div>
      )
    },
    {
      header: "Roles",
      render: (user) =>
        user.roles.length > 0 ? (
          <div className="flex flex-wrap gap-2">
            {user.roles.map((role) => (
              <Badge key={role.id}>{role.name}</Badge>
            ))}
          </div>
        ) : (
          <span className="text-sm text-muted-foreground">No roles</span>
        )
    },
    {
      header: "Status",
      render: (user) => <Badge variant={user.isActive ? "success" : "danger"}>{user.isActive ? "Active" : "Inactive"}</Badge>
    },
    {
      header: "Actions",
      render: (user) => (
        <div className="flex flex-wrap gap-2">
          <Button size="sm" variant="outline" onClick={() => openEditUser(user)}>
            Edit
          </Button>
          <Button size="sm" variant="outline" onClick={() => openResetPassword(user)}>
            <KeyRound className="size-4" />
            Reset
          </Button>
        </div>
      )
    }
  ];

  async function refreshWorkspace() {
    setLoading(true);
    setError(null);

    try {
      const [nextUsers, nextRoles, nextForms] = await Promise.all([listUsers(), listRoles(), listFormAccessOptions()]);
      setUsers(nextUsers);
      setRoles(nextRoles);
      setForms(nextForms);
      setSelectedRoleId((current) => current || nextRoles[0]?.id || "");
    } catch (caught) {
      setError(getErrorMessage(caught));
    } finally {
      setLoading(false);
    }
  }

  function openCreateUser() {
    setUserDraft(emptyUserDraft);
    clearModalFeedback();
    setCreateUserOpen(true);
  }

  function openEditUser(user: UserDto) {
    clearModalFeedback();
    setEditingUser(user);
    setUserDraft({
      name: user.name,
      email: user.email,
      password: "",
      isActive: user.isActive,
      roleIds: user.roles.map((role) => role.id)
    });
  }

  function openResetPassword(user: UserDto) {
    clearModalFeedback();
    setResetUser(user);
    setResetPasswordValue("");
  }

  function openCreateRole() {
    setRoleDraft(emptyRoleDraft);
    clearModalFeedback();
    setCreateRoleOpen(true);
  }

  function openEditRole(role: RoleDto) {
    clearModalFeedback();
    setEditingRole(role);
    setRoleDraft({
      name: role.name,
      description: role.description ?? "",
      isActive: role.isActive
    });
  }

  async function handleCreateUser(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setModalError(null);

    const validation = validateUserDraft(userDraft, "create");

    if (!validation.valid) {
      setUserFormErrors(validation.errors);
      return;
    }

    setUserFormErrors({});

    try {
      const created = await createUser({ ...validation.value, departmentIds: [] });
      setUsers((current) => [created, ...current]);
      setCreateUserOpen(false);
      setNotice("User created.");
    } catch (caught) {
      setModalError(getErrorMessage(caught));
    }
  }

  async function handleUpdateUser(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!editingUser) return;
    setError(null);
    setModalError(null);

    const validation = validateUserDraft(userDraft, "edit");

    if (!validation.valid) {
      setUserFormErrors(validation.errors);
      return;
    }

    setUserFormErrors({});

    try {
      const updated = await updateUser(editingUser.id, {
        name: validation.value.name,
        isActive: validation.value.isActive,
        roleIds: validation.value.roleIds,
        departmentIds: editingUser.departments.map((department) => department.id),
        concurrencyStamp: editingUser.concurrencyStamp
      });
      setUsers((current) => current.map((user) => (user.id === updated.id ? updated : user)));
      setEditingUser(null);
      setNotice("User updated.");
    } catch (caught) {
      setModalError(getErrorMessage(caught));
    }
  }

  async function handleResetPassword(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!resetUser) return;
    setError(null);
    setModalError(null);

    const validation = validateResetPassword(resetPasswordValue);

    if (!validation.valid) {
      setResetPasswordError(validation.error);
      return;
    }

    setResetPasswordError(undefined);

    try {
      await resetUserPassword(resetUser.id, { newPassword: validation.value });
      setResetUser(null);
      setNotice("Password reset.");
    } catch (caught) {
      setModalError(getErrorMessage(caught));
    }
  }

  async function handleCreateRole(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setModalError(null);

    const validation = validateRoleDraft(roleDraft);

    if (!validation.valid) {
      setRoleFormErrors(validation.errors);
      return;
    }

    setRoleFormErrors({});

    try {
      const created = await createRole({
        name: validation.value.name,
        description: validation.value.description || null,
        isActive: validation.value.isActive
      });
      setRoles((current) => [created, ...current]);
      setSelectedRoleId(created.id);
      setActiveTab("roles");
      setCreateRoleOpen(false);
      setNotice("Role created.");
    } catch (caught) {
      setModalError(getErrorMessage(caught));
    }
  }

  async function handleUpdateRole(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!editingRole) return;
    setError(null);
    setModalError(null);

    const validation = validateRoleDraft(roleDraft);

    if (!validation.valid) {
      setRoleFormErrors(validation.errors);
      return;
    }

    setRoleFormErrors({});

    try {
      const updated = await updateRole(editingRole.id, {
        name: validation.value.name,
        description: validation.value.description || null,
        isActive: validation.value.isActive,
        concurrencyStamp: editingRole.concurrencyStamp
      });
      setRoles((current) => current.map((role) => (role.id === updated.id ? updated : role)));
      setEditingRole(null);
      setNotice("Role updated.");
    } catch (caught) {
      setModalError(getErrorMessage(caught));
    }
  }

  async function handleSavePermissions() {
    if (!selectedRoleId || !selectedRolePermissions) return;
    setSavingPermissions(true);
    setError(null);

    try {
      const updated = await updateRolePermissions(selectedRoleId, {
        permissions: selectedRolePermissions.permissions,
        formPermissions: selectedRolePermissions.formPermissions
      });
      setSelectedRolePermissions(updated);
      setNotice("Role permissions saved.");
    } catch (caught) {
      setError(getErrorMessage(caught));
    } finally {
      setSavingPermissions(false);
    }
  }

  function toggleRoleOnDraft(roleId: string) {
    setUserDraft((current) => ({
      ...current,
      roleIds: current.roleIds.includes(roleId) ? current.roleIds.filter((id) => id !== roleId) : [...current.roleIds, roleId]
    }));
  }

  function toggleGlobalPermission(permission: string) {
    setSelectedRolePermissions((current) => {
      if (!current) return current;

      return {
        ...current,
        permissions: current.permissions.includes(permission)
          ? current.permissions.filter((item) => item !== permission)
          : [...current.permissions, permission].sort()
      };
    });
  }

  function toggleFormPermission(formId: string, action: FormAccessAction) {
    setSelectedRolePermissions((current) => {
      if (!current) return current;

      const exists = current.formPermissions.some((permission) => permission.formId === formId && permission.action === action);
      const formPermissions = exists
        ? current.formPermissions.filter((permission) => permission.formId !== formId || permission.action !== action)
        : [...current.formPermissions, { formId, action }];

      return { ...current, formPermissions };
    });
  }

  function clearModalFeedback() {
    setModalError(null);
    setUserFormErrors({});
    setRoleFormErrors({});
    setResetPasswordError(undefined);
  }

  function clearUserFieldError(field: keyof UserDraftValidationErrors) {
    setModalError(null);
    setUserFormErrors((current) => {
      if (!current[field]) return current;
      const next = { ...current };
      delete next[field];
      return next;
    });
  }

  function clearRoleFieldError(field: keyof RoleDraftValidationErrors) {
    setModalError(null);
    setRoleFormErrors((current) => {
      if (!current[field]) return current;
      const next = { ...current };
      delete next[field];
      return next;
    });
  }

  return (
    <div className="grid gap-6">
      <PageHeader
        eyebrow="Access control"
        title="Users & Access"
        actions={
          <Button onClick={() => void refreshWorkspace()} variant="outline">
            <RefreshCw className="size-4" />
            Refresh
          </Button>
        }
      />

      {error ? <Alert title="Users & Access">{error}</Alert> : null}
      {notice ? (
        <div className="rounded-xl border border-success/40 bg-success/10 px-4 py-3 text-sm font-semibold text-success">
          {notice}
        </div>
      ) : null}

      <Tabs
        active={activeTab}
        onChange={(value) => setActiveTab(value as AccessTab)}
        tabs={accessManagementTabs.map((tab) => ({
          ...tab,
          content: tab.value === "users" ? renderUsersTab() : renderRolesTab()
        }))}
      />

      <Modal
        open={createUserOpen}
        onClose={() => {
          setCreateUserOpen(false);
          clearModalFeedback();
        }}
        title="Create user"
        description="Create a local account and assign one or more roles."
        footer={
          <>
            <Button onClick={() => {
              setCreateUserOpen(false);
              clearModalFeedback();
            }} variant="outline">
              Cancel
            </Button>
            <Button form="create-user" type="submit">
              Create user
            </Button>
          </>
        }
      >
        <div className="grid gap-4">
          {modalError ? <Alert title="Create user">{modalError}</Alert> : null}
          <UserForm
            errors={userFormErrors}
            formId="create-user"
            roles={roles}
            userDraft={userDraft}
            onClearError={clearUserFieldError}
            onSubmit={handleCreateUser}
            onChange={setUserDraft}
            onToggleRole={toggleRoleOnDraft}
            mode="create"
          />
        </div>
      </Modal>

      <Modal
        open={Boolean(editingUser)}
        onClose={() => {
          setEditingUser(null);
          clearModalFeedback();
        }}
        title="Edit user"
        description="Update account status and role assignments."
        footer={
          <>
            <Button onClick={() => {
              setEditingUser(null);
              clearModalFeedback();
            }} variant="outline">
              Cancel
            </Button>
            <Button form="edit-user" type="submit">
              Save user
            </Button>
          </>
        }
      >
        <div className="grid gap-4">
          {modalError ? <Alert title="Edit user">{modalError}</Alert> : null}
          <UserForm
            errors={userFormErrors}
            formId="edit-user"
            roles={roles}
            userDraft={userDraft}
            onClearError={clearUserFieldError}
            onSubmit={handleUpdateUser}
            onChange={setUserDraft}
            onToggleRole={toggleRoleOnDraft}
            mode="edit"
          />
        </div>
      </Modal>

      <Modal
        open={Boolean(resetUser)}
        onClose={() => {
          setResetUser(null);
          clearModalFeedback();
        }}
        title="Reset password"
        description={resetUser ? `Set a new temporary password for ${resetUser.name}.` : undefined}
        footer={
          <>
            <Button onClick={() => {
              setResetUser(null);
              clearModalFeedback();
            }} variant="outline">
              Cancel
            </Button>
            <Button form="reset-password" type="submit">
              Reset password
            </Button>
          </>
        }
      >
        <form className="grid gap-4" id="reset-password" noValidate onSubmit={handleResetPassword}>
          {modalError ? <Alert title="Reset password">{modalError}</Alert> : null}
          <Input
            autoComplete="new-password"
            error={resetPasswordError}
            helperText="At least 8 characters."
            label="New temporary password"
            minLength={minimumPasswordLength}
            onChange={(event) => {
              setResetPasswordValue(event.target.value);
              setModalError(null);
              if (resetPasswordError) setResetPasswordError(undefined);
            }}
            required
            type="password"
            value={resetPasswordValue}
          />
        </form>
      </Modal>

      <Modal
        open={createRoleOpen}
        onClose={() => {
          setCreateRoleOpen(false);
          clearModalFeedback();
        }}
        title="Create role"
        description="Create a reusable role for menu and form access."
        footer={
          <>
            <Button onClick={() => {
              setCreateRoleOpen(false);
              clearModalFeedback();
            }} variant="outline">
              Cancel
            </Button>
            <Button form="create-role" type="submit">
              Create role
            </Button>
          </>
        }
      >
        <div className="grid gap-4">
          {modalError ? <Alert title="Create role">{modalError}</Alert> : null}
          <RoleForm errors={roleFormErrors} formId="create-role" roleDraft={roleDraft} onClearError={clearRoleFieldError} onChange={setRoleDraft} onSubmit={handleCreateRole} />
        </div>
      </Modal>

      <Modal
        open={Boolean(editingRole)}
        onClose={() => {
          setEditingRole(null);
          clearModalFeedback();
        }}
        title="Edit role"
        description="Update role details and active status."
        footer={
          <>
            <Button onClick={() => {
              setEditingRole(null);
              clearModalFeedback();
            }} variant="outline">
              Cancel
            </Button>
            <Button form="edit-role" type="submit">
              Save role
            </Button>
          </>
        }
      >
        <div className="grid gap-4">
          {modalError ? <Alert title="Edit role">{modalError}</Alert> : null}
          <RoleForm errors={roleFormErrors} formId="edit-role" roleDraft={roleDraft} onClearError={clearRoleFieldError} onChange={setRoleDraft} onSubmit={handleUpdateRole} />
        </div>
      </Modal>
    </div>
  );

  function renderUsersTab() {
    return (
      <Card>
        <CardHeader>
          <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
            <div>
              <CardTitle>User directory</CardTitle>
              <CardDescription>Local accounts that can sign in with email and password.</CardDescription>
            </div>
            <Button onClick={openCreateUser}>
              <Plus className="size-4" />
              Create user
            </Button>
          </div>
        </CardHeader>
        <CardContent>
          {loading ? (
            <p className="text-sm font-semibold text-muted-foreground">Loading users...</p>
          ) : users.length > 0 ? (
            <Table columns={userColumns} rows={users} />
          ) : (
            <EmptyState title="No local users" description="Create the first local administrator or role-based user." />
          )}
        </CardContent>
      </Card>
    );
  }

  function renderRolesTab() {
    return (
      <div className="grid gap-4 xl:grid-cols-[20rem_minmax(0,1fr)]">
        <Card className="self-start">
          <CardHeader>
            <div className="flex items-start justify-between gap-3">
              <div>
                <CardTitle>Roles</CardTitle>
                <CardDescription>Choose a role to adjust its access.</CardDescription>
              </div>
              <Button aria-label="Create role" onClick={openCreateRole} size="icon">
                <Plus className="size-4" />
              </Button>
            </div>
          </CardHeader>
          <CardContent className="grid gap-2">
            {loading ? (
              <p className="text-sm font-semibold text-muted-foreground">Loading roles...</p>
            ) : roles.length > 0 ? (
              roles.map((role) => (
                <button
                  className={cn(
                    "rounded-lg border px-3 py-3 text-left transition",
                    selectedRoleId === role.id
                      ? "border-primary bg-primary/10 text-foreground"
                      : "border-border bg-card/70 text-muted-foreground hover:border-primary/40 hover:text-foreground"
                  )}
                  key={role.id}
                  type="button"
                  onClick={() => setSelectedRoleId(role.id)}
                >
                  <span className="flex items-center justify-between gap-3">
                    <span className="font-bold text-foreground">{role.name}</span>
                    <Badge variant={role.isActive ? "success" : "danger"}>{role.isActive ? "Active" : "Inactive"}</Badge>
                  </span>
                  <span className="mt-1 block text-xs font-semibold text-muted-foreground">
                    {role.userCount} {role.userCount === 1 ? "user" : "users"}
                  </span>
                </button>
              ))
            ) : (
              <EmptyState title="No roles" description="Create a role before assigning users or form access." />
            )}
          </CardContent>
        </Card>

        <div className="grid gap-4">
          {!selectedRole || !selectedRolePermissions ? (
            <Card>
              <CardContent>
                <EmptyState title="Select a role" description="Create or select a role to edit details and permissions." />
              </CardContent>
            </Card>
          ) : (
            <>
              <Card>
                <CardHeader>
                  <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
                    <div>
                      <CardTitle>{selectedRole.name}</CardTitle>
                      <CardDescription>{selectedRole.description || "No description provided."}</CardDescription>
                    </div>
                    <div className="flex flex-wrap gap-2">
                      <Button onClick={() => openEditRole(selectedRole)} size="sm" variant="outline">
                        Edit role
                      </Button>
                      <Button disabled={savingPermissions} onClick={() => void handleSavePermissions()} size="sm">
                        <Save className="size-4" />
                        Save access
                      </Button>
                    </div>
                  </div>
                </CardHeader>
                <CardContent>
                  <div className="grid gap-3 text-sm sm:grid-cols-3">
                    <RoleFact label="Users" value={selectedRole.userCount.toString()} />
                    <RoleFact label="Status" value={selectedRole.isActive ? "Active" : "Inactive"} />
                    <RoleFact label="Updated" value={formatDate(selectedRole.updatedAt ?? selectedRole.createdAt)} />
                  </div>
                </CardContent>
              </Card>

              <Card>
                <CardHeader>
                  <CardTitle>Menu access</CardTitle>
                  <CardDescription>Controls navigation and platform-level actions for this role.</CardDescription>
                </CardHeader>
                <CardContent>
                  <div className="grid gap-6">
                    <PermissionGroup
                      icon={<UserCog className="size-5" />}
                      title="Menu visibility"
                      description="Choose which app areas appear in the navigation."
                      permissions={menuPermissionOptions}
                      selected={selectedRolePermissions.permissions}
                      onToggle={toggleGlobalPermission}
                    />
                    <PermissionGroup
                      icon={<ShieldCheck className="size-5" />}
                      title="Platform actions"
                      description="Allow role holders to manage users, roles, and form administration."
                      permissions={platformPermissionOptions}
                      selected={selectedRolePermissions.permissions}
                      onToggle={toggleGlobalPermission}
                    />
                  </div>
                </CardContent>
              </Card>

              <Card>
                <CardHeader>
                  <CardTitle>Form access</CardTitle>
                  <CardDescription>Grant this role access to individual forms by action.</CardDescription>
                </CardHeader>
                <CardContent>
                  {forms.length > 0 ? (
                    <div className="overflow-x-auto rounded-lg border border-border">
                      <table className="min-w-full divide-y divide-border text-left text-sm">
                        <thead className="bg-muted/70">
                          <tr>
                            <th className="px-4 py-3 font-bold text-muted-foreground">Form</th>
                            {formAccessActions.map((action) => (
                              <th className="px-4 py-3 font-bold text-muted-foreground" key={action}>
                                {actionLabels[action]}
                              </th>
                            ))}
                          </tr>
                        </thead>
                        <tbody className="divide-y divide-border bg-card/70">
                          {forms.map((form) => (
                            <tr key={form.id}>
                              <td className="px-4 py-3">
                                <p className="font-bold text-foreground">{form.name}</p>
                                <p className="mt-1 text-xs font-semibold uppercase text-muted-foreground">{form.status}</p>
                              </td>
                              {formAccessActions.map((action) => (
                                <td className="px-4 py-3" key={action}>
                                  <input
                                    aria-label={`${actionLabels[action]} ${form.name}`}
                                    checked={hasFormPermission(selectedRolePermissions.formPermissions, form.id, action)}
                                    className="size-4"
                                    type="checkbox"
                                    onChange={() => toggleFormPermission(form.id, action)}
                                  />
                                </td>
                              ))}
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  ) : (
                    <EmptyState title="No forms available" description="Create forms before assigning per-form permissions." />
                  )}
                </CardContent>
              </Card>
            </>
          )}
        </div>
      </div>
    );
  }
}

function UserForm({
  errors = {},
  formId,
  mode,
  roles,
  userDraft,
  onClearError,
  onChange,
  onSubmit,
  onToggleRole
}: {
  errors?: UserDraftValidationErrors;
  formId: string;
  mode: "create" | "edit";
  roles: RoleDto[];
  userDraft: UserDraft;
  onClearError: (field: keyof UserDraftValidationErrors) => void;
  onChange: (value: UserDraft) => void;
  onSubmit: (event: FormEvent<HTMLFormElement>) => void;
  onToggleRole: (roleId: string) => void;
}) {
  return (
    <form className="grid gap-4" id={formId} noValidate onSubmit={onSubmit}>
      <Input
        error={errors.name}
        label="Full name"
        onChange={(event) => {
          onChange({ ...userDraft, name: event.target.value });
          onClearError("name");
        }}
        required
        value={userDraft.name}
      />
      <Input
        disabled={mode === "edit"}
        error={errors.email}
        label="Email"
        onChange={(event) => {
          onChange({ ...userDraft, email: event.target.value });
          onClearError("email");
        }}
        required
        type="email"
        value={userDraft.email}
      />
      {mode === "create" ? (
        <Input
          autoComplete="new-password"
          error={errors.password}
          helperText="At least 8 characters."
          label="Initial password"
          minLength={minimumPasswordLength}
          onChange={(event) => {
            onChange({ ...userDraft, password: event.target.value });
            onClearError("password");
          }}
          required
          type="password"
          value={userDraft.password}
        />
      ) : null}
      <Select
        label="Status"
        onChange={(event) => onChange({ ...userDraft, isActive: event.target.value === "active" })}
        options={[
          { label: "Active", value: "active" },
          { label: "Inactive", value: "inactive" }
        ]}
        value={userDraft.isActive ? "active" : "inactive"}
      />
      <div>
        <p className="mb-2 text-sm font-bold text-foreground">Roles</p>
        <div className="grid gap-2 sm:grid-cols-2">
          {roles.map((role) => (
            <Checkbox
              checked={userDraft.roleIds.includes(role.id)}
              key={role.id}
              label={role.name}
              onChange={() => onToggleRole(role.id)}
            />
          ))}
        </div>
      </div>
    </form>
  );
}

function RoleForm({
  errors = {},
  formId,
  roleDraft,
  onClearError,
  onChange,
  onSubmit
}: {
  errors?: RoleDraftValidationErrors;
  formId: string;
  roleDraft: RoleDraft;
  onClearError: (field: keyof RoleDraftValidationErrors) => void;
  onChange: (value: RoleDraft) => void;
  onSubmit: (event: FormEvent<HTMLFormElement>) => void;
}) {
  return (
    <form className="grid gap-4" id={formId} noValidate onSubmit={onSubmit}>
      <Input
        error={errors.name}
        label="Role name"
        onChange={(event) => {
          onChange({ ...roleDraft, name: event.target.value });
          onClearError("name");
        }}
        required
        value={roleDraft.name}
      />
      <Textarea
        label="Description"
        onChange={(event) => onChange({ ...roleDraft, description: event.target.value })}
        value={roleDraft.description}
      />
      <Select
        label="Status"
        onChange={(event) => onChange({ ...roleDraft, isActive: event.target.value === "active" })}
        options={[
          { label: "Active", value: "active" },
          { label: "Inactive", value: "inactive" }
        ]}
        value={roleDraft.isActive ? "active" : "inactive"}
      />
    </form>
  );
}

function RoleFact({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-lg border border-border bg-card/70 px-3 py-2">
      <p className="text-xs font-bold uppercase tracking-normal text-muted-foreground">{label}</p>
      <p className="mt-1 font-bold text-foreground">{value}</p>
    </div>
  );
}

function PermissionGroup({
  description,
  icon,
  onToggle,
  permissions,
  selected,
  title
}: {
  description: string;
  icon: ReactNode;
  onToggle: (permission: string) => void;
  permissions: ReadonlyArray<{ label: string; value: string }>;
  selected: string[];
  title: string;
}) {
  return (
    <section>
      <div className="mb-3 flex items-start gap-3">
        <span className="grid size-10 shrink-0 place-items-center rounded-xl bg-muted text-muted-foreground">{icon}</span>
        <div>
          <h2 className="font-bold text-foreground">{title}</h2>
          <p className="mt-1 text-sm text-muted-foreground">{description}</p>
        </div>
      </div>
      <div className="grid gap-2 sm:grid-cols-2 lg:grid-cols-3">
        {permissions.map((permission) => (
          <Checkbox
            checked={selected.includes(permission.value)}
            key={permission.value}
            label={permission.label}
            onChange={() => onToggle(permission.value)}
          />
        ))}
      </div>
    </section>
  );
}

function hasFormPermission(formPermissions: RoleFormPermissionDto[], formId: string, action: FormAccessAction): boolean {
  return formPermissions.some((permission) => permission.formId === formId && permission.action === action);
}

function formatDate(value: string): string {
  return new Intl.DateTimeFormat("en", {
    month: "short",
    day: "numeric",
    year: "numeric"
  }).format(new Date(value));
}

function getErrorMessage(value: unknown): string {
  if (value instanceof Error && value.message.trim().length > 0) {
    return value.message;
  }

  return "The users and access request failed.";
}

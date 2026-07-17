export type AuthRole = string;

export type AuthUser = {
  id: string;
  name: string;
  email: string;
  workspaceId: string;
  roles: AuthRole[];
  permissions: string[];
};

export type LoginCredentials = {
  email: string;
  password: string;
};

export type CompletePasswordResetRequest = {
  token: string;
  newPassword: string;
};

export type AuthSessionResponse = {
  user: AuthUser;
};

export type SsoProvider = {
  id: string;
  providerKey: string;
  displayName: string;
};

export type StartSsoRequest = {
  tenantSlug: string;
  workspaceSlug: string;
  providerKey: string;
  returnPath: string;
};

export type AuthRole = string;

export type AuthUser = {
  id: string;
  name: string;
  email: string;
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

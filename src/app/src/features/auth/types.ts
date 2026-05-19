export type AuthRole = "Admin" | "Builder" | "User" | "Viewer";

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

export type AuthSessionResponse = {
  user: AuthUser;
};

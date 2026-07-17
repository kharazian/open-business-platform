export type WorkspaceBranding = {
  appName: string;
  logoText: string;
  logoDataUrl: string | null;
  primaryColor: string;
  loginMessage: string | null;
  concurrencyStamp: string | null;
};

export type SaveWorkspaceBrandingRequest = WorkspaceBranding;

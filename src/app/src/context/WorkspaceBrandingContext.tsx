import { createContext, useContext, useEffect, useMemo, useState, type CSSProperties, type ReactNode } from "react";
import { useLocation } from "react-router-dom";
import { appBranding } from "../config/branding";
import { getCurrentBranding, getHostBranding, getPublicBranding, saveCurrentBranding, type SaveWorkspaceBrandingRequest, type WorkspaceBranding } from "../features/branding";
import { useAuth } from "./AuthContext";

const fallbackBranding: WorkspaceBranding = {
  appName: appBranding.appName,
  logoText: appBranding.logoText,
  logoDataUrl: null,
  primaryColor: "#2563eb",
  loginMessage: null,
  concurrencyStamp: null
};

type BrandingContextValue = {
  branding: WorkspaceBranding;
  loading: boolean;
  canManage: boolean;
  saveBranding: (value: SaveWorkspaceBrandingRequest) => Promise<WorkspaceBranding>;
  brandingStyle: CSSProperties;
};

const WorkspaceBrandingContext = createContext<BrandingContextValue | null>(null);

export function WorkspaceBrandingProvider({ children }: { children: ReactNode }) {
  const { status, user } = useAuth();
  const location = useLocation();
  const [branding, setBranding] = useState(fallbackBranding);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (location.pathname.startsWith("/theme")) return;
    let active = true;
    const query = new URLSearchParams(location.search);
    const tenant = query.get("tenant");
    const workspace = query.get("workspace");
    const load = status === "authenticated"
      ? getCurrentBranding()
      : tenant && workspace ? getPublicBranding(tenant, workspace) : getHostBranding();
    setLoading(true);
    load.then((value) => active && setBranding(value)).catch(() => active && setBranding(fallbackBranding)).finally(() => active && setLoading(false));
    return () => { active = false; };
  }, [location.pathname, location.search, status, user?.workspaceId]);

  useEffect(() => {
    if (location.pathname.startsWith("/theme")) return;
    document.title = branding.appName;
  }, [branding.appName, location.pathname]);

  const value = useMemo<BrandingContextValue>(() => ({
    branding,
    loading,
    canManage: Boolean(user?.permissions.includes("branding.manage")),
    saveBranding: async (request) => {
      const saved = await saveCurrentBranding(request);
      setBranding(saved);
      return saved;
    },
    brandingStyle: {
      "--app-primary": branding.primaryColor,
      "--app-primary-hover": branding.primaryColor,
      "--app-ring": branding.primaryColor,
      "--color-primary": branding.primaryColor
    } as CSSProperties
  }), [branding, loading, user?.permissions]);

  return <WorkspaceBrandingContext.Provider value={value}>{children}</WorkspaceBrandingContext.Provider>;
}

export function useWorkspaceBranding() {
  const value = useContext(WorkspaceBrandingContext);
  if (!value) throw new Error("useWorkspaceBranding must be used inside WorkspaceBrandingProvider.");
  return value;
}

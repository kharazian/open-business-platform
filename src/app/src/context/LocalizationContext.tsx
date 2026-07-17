import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { useLocation } from "react-router-dom";
import { getLocalization, saveUserLocalization, saveWorkspaceLocalization, type LocalizationSettings, type UserLocalizationPreference, type WorkspaceLocalization } from "../features/localization";
import { useAuth } from "./AuthContext";

const fallback: LocalizationSettings = { workspace: { defaultLocale: "en-CA", defaultTimeZone: "UTC", firstDayOfWeek: 1, concurrencyStamp: null }, user: { locale: null, timeZone: null, concurrencyStamp: null }, effectiveLocale: "en-CA", effectiveTimeZone: "UTC" };
const messages: Record<string, Record<string, string>> = { en: { "settings.localization": "Localization" }, fr: { "settings.localization": "Localisation" } };

type LocalizationContextValue = LocalizationSettings & {
  canManageWorkspace: boolean;
  formatDate: (value: string | number | Date) => string;
  formatDateTime: (value: string | number | Date) => string;
  formatNumber: (value: number) => string;
  t: (key: string, fallbackText?: string) => string;
  saveWorkspace: (value: WorkspaceLocalization) => Promise<LocalizationSettings>;
  saveUser: (value: UserLocalizationPreference) => Promise<LocalizationSettings>;
};

const LocalizationContext = createContext<LocalizationContextValue | null>(null);

export function LocalizationProvider({ children }: { children: ReactNode }) {
  const { status, user } = useAuth();
  const location = useLocation();
  const [settings, setSettings] = useState(fallback);

  useEffect(() => {
    if (status !== "authenticated" || location.pathname.startsWith("/theme")) return;
    let active = true;
    getLocalization().then((value) => active && setSettings(value)).catch(() => active && setSettings(fallback));
    return () => { active = false; };
  }, [location.pathname, status, user?.workspaceId]);

  const value = useMemo<LocalizationContextValue>(() => ({
    ...settings,
    canManageWorkspace: Boolean(user?.permissions.includes("localization.manage")),
    formatDate: (input) => new Intl.DateTimeFormat(settings.effectiveLocale, { dateStyle: "medium", timeZone: settings.effectiveTimeZone }).format(new Date(input)),
    formatDateTime: (input) => new Intl.DateTimeFormat(settings.effectiveLocale, { dateStyle: "medium", timeStyle: "short", timeZone: settings.effectiveTimeZone }).format(new Date(input)),
    formatNumber: (input) => new Intl.NumberFormat(settings.effectiveLocale).format(input),
    t: (key, fallbackText = key) => messages[settings.effectiveLocale.split("-")[0]]?.[key] ?? messages.en[key] ?? fallbackText,
    saveWorkspace: async (input) => { const saved = await saveWorkspaceLocalization(input); setSettings(saved); return saved; },
    saveUser: async (input) => { const saved = await saveUserLocalization(input); setSettings(saved); return saved; }
  }), [settings, user?.permissions]);

  return <LocalizationContext.Provider value={value}>{children}</LocalizationContext.Provider>;
}

export function useLocalization() {
  const value = useContext(LocalizationContext);
  if (!value) throw new Error("useLocalization must be used inside LocalizationProvider.");
  return value;
}

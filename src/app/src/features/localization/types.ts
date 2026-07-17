export type WorkspaceLocalization = { defaultLocale: string; defaultTimeZone: string; firstDayOfWeek: number; concurrencyStamp: string | null };
export type UserLocalizationPreference = { locale: string | null; timeZone: string | null; concurrencyStamp: string | null };
export type LocalizationSettings = { workspace: WorkspaceLocalization; user: UserLocalizationPreference; effectiveLocale: string; effectiveTimeZone: string };

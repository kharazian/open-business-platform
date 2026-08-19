import type { DashboardAdapterRegistration, DashboardAdapterWidget } from "./types";

const adapters = new Map<string, DashboardAdapterRegistration>();

export function registerDashboardAdapter(registration: DashboardAdapterRegistration) {
  if (!registration.id.trim()) throw new Error("Dashboard adapter id is required.");
  adapters.set(registration.id, registration);
  return () => adapters.delete(registration.id);
}

export function getDashboardAdapter(id: string) {
  return adapters.get(id);
}

export function listDashboardAdapters() {
  return [...adapters.values()].sort((left, right) => left.name.localeCompare(right.name));
}

export function createDashboardAdapterWidget(registration: DashboardAdapterRegistration, visualizationId = registration.visualizations[0]?.id): DashboardAdapterWidget | null {
  const visualization = registration.visualizations.find((item) => item.id === visualizationId);
  if (!visualization) return null;
  const settings: DashboardAdapterWidget["settings"] = {};

  for (const field of visualization.settings) {
    if (field.type === "select" && field.required && field.options?.[0]) settings[field.key] = field.options[0].value;
    if (field.type === "boolean" && field.required) settings[field.key] = false;
  }

  return { adapterId: registration.id, visualizationId: visualization.id, settings };
}

export function isDashboardAdapterWidgetConfigured(registration: DashboardAdapterRegistration | undefined, widget: DashboardAdapterWidget | null): boolean {
  if (!registration || !widget || widget.adapterId !== registration.id) return false;
  const visualization = registration.visualizations.find((item) => item.id === widget.visualizationId);
  if (!visualization) return false;

  return visualization.settings.filter((field) => field.required).every((field) => {
    const value = widget.settings[field.key];
    if (typeof value === "string") return value.trim().length > 0;
    if (typeof value === "number") return Number.isFinite(value);
    return typeof value === "boolean";
  });
}

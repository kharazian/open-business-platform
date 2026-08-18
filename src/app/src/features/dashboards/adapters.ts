import type { DashboardAdapterRegistration } from "./types";

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

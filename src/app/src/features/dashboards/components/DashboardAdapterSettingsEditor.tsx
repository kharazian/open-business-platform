import { Checkbox } from "../../../components/ui/Checkbox";
import { Input } from "../../../components/ui/Input";
import { Select } from "../../../components/ui/Select";
import type { DashboardAdapterRegistration, DashboardAdapterWidget } from "../types";

export function DashboardAdapterSettingsEditor({ adapter, onChange, value }: { adapter: DashboardAdapterRegistration; onChange: (value: DashboardAdapterWidget) => void; value: DashboardAdapterWidget }) {
  const visualization = adapter.visualizations.find((item) => item.id === value.visualizationId) ?? adapter.visualizations[0];
  if (!visualization) return <p className="text-sm font-semibold text-muted-foreground">This adapter has no visualizations.</p>;
  const update = (key: string, setting: string | number | boolean) => onChange({ ...value, settings: { ...value.settings, [key]: setting } });
  return <div className="grid gap-4">
    <Select label="Visualization" onChange={(event) => onChange({ ...value, visualizationId: event.target.value, settings: {} })} options={adapter.visualizations.map((item) => ({ label: item.name, value: item.id }))} value={visualization.id} />
    {visualization.settings.filter((field) => !isUnsafeSettingKey(field.key)).map((field) => field.type === "select" ? <Select key={field.key} label={field.label} onChange={(event) => update(field.key, event.target.value)} options={field.options ?? []} value={String(value.settings[field.key] ?? "")} /> : field.type === "boolean" ? <Checkbox checked={Boolean(value.settings[field.key])} key={field.key} label={field.label} onChange={(event) => update(field.key, event.target.checked)} /> : <Input key={field.key} label={field.label} onChange={(event) => update(field.key, field.type === "number" ? Number(event.target.value) : event.target.value)} required={field.required} type={field.type === "number" ? "number" : "text"} value={String(value.settings[field.key] ?? "")} />)}
  </div>;
}

function isUnsafeSettingKey(key: string) {
  return /(secret|password|credential|connection|token|path|file)/i.test(key);
}

import { Checkbox } from "../../../components/ui/Checkbox";
import { Input } from "../../../components/ui/Input";
import type { DashboardKpiTarget } from "../types";

export function DashboardKpiTargetEditor({ onChange, value }: { onChange: (value: DashboardKpiTarget) => void; value: DashboardKpiTarget }) {
  return <div className="grid gap-3 rounded-xl border border-border bg-card/60 p-3">
    <Checkbox checked={value.enabled} description="Compare the live KPI value with a saved display target." label="Show KPI target and variance" onChange={(event) => onChange({ ...value, enabled: event.target.checked })} />
    {value.enabled ? <div className="grid gap-3 sm:grid-cols-2">
      <Input id="kpi-target-value" label="Target value" max={1_000_000_000_000_000} min={-1_000_000_000_000_000} onChange={(event) => onChange({ ...value, value: Number(event.target.value) || 0 })} type="number" value={value.value} />
      <div><Input id="kpi-target-label" label="Target label" maxLength={80} onChange={(event) => onChange({ ...value, label: event.target.value })} placeholder="Target" value={value.label} />{!value.label.trim() ? <p className="mt-1 text-xs font-semibold text-danger">Enter a target label.</p> : null}</div>
      <p className="text-xs text-muted-foreground sm:col-span-2">Variance uses a percentage when the target is nonzero and an absolute difference when it is zero.</p>
    </div> : null}
  </div>;
}

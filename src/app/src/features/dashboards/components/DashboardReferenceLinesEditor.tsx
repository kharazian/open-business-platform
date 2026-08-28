import { Plus, Trash2 } from "lucide-react";
import { Button } from "../../../components/ui/Button";
import { Input } from "../../../components/ui/Input";
import { Select } from "../../../components/ui/Select";
import type { DashboardReferenceLine, DashboardReferenceLineStyle, DashboardSeriesAxis, DashboardSeriesColor } from "../types";

const colors = [{ label: "Blue", value: "primary" }, { label: "Cyan", value: "info" }, { label: "Green", value: "success" }, { label: "Amber", value: "warning" }, { label: "Red", value: "danger" }, { label: "Violet", value: "violet" }];
const styles = [{ label: "Solid", value: "solid" }, { label: "Dashed", value: "dashed" }, { label: "Dotted", value: "dotted" }];
const axes = [{ label: "Left axis", value: "left" }, { label: "Right axis", value: "right" }];

export function DashboardReferenceLinesEditor({ onChange, value }: { onChange: (value: DashboardReferenceLine[]) => void; value: DashboardReferenceLine[] }) {
  function add() {
    if (value.length >= 4) return;
    onChange([...value, { id: `reference-${Date.now()}`, label: value.length === 0 ? "Target" : `Reference ${value.length + 1}`, value: 0, color: value.length === 0 ? "success" : "warning", style: "dashed", axis: "left" }]);
  }
  function update(index: number, next: Partial<DashboardReferenceLine>) { onChange(value.map((line, current) => current === index ? { ...line, ...next } : line)); }
  function remove(index: number) { onChange(value.filter((_, current) => current !== index)); }

  return <div className="grid gap-3 rounded-xl border border-border bg-muted/10 p-3">
    <div className="flex items-center justify-between gap-3"><div><p className="text-sm font-bold">Reference and target lines</p><p className="text-xs text-muted-foreground">Show up to four labeled goals, standards, or limits without changing chart data.</p></div><Button disabled={value.length >= 4} onClick={add} size="sm" variant="outline"><Plus className="size-4" />Add line</Button></div>
    {value.length === 0 ? <p className="text-sm text-muted-foreground">No reference lines. Add one to compare chart values with a target or limit.</p> : value.map((line, index) => <div className="grid gap-3 rounded-lg border border-border bg-card p-3" key={line.id}>
      <div className="flex items-center justify-between gap-2"><p className="text-xs font-bold text-muted-foreground">Line {index + 1}</p><Button aria-label={`Remove reference line ${line.label || index + 1}`} onClick={() => remove(index)} size="icon" variant="ghost"><Trash2 className="size-4" /></Button></div>
      <div className="grid gap-3 sm:grid-cols-2"><Input error={line.label.trim() ? undefined : "Enter a label."} label="Line label" maxLength={80} onChange={(event) => update(index, { label: event.target.value })} value={line.label} /><Input error={Number.isFinite(line.value) && line.value >= 0 ? undefined : "Enter zero or a positive value."} label="Value" min={0} onChange={(event) => update(index, { value: Number(event.target.value) })} type="number" value={line.value} /><Select label="Color" onChange={(event) => update(index, { color: event.target.value as DashboardSeriesColor })} options={colors} value={line.color} /><Select label="Line style" onChange={(event) => update(index, { style: event.target.value as DashboardReferenceLineStyle })} options={styles} value={line.style} /><Select label="Scale" onChange={(event) => update(index, { axis: event.target.value as DashboardSeriesAxis })} options={axes} value={line.axis} /></div>
    </div>)}
  </div>;
}

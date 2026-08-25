import { ArrowDown, ArrowUp, Plus, Trash2 } from "lucide-react";
import { Button } from "../../../components/ui/Button";
import { Checkbox } from "../../../components/ui/Checkbox";
import { Input } from "../../../components/ui/Input";
import { Select } from "../../../components/ui/Select";
import type { DashboardConditionalFormatting, DashboardConditionalOperator, DashboardConditionalRule, DashboardSeriesColor } from "../types";

const operators: Array<{ label: string; value: DashboardConditionalOperator }> = [
  { label: "Greater than or equal", value: "greater_or_equal" }, { label: "Greater than", value: "greater_than" },
  { label: "Less than or equal", value: "less_or_equal" }, { label: "Less than", value: "less_than" }, { label: "Equal", value: "equal" }
];
const accents: Array<{ label: string; value: DashboardSeriesColor }> = [
  { label: "Green", value: "success" }, { label: "Amber", value: "warning" }, { label: "Red", value: "danger" },
  { label: "Blue", value: "primary" }, { label: "Cyan", value: "info" }, { label: "Violet", value: "violet" }
];

export function DashboardConditionalFormattingEditor({ onChange, value }: { onChange: (value: DashboardConditionalFormatting) => void; value: DashboardConditionalFormatting }) {
  const setRules = (rules: DashboardConditionalRule[]) => onChange({ ...value, rules });
  const updateRule = (index: number, next: Partial<DashboardConditionalRule>) => setRules(value.rules.map((rule, current) => current === index ? { ...rule, ...next } : rule));
  const moveRule = (index: number, direction: -1 | 1) => { const target = index + direction; if (target < 0 || target >= value.rules.length) return; const rules = [...value.rules]; [rules[index], rules[target]] = [rules[target], rules[index]]; setRules(rules); };
  const enable = (enabled: boolean) => onChange({ enabled, rules: enabled && value.rules.length === 0 ? defaultRules() : value.rules });
  return <div className="grid gap-3 rounded-xl border border-border bg-card/60 p-3">
    <Checkbox checked={value.enabled} description="Apply the first matching color and readable status to the live KPI value." label="Use KPI conditional status" onChange={(event) => enable(event.target.checked)} />
    {value.enabled ? <><div className="flex items-center justify-between gap-3"><p className="text-xs text-muted-foreground">Rules run from top to bottom. The normal card accent is used when none match.</p><Button disabled={value.rules.length >= 5} onClick={() => setRules([...value.rules, { id: `rule-${Date.now()}`, operator: "greater_or_equal", value: 0, accent: "success", label: "Status" }])} size="sm" variant="outline"><Plus className="size-4" />Add rule</Button></div>
      {value.rules.length === 0 ? <p className="text-xs font-semibold text-danger">Add at least one rule.</p> : value.rules.map((rule, index) => <div className="grid gap-2 rounded-lg border border-border bg-background p-3" key={rule.id}>
        <div className="flex items-center justify-between"><p className="text-sm font-bold">Rule {index + 1}</p><div className="flex gap-1"><Button aria-label={`Move conditional rule ${index + 1} up`} disabled={index === 0} onClick={() => moveRule(index, -1)} size="icon" variant="ghost"><ArrowUp className="size-4" /></Button><Button aria-label={`Move conditional rule ${index + 1} down`} disabled={index === value.rules.length - 1} onClick={() => moveRule(index, 1)} size="icon" variant="ghost"><ArrowDown className="size-4" /></Button><Button aria-label={`Remove conditional rule ${index + 1}`} onClick={() => setRules(value.rules.filter((_, current) => current !== index))} size="icon" variant="ghost"><Trash2 className="size-4" /></Button></div></div>
        <div className="grid gap-2 sm:grid-cols-2"><Select id={`conditional-rule-${index}-operator`} label="When value is" onChange={(event) => updateRule(index, { operator: event.target.value as DashboardConditionalOperator })} options={operators} value={rule.operator} /><Input id={`conditional-rule-${index}-value`} label="Threshold" max={1_000_000_000_000_000} min={-1_000_000_000_000_000} onChange={(event) => updateRule(index, { value: Number(event.target.value) || 0 })} type="number" value={rule.value} /><Select id={`conditional-rule-${index}-accent`} label="Color" onChange={(event) => updateRule(index, { accent: event.target.value as DashboardSeriesColor })} options={accents} value={rule.accent} /><div><Input id={`conditional-rule-${index}-label`} label="Status label" maxLength={80} onChange={(event) => updateRule(index, { label: event.target.value || null })} placeholder="Required" value={rule.label ?? ""} />{!rule.label?.trim() ? <p className="mt-1 text-xs font-semibold text-danger">Enter a status label.</p> : null}</div></div>
      </div>)}</> : null}
  </div>;
}

function defaultRules(): DashboardConditionalRule[] {
  return [
    { id: "target", operator: "greater_or_equal", value: 100, accent: "success", label: "On target" },
    { id: "near-target", operator: "greater_or_equal", value: 90, accent: "warning", label: "Watch" },
    { id: "below-target", operator: "less_than", value: 90, accent: "danger", label: "Below target" }
  ];
}

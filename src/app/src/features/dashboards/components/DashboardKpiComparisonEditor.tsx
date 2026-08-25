import { Checkbox } from "../../../components/ui/Checkbox";
import { Select } from "../../../components/ui/Select";
import type { ReportableField } from "../../forms/reportableFields";
import type { DashboardKpiComparison, DashboardKpiComparisonPeriod } from "../types";

const periods: Array<{ label: string; value: DashboardKpiComparisonPeriod }> = [
  { label: "Last 7 days", value: "last_7_days" },
  { label: "Last 30 days", value: "last_30_days" },
  { label: "Last 90 days", value: "last_90_days" }
];

export function DashboardKpiComparisonEditor({ dateFields, onChange, value }: { dateFields: ReportableField[]; onChange: (value: DashboardKpiComparison) => void; value: DashboardKpiComparison }) {
  const enable = (enabled: boolean) => onChange({ ...value, enabled, dateFieldId: value.dateFieldId || dateFields[0]?.id || "" });
  return <div className="grid gap-3 rounded-xl border border-border bg-card/60 p-3">
    <Checkbox checked={value.enabled} description="Compare this KPI with the immediately preceding equal UTC date window." label="Compare with previous period" onChange={(event) => enable(event.target.checked)} />
    {value.enabled ? <div className="grid gap-3 sm:grid-cols-2">
      <Select id="kpi-comparison-date-field" label="Date field" onChange={(event) => onChange({ ...value, dateFieldId: event.target.value })} value={value.dateFieldId}>{dateFields.map((field) => <option key={field.id} value={field.id}>{field.label}</option>)}</Select>
      <Select id="kpi-comparison-period" label="Current period" onChange={(event) => onChange({ ...value, period: event.target.value as DashboardKpiComparisonPeriod })} options={periods} value={value.period} />
      {dateFields.length === 0 ? <p className="text-xs font-semibold text-danger sm:col-span-2">This source has no reportable date field.</p> : <p className="text-xs text-muted-foreground sm:col-span-2">The comparison window replaces other filters on this date field; all other fixed and dashboard filters still apply.</p>}
    </div> : null}
  </div>;
}

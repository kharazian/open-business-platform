import { Checkbox } from "../../../components/ui/Checkbox";
import { Input } from "../../../components/ui/Input";
import type { DashboardChartAxes, DashboardSeriesAxis } from "../types";

export function DashboardAxisSettingsEditor({ activeAxes, onChange, percentageMode, value }: { activeAxes: DashboardSeriesAxis[]; onChange: (value: DashboardChartAxes) => void; percentageMode: boolean; value: DashboardChartAxes }) {
  function update(axis: DashboardSeriesAxis, next: Partial<DashboardChartAxes[DashboardSeriesAxis]>) {
    onChange({ ...value, [axis]: { ...value[axis], ...next } });
  }

  return <div className="grid gap-4 rounded-xl border border-border bg-muted/10 p-3">
    <div><p className="text-sm font-bold">Axis titles and scale</p><p className="text-xs text-muted-foreground">Bar charts keep a zero baseline. A manual maximum can focus the scale but may clip larger values.</p></div>
    <div className={`grid gap-4 ${activeAxes.length > 1 ? "sm:grid-cols-2" : ""}`}>
      {activeAxes.map((axis) => {
        const label = axis === "left" ? "Left" : "Right";
        const maximum = value[axis].maximum;
        return <div className="grid gap-3 rounded-lg border border-border bg-card p-3" key={axis}>
          <p className="text-xs font-bold uppercase tracking-wide text-muted-foreground">{label} axis</p>
          <Input label={`${label} axis title (optional)`} maxLength={80} onChange={(event) => update(axis, { title: event.target.value })} value={value[axis].title} />
          <Checkbox checked={maximum !== null} description={percentageMode ? "100% stacked charts always use a 100% maximum." : "Otherwise the maximum follows visible values and reference lines."} disabled={percentageMode} label="Set manual maximum" onChange={(event) => update(axis, { maximum: event.target.checked ? 100 : null })} />
          {maximum !== null ? <Input error={maximum <= 0 || maximum > 1_000_000_000_000_000 ? "Enter a value greater than zero within the supported range." : undefined} label={`${label} axis maximum`} max={1_000_000_000_000_000} min={0.000001} onChange={(event) => update(axis, { maximum: Number(event.target.value) })} step="any" type="number" value={maximum} /> : null}
        </div>;
      })}
    </div>
  </div>;
}

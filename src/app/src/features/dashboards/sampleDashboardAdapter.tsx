import { useState } from "react";
import { Activity, ArrowRight, CheckCircle2, CircleAlert, Database, Target } from "lucide-react";
import { Badge } from "../../components/ui/Badge";
import { Button } from "../../components/ui/Button";
import { Modal } from "../../components/ui/Modal";
import { registerDashboardAdapter } from "./adapters";
import type { DashboardAdapterRegistration, DashboardAdapterRendererProps } from "./types";

const commonSourceField = { key: "sourceLabel", label: "Source label", type: "text" as const };

export const sampleDashboardAdapter: DashboardAdapterRegistration = {
  id: "sample-dashboard",
  name: "Sample dashboard visualizations",
  visualizations: [
    { id: "kpi_delta", name: "KPI with delta", settings: [numberField("actual", "Actual"), numberField("comparison", "Comparison"), textField("unit", "Unit"), commonSourceField] },
    { id: "target_attainment", name: "Target attainment", settings: [numberField("actual", "Actual"), numberField("target", "Target"), textField("unit", "Unit"), commonSourceField] },
    { id: "stacked_bar", name: "Stacked bar", settings: [textField("labels", "Labels"), textField("primary", "Primary series"), textField("secondary", "Secondary series"), commonSourceField] },
    { id: "combo", name: "Bar and line comparison", settings: [textField("labels", "Labels"), textField("primary", "Primary series"), textField("secondary", "Comparison series"), commonSourceField] },
    { id: "target_line", name: "Trend with target line", settings: [textField("labels", "Labels"), textField("primary", "Actual series"), textField("secondary", "Target series"), commonSourceField] },
    { id: "donut", name: "Donut", settings: [textField("labels", "Labels"), textField("values", "Values"), commonSourceField] },
    { id: "heatmap", name: "Heatmap", settings: [textField("rows", "Rows"), textField("columns", "Columns"), textField("values", "Values"), commonSourceField] },
    { id: "waterfall", name: "Waterfall", settings: [textField("labels", "Labels"), textField("values", "Values"), commonSourceField] },
    { id: "detail_popup", name: "Detail popup", settings: [textField("title", "Popup title"), textField("period", "Period"), numberField("rows", "Rows"), commonSourceField] },
    { id: "data_health", name: "Data health", settings: [numberField("businessRows", "Business rows"), numberField("operationsRows", "Operations rows"), numberField("incidentRows", "Incident rows"), numberField("issues", "Issues"), commonSourceField] },
    { id: "status_panel", name: "Status panel", settings: [textField("title", "Title"), textField("detail", "Detail"), numberField("count", "Count"), commonSourceField] }
  ],
  render: SampleDashboardVisualization
};

registerDashboardAdapter(sampleDashboardAdapter);

function SampleDashboardVisualization({ widget }: DashboardAdapterRendererProps) {
  const adapter = widget.adapter;
  if (!adapter) return null;
  const settings = adapter.settings;
  const source = text(settings.sourceLabel, "Bounded sample adapter");
  let content;

  switch (adapter.visualizationId) {
    case "kpi_delta": content = <KpiDelta actual={number(settings.actual)} comparison={number(settings.comparison)} unit={text(settings.unit)} />; break;
    case "target_attainment": content = <TargetAttainment actual={number(settings.actual)} target={number(settings.target, 1)} unit={text(settings.unit)} />; break;
    case "stacked_bar": content = <StackedBars labels={list(settings.labels)} series={[numbers(settings.primary), numbers(settings.secondary), numbers(settings.tertiary)]} />; break;
    case "combo": content = <ComparisonChart labels={list(settings.labels)} primary={numbers(settings.primary)} secondary={numbers(settings.secondary)} target={false} />; break;
    case "target_line": content = <ComparisonChart labels={list(settings.labels)} primary={numbers(settings.primary)} secondary={numbers(settings.secondary)} target />; break;
    case "donut": content = <Donut labels={list(settings.labels)} values={numbers(settings.values)} />; break;
    case "heatmap": content = <Heatmap columns={list(settings.columns, 8)} rows={list(settings.rows, 8)} values={numbers(settings.values, 64)} />; break;
    case "waterfall": content = <Waterfall labels={list(settings.labels)} values={numbers(settings.values)} unit={text(settings.unit)} />; break;
    case "detail_popup": content = <DetailPopup period={text(settings.period, "Selected period")} rows={Math.max(0, Math.round(number(settings.rows)))} title={text(settings.title, "Detail rows")} />; break;
    case "data_health": content = <DataHealth settings={settings} />; break;
    case "status_panel": content = <StatusPanel count={number(settings.count)} detail={text(settings.detail)} status={text(settings.status, "success")} title={text(settings.title, "Status")} />; break;
    default: content = <p className="text-sm text-muted-foreground">This bounded visualization is unavailable.</p>;
  }

  return <div className="grid min-w-0 gap-4" aria-label={`${widget.title} visualization`}><div className="min-w-0">{content}</div><p className="flex min-w-0 items-center gap-1 break-words text-xs font-semibold text-muted-foreground"><Database className="size-3.5 shrink-0" />{source}</p></div>;
}

function KpiDelta({ actual, comparison, unit }: { actual: number; comparison: number; unit: string }) {
  const delta = actual - comparison;
  const percent = comparison === 0 ? 0 : delta / Math.abs(comparison) * 100;
  return <div className="grid gap-2"><p className="text-3xl font-extrabold tabular-nums">{unit}{actual.toLocaleString()}</p><Badge tone={delta >= 0 ? "success" : "danger"}>{delta >= 0 ? "+" : ""}{percent.toFixed(1)}% versus comparison</Badge></div>;
}

function TargetAttainment({ actual, target, unit }: { actual: number; target: number; unit: string }) {
  const percentage = Math.max(0, Math.min(150, actual / target * 100));
  return <div className="grid gap-3"><div className="flex items-end justify-between gap-3"><p className="text-3xl font-extrabold tabular-nums">{actual.toLocaleString()}{unit}</p><span className="text-sm font-bold text-muted-foreground">Target {target.toLocaleString()}{unit}</span></div><div className="h-4 overflow-hidden rounded-full bg-muted"><div className="h-full rounded-full bg-primary" style={{ width: `${Math.min(100, percentage)}%` }} /></div><p className="flex items-center gap-1 text-xs font-semibold"><Target className="size-3.5" />{percentage.toFixed(1)}% attainment</p></div>;
}

function StackedBars({ labels, series }: { labels: string[]; series: number[][] }) {
  const colors = ["bg-primary", "bg-info", "bg-warning"];
  return <div className="grid gap-3" aria-label="Stacked series chart">{labels.map((label, index) => { const values = series.map((item) => Math.max(0, item[index] ?? 0)); const total = Math.max(values.reduce((sum, value) => sum + value, 0), 1); return <div className="grid gap-1" key={label}><div className="flex justify-between text-xs font-bold"><span>{label}</span><span>{total.toLocaleString()}</span></div><div className="flex h-5 overflow-hidden rounded-md bg-muted">{values.map((value, seriesIndex) => <div aria-label={`Series ${seriesIndex + 1}: ${value}`} className={colors[seriesIndex]} key={seriesIndex} style={{ width: `${value / total * 100}%` }} />)}</div></div>; })}</div>;
}

function ComparisonChart({ labels, primary, secondary, target }: { labels: string[]; primary: number[]; secondary: number[]; target: boolean }) {
  const max = Math.max(1, ...primary, ...secondary);
  return <div className="grid grid-cols-6 items-end gap-3 border-b border-border pt-6" aria-label={target ? "Actual with target line" : "Actual and comparison chart"}>{labels.slice(0, 6).map((label, index) => <div className="grid gap-1 text-center" key={label}><div className="relative flex h-36 items-end justify-center gap-1"><div aria-label={`Actual ${primary[index] ?? 0}`} className="w-4 rounded-t bg-primary" style={{ height: `${(primary[index] ?? 0) / max * 100}%` }} /><div aria-label={`${target ? "Target" : "Comparison"} ${secondary[index] ?? 0}`} className={`w-4 rounded-t ${target ? "bg-warning/70" : "bg-info/70"}`} style={{ height: `${(secondary[index] ?? 0) / max * 100}%` }} /></div><span className="truncate text-[11px] font-bold text-muted-foreground">{label}</span></div>)}</div>;
}

function Donut({ labels, values }: { labels: string[]; values: number[] }) {
  const colors = ["#2563eb", "#14b8a6", "#f59e0b", "#8b5cf6", "#ef4444"];
  const total = Math.max(1, values.reduce((sum, value) => sum + Math.max(0, value), 0));
  let cursor = 0;
  const stops = values.map((value, index) => { const start = cursor; cursor += Math.max(0, value) / total * 100; return `${colors[index % colors.length]} ${start}% ${cursor}%`; }).join(", ");
  return <div className="grid items-center gap-5 sm:grid-cols-[9rem_1fr]"><div aria-label={`Total ${total}`} className="mx-auto grid size-36 place-items-center rounded-full" style={{ background: `radial-gradient(circle, var(--color-card) 0 44%, transparent 46%), conic-gradient(${stops})` }}><span className="text-2xl font-extrabold">{total}</span></div><ul className="grid gap-2 text-xs">{labels.map((label, index) => <li className="flex justify-between gap-3" key={label}><span>{label}</span><strong>{values[index] ?? 0}</strong></li>)}</ul></div>;
}

function Heatmap({ columns, rows, values }: { columns: string[]; rows: string[]; values: number[] }) {
  const max = Math.max(1, ...values);
  return <div className="min-w-0 max-w-full overflow-x-auto"><div className="grid min-w-[28rem] gap-1" style={{ gridTemplateColumns: `7rem repeat(${columns.length}, minmax(3rem, 1fr))` }}><span />{columns.map((column) => <strong className="truncate p-1 text-center text-[11px]" key={column}>{column}</strong>)}{rows.flatMap((row, rowIndex) => [<strong className="truncate p-2 text-xs" key={`${row}-label`}>{row}</strong>, ...columns.map((column, columnIndex) => { const value = values[rowIndex * columns.length + columnIndex] ?? 0; return <span aria-label={`${row}, ${column}: ${value}`} className="rounded p-2 text-center text-xs font-bold" key={`${row}-${column}`} style={{ backgroundColor: `color-mix(in srgb, var(--color-primary) ${20 + value / max * 70}%, transparent)` }}>{value}</span>; })])}</div></div>;
}

function Waterfall({ labels, values, unit }: { labels: string[]; values: number[]; unit: string }) {
  const max = Math.max(1, ...values.map(Math.abs));
  return <div className="grid grid-cols-5 items-center gap-2">{labels.slice(0, 5).map((label, index) => { const value = values[index] ?? 0; return <div className="grid gap-2 text-center" key={label}><div className="flex h-32 items-center"><div className={`w-full rounded ${value >= 0 ? "bg-success" : "bg-danger"}`} style={{ height: `${Math.max(12, Math.abs(value) / max * 100)}%` }} /></div><strong className="text-xs">{value > 0 ? "+" : ""}{value}{unit}</strong><span className="truncate text-[11px] text-muted-foreground">{label}</span></div>; })}</div>;
}

function DetailPopup({ period, rows, title }: { period: string; rows: number; title: string }) {
  const [open, setOpen] = useState(false);
  return <><div className="flex flex-wrap items-center justify-between gap-3 rounded-lg border border-border bg-muted/20 p-4"><div><p className="font-bold">{period}</p><p className="text-xs text-muted-foreground">{rows} permission-safe illustrative rows</p></div><Button onClick={() => setOpen(true)} size="sm">View detail <ArrowRight className="size-4" /></Button></div><Modal description={`${period} · ${rows} rows`} onClose={() => setOpen(false)} open={open} title={title}><div className="max-h-72 overflow-auto rounded-lg border border-border"><table className="w-full text-left text-sm"><thead className="sticky top-0 bg-card"><tr><th className="p-3">Group</th><th className="p-3">Metric</th><th className="p-3">Value</th></tr></thead><tbody>{Array.from({ length: Math.min(rows, 12) }, (_, index) => <tr className="border-t border-border" key={index}><td className="p-3">Group {index % 3 + 1}</td><td className="p-3">Metric {index + 1}</td><td className="p-3 tabular-nums">{(index + 1) * 12}</td></tr>)}</tbody></table></div></Modal></>;
}

function DataHealth({ settings }: { settings: Record<string, string | number | boolean | null> }) {
  const items = [["Business", number(settings.businessRows)], ["Operations", number(settings.operationsRows)], ["HSE", number(settings.incidentRows)]] as const;
  const issues = number(settings.issues);
  return <div className="grid gap-3 sm:grid-cols-3">{items.map(([label, value]) => <div className="rounded-lg border border-border p-3" key={label}><p className="text-xs font-bold text-muted-foreground">{label} rows</p><p className="mt-1 text-2xl font-extrabold tabular-nums">{value}</p></div>)}<p className="flex items-center gap-2 text-sm font-bold sm:col-span-3">{issues === 0 ? <CheckCircle2 className="size-4 text-success" /> : <CircleAlert className="size-4 text-warning" />}{issues === 0 ? "No source issues detected" : `${issues} source issues need review`}</p></div>;
}

function StatusPanel({ count, detail, status, title }: { count: number; detail: string; status: string; title: string }) {
  return <div className="flex items-start gap-3 rounded-lg border border-border bg-muted/20 p-4"><Activity className={`mt-0.5 size-5 ${status === "success" ? "text-success" : "text-warning"}`} /><div><p className="font-bold">{title}</p><p className="mt-1 text-sm text-muted-foreground">{detail}</p><Badge tone={status === "success" ? "success" : "warning"}>{count} tracked</Badge></div></div>;
}

function list(value: unknown, limit = 12) { return text(value).split("|").map((item) => item.trim()).filter(Boolean).slice(0, limit); }
function numbers(value: unknown, limit = 12) { return list(value, limit).map((item) => bounded(Number(item))); }
function number(value: unknown, fallback = 0) { const parsed = typeof value === "number" ? value : Number(value); return Number.isFinite(parsed) ? bounded(parsed) : fallback; }
function bounded(value: number) { return Math.max(-1_000_000_000, Math.min(1_000_000_000, value)); }
function text(value: unknown, fallback = "") { return typeof value === "string" && value.trim() ? value.trim().slice(0, 500) : fallback; }
function textField(key: string, label: string) { return { key, label, type: "text" as const }; }
function numberField(key: string, label: string) { return { key, label, type: "number" as const }; }

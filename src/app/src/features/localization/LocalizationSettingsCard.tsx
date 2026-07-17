import { Save } from "lucide-react";
import { useEffect, useState } from "react";
import { Badge } from "../../components/ui/Badge";
import { Button } from "../../components/ui/Button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "../../components/ui/Card";
import { Select } from "../../components/ui/Select";
import { useLocalization } from "../../context/LocalizationContext";

const locales = [
  { value: "en-CA", label: "English (Canada)" }, { value: "en-US", label: "English (United States)" },
  { value: "fr-CA", label: "Français (Canada)" }, { value: "fr-FR", label: "Français (France)" },
  { value: "es-ES", label: "Español (España)" }, { value: "de-DE", label: "Deutsch (Deutschland)" }
];
const timeZones = ["UTC", "America/Toronto", "America/Vancouver", "America/New_York", "Europe/London", "Europe/Paris", "Asia/Dubai"].map((value) => ({ value, label: value.replaceAll("_", " ") }));
const weekDays = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"].map((label, value) => ({ value: String(value), label }));
const includeCurrent = (options: { value: string; label: string }[], current: string) => options.some((option) => option.value === current) ? options : [{ value: current, label: current }, ...options];

export function LocalizationSettingsCard() {
  const localization = useLocalization();
  const [workspace, setWorkspace] = useState(localization.workspace);
  const [user, setUser] = useState(localization.user);
  const [message, setMessage] = useState("");
  const [saving, setSaving] = useState<"workspace" | "user" | null>(null);
  useEffect(() => { setWorkspace(localization.workspace); setUser(localization.user); }, [localization.workspace, localization.user]);

  const saveWorkspace = async () => {
    setSaving("workspace"); setMessage("");
    try { await localization.saveWorkspace(workspace); setMessage("Workspace localization defaults saved."); }
    catch (error) { setMessage(error instanceof Error ? error.message : "Workspace localization could not be saved."); }
    finally { setSaving(null); }
  };
  const saveUser = async () => {
    setSaving("user"); setMessage("");
    try { await localization.saveUser(user); setMessage("Your localization preferences were saved."); }
    catch (error) { setMessage(error instanceof Error ? error.message : "Localization preferences could not be saved."); }
    finally { setSaving(null); }
  };

  return (
    <Card>
      <CardHeader>
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div><CardTitle>{localization.t("settings.localization", "Localization")}</CardTitle><CardDescription>Workspace defaults and personal formatting overrides.</CardDescription></div>
          <Badge tone={localization.canManageWorkspace ? "success" : "default"}>{localization.effectiveLocale} · {localization.effectiveTimeZone}</Badge>
        </div>
      </CardHeader>
      <CardContent className="grid gap-6">
        <section>
          <h3 className="font-bold text-foreground">Workspace defaults</h3>
          <div className="mt-3 grid gap-4 md:grid-cols-3">
            <Select disabled={!localization.canManageWorkspace} label="Default locale" onChange={(event) => setWorkspace({ ...workspace, defaultLocale: event.target.value })} options={includeCurrent(locales, workspace.defaultLocale)} value={workspace.defaultLocale} />
            <Select disabled={!localization.canManageWorkspace} label="Default timezone" onChange={(event) => setWorkspace({ ...workspace, defaultTimeZone: event.target.value })} options={includeCurrent(timeZones, workspace.defaultTimeZone)} value={workspace.defaultTimeZone} />
            <Select disabled={!localization.canManageWorkspace} label="First day of week" onChange={(event) => setWorkspace({ ...workspace, firstDayOfWeek: Number(event.target.value) })} options={weekDays} value={String(workspace.firstDayOfWeek)} />
          </div>
          <div className="mt-4 flex justify-end"><Button disabled={!localization.canManageWorkspace || saving !== null} onClick={() => void saveWorkspace()}><Save className="size-4" />{saving === "workspace" ? "Saving..." : "Save workspace defaults"}</Button></div>
        </section>
        <section className="border-t border-border pt-5">
          <h3 className="font-bold text-foreground">My preferences</h3>
          <div className="mt-3 grid gap-4 md:grid-cols-2">
            <Select label="My locale" onChange={(event) => setUser({ ...user, locale: event.target.value || null })} options={[{ value: "", label: `Workspace default (${workspace.defaultLocale})` }, ...includeCurrent(locales, user.locale ?? workspace.defaultLocale)]} value={user.locale ?? ""} />
            <Select label="My timezone" onChange={(event) => setUser({ ...user, timeZone: event.target.value || null })} options={[{ value: "", label: `Workspace default (${workspace.defaultTimeZone})` }, ...includeCurrent(timeZones, user.timeZone ?? workspace.defaultTimeZone)]} value={user.timeZone ?? ""} />
          </div>
          <div className="mt-4 rounded-xl border border-border bg-muted/35 p-4 text-sm text-muted-foreground">Preview: <strong className="text-foreground">{localization.formatDateTime(Date.now())}</strong> · {localization.formatNumber(1234567.89)}</div>
          <div className="mt-4 flex justify-end"><Button disabled={saving !== null} onClick={() => void saveUser()}><Save className="size-4" />{saving === "user" ? "Saving..." : "Save my preferences"}</Button></div>
        </section>
        {message ? <p className="text-sm font-semibold text-foreground">{message}</p> : null}
      </CardContent>
    </Card>
  );
}

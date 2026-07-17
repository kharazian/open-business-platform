import { Copy, Globe2, Plus, RefreshCw } from "lucide-react";
import { useEffect, useState } from "react";
import { Badge } from "../../components/ui/Badge";
import { Button } from "../../components/ui/Button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "../../components/ui/Card";
import { Input } from "../../components/ui/Input";
import { useAuth } from "../../context/AuthContext";
import { createCustomDomain, listCustomDomains, mutateCustomDomain, type CustomDomain } from ".";

export function CustomDomainsSettingsCard() {
  const { user } = useAuth(); const canManage = Boolean(user?.permissions.includes("domains.manage"));
  const [items, setItems] = useState<CustomDomain[]>([]); const [hostname, setHostname] = useState(""); const [message, setMessage] = useState(""); const [busy, setBusy] = useState<string | null>(null);
  const load = async () => { if (!canManage) return; try { setItems(await listCustomDomains()); } catch (error) { setMessage(error instanceof Error ? error.message : "Custom domains could not be loaded."); } };
  useEffect(() => { void load(); }, [canManage]);
  const create = async () => { setBusy("create"); setMessage(""); try { const item = await createCustomDomain(hostname); setItems((current) => [...current, item]); setHostname(""); setMessage("Domain registered. Publish the displayed TXT record, then check verification."); } catch (error) { setMessage(error instanceof Error ? error.message : "Domain could not be registered."); } finally { setBusy(null); } };
  const mutate = async (item: CustomDomain, action: "check" | "enable" | "disable" | "rotate") => { setBusy(item.id); setMessage(""); try { const saved = await mutateCustomDomain(item, action); setItems((current) => current.map((entry) => entry.id === saved.id ? saved : entry)); setMessage(action === "check" ? (saved.status === "verified" ? "DNS ownership verified." : saved.lastFailure ?? "DNS proof was not found.") : "Domain updated."); } catch (error) { setMessage(error instanceof Error ? error.message : "Domain could not be updated."); } finally { setBusy(null); } };

  return <Card><CardHeader><div className="flex flex-wrap items-start justify-between gap-3"><div><CardTitle>Custom domains</CardTitle><CardDescription>Prove DNS ownership before routing a hostname to this workspace.</CardDescription></div><Badge tone={canManage ? "success" : "default"}>{canManage ? "Can manage" : "Read only"}</Badge></div></CardHeader><CardContent>
    {canManage ? <div className="flex flex-col gap-3 sm:flex-row sm:items-end"><Input className="flex-1" label="Hostname" onChange={(event) => setHostname(event.target.value)} placeholder="app.example.com" value={hostname} /><Button disabled={!hostname.trim() || busy !== null} onClick={() => void create()}><Plus className="size-4" />Register domain</Button></div> : null}
    <div className="mt-5 grid gap-4">{items.map((item) => <article className="rounded-xl border border-border p-4" key={item.id}><div className="flex flex-wrap items-center gap-2"><Globe2 className="size-4 text-primary" /><strong className="text-foreground">{item.hostname}</strong><Badge tone={item.status === "verified" ? "success" : "warning"}>{item.status}</Badge>{item.isEnabled ? <Badge tone="success">Enabled</Badge> : null}</div><div className="mt-3 grid gap-2 rounded-lg bg-muted/40 p-3 text-xs"><code className="break-all">TXT {item.verificationRecordName}</code><code className="break-all">{item.verificationRecordValue}</code><Button className="justify-self-start" onClick={() => void navigator.clipboard.writeText(item.verificationRecordValue)} size="sm" variant="outline"><Copy className="size-3" />Copy value</Button></div>{item.lastFailure ? <p className="mt-3 text-sm text-destructive">{item.lastFailure}</p> : null}<div className="mt-4 flex flex-wrap gap-2"><Button disabled={busy !== null} onClick={() => void mutate(item, "check")} size="sm" variant="outline"><RefreshCw className="size-3" />Check DNS</Button>{item.status === "verified" ? <Button disabled={busy !== null} onClick={() => void mutate(item, item.isEnabled ? "disable" : "enable")} size="sm">{item.isEnabled ? "Disable" : "Enable"}</Button> : null}<Button disabled={busy !== null} onClick={() => void mutate(item, "rotate")} size="sm" variant="outline">Rotate challenge</Button></div></article>)}</div>
    {canManage && items.length === 0 ? <p className="mt-5 text-sm text-muted-foreground">No custom domains registered.</p> : null}{message ? <p className="mt-4 text-sm font-semibold text-foreground">{message}</p> : null}
  </CardContent></Card>;
}

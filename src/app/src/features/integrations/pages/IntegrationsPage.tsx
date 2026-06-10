import { useEffect, useMemo, useState } from "react";
import { KeyRound, RefreshCw, RotateCcw, ShieldOff } from "lucide-react";
import { Alert } from "../../../components/ui/Alert";
import { Badge } from "../../../components/ui/Badge";
import { Button } from "../../../components/ui/Button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "../../../components/ui/Card";
import { Checkbox } from "../../../components/ui/Checkbox";
import { EmptyState } from "../../../components/ui/EmptyState";
import { Input } from "../../../components/ui/Input";
import { PageHeader } from "../../../components/ui/PageHeader";
import { Select } from "../../../components/ui/Select";
import { Table } from "../../../components/ui/Table";
import { Tabs } from "../../../components/ui/Tabs";
import { Textarea } from "../../../components/ui/Textarea";
import {
  createIntegrationApiKey,
  listIntegrationApiKeys,
  listIntegrationLogs,
  requestIntegrationLogRetry,
  revokeIntegrationApiKey,
  rotateIntegrationApiKey
} from "../api";
import { filterIntegrationLogs, formatIntegrationDate, formatMetadata, isIntegrationLogRetryEligible } from "../operations";
import { integrationApiKeyScopes, type IntegrationApiKeyDto, type IntegrationApiKeyScope, type IntegrationLogDto, type IntegrationLogFilters } from "../types";

type TabKey = "keys" | "logs";

const emptyKeyForm = {
  name: "",
  integrationKey: "",
  scopes: ["integrations.authenticate"] as IntegrationApiKeyScope[],
  isActive: true
};

export function IntegrationsPage() {
  const [activeTab, setActiveTab] = useState<TabKey>("keys");
  const [apiKeys, setApiKeys] = useState<IntegrationApiKeyDto[]>([]);
  const [logs, setLogs] = useState<IntegrationLogDto[]>([]);
  const [keyForm, setKeyForm] = useState(emptyKeyForm);
  const [filters, setFilters] = useState<IntegrationLogFilters>({});
  const [secret, setSecret] = useState<string | null>(null);
  const [selectedLog, setSelectedLog] = useState<IntegrationLogDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [busyId, setBusyId] = useState<string | null>(null);
  const [savingKey, setSavingKey] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  useEffect(() => {
    void load();
  }, []);

  const filteredLogs = useMemo(() => filterIntegrationLogs(logs, filters), [logs, filters]);

  async function load() {
    setLoading(true);
    setError(null);

    try {
      const [keyItems, logItems] = await Promise.all([listIntegrationApiKeys(), listIntegrationLogs()]);
      setApiKeys(keyItems);
      setLogs(logItems);
      setSelectedLog((current) => current ? logItems.find((log) => log.id === current.id) ?? null : logItems[0] ?? null);
    } catch (caught) {
      setError(getErrorMessage(caught));
    } finally {
      setLoading(false);
    }
  }

  async function handleCreateKey() {
    setSavingKey(true);
    setError(null);
    setNotice(null);

    try {
      const created = await createIntegrationApiKey({
        name: keyForm.name,
        integrationKey: keyForm.integrationKey,
        scopes: keyForm.scopes,
        isActive: keyForm.isActive
      });
      setApiKeys((current) => [created.apiKey, ...current]);
      setSecret(created.rawKey);
      setKeyForm(emptyKeyForm);
      setNotice("API key created. Save the raw key now; it will not be shown again.");
    } catch (caught) {
      setError(getErrorMessage(caught));
    } finally {
      setSavingKey(false);
    }
  }

  async function handleRevokeKey(apiKey: IntegrationApiKeyDto) {
    setBusyId(apiKey.id);
    setError(null);
    setNotice(null);

    try {
      const revoked = await revokeIntegrationApiKey(apiKey.id, { reason: "Revoked from integrations operations UI.", concurrencyStamp: apiKey.concurrencyStamp });
      setApiKeys((current) => current.map((item) => item.id === revoked.id ? revoked : item));
      setNotice("API key revoked.");
    } catch (caught) {
      setError(getErrorMessage(caught));
    } finally {
      setBusyId(null);
    }
  }

  async function handleRotateKey(apiKey: IntegrationApiKeyDto) {
    setBusyId(apiKey.id);
    setError(null);
    setNotice(null);

    try {
      const rotated = await rotateIntegrationApiKey(apiKey.id, { concurrencyStamp: apiKey.concurrencyStamp });
      setApiKeys((current) => current.map((item) => item.id === rotated.apiKey.id ? rotated.apiKey : item));
      setSecret(rotated.rawKey);
      setNotice("API key rotated. Save the new raw key now; it will not be shown again.");
    } catch (caught) {
      setError(getErrorMessage(caught));
    } finally {
      setBusyId(null);
    }
  }

  async function handleRetryLog(log: IntegrationLogDto) {
    setBusyId(log.id);
    setError(null);
    setNotice(null);

    try {
      const retried = await requestIntegrationLogRetry(log.id, { reason: "Retry requested from integrations operations UI." });
      setLogs((current) => current.map((item) => item.id === retried.id ? retried : item));
      setSelectedLog(retried);
      setNotice("Retry requested and audit metadata recorded.");
    } catch (caught) {
      setError(getErrorMessage(caught));
    } finally {
      setBusyId(null);
    }
  }

  function toggleScope(scope: IntegrationApiKeyScope, checked: boolean) {
    setKeyForm((current) => ({
      ...current,
      scopes: checked
        ? [...current.scopes, scope].filter((value, index, all) => all.indexOf(value) === index)
        : current.scopes.filter((value) => value !== scope)
    }));
  }

  return (
    <section className="space-y-6">
      <PageHeader
        eyebrow="Integrations"
        title="Integration operations"
        description="Manage integration credentials and inspect sanitized integration activity."
        actions={<Button onClick={() => void load()} variant="outline"><RefreshCw className="size-4" />Refresh</Button>}
      />

      {error ? <Alert title="Integration operation failed">{error}</Alert> : null}
      {notice ? <Alert title="Saved">{notice}</Alert> : null}
      {secret ? (
        <Alert title="Raw key returned once">
          <code className="break-all text-xs font-bold">{secret}</code>
        </Alert>
      ) : null}

      <Tabs
        active={activeTab}
        onChange={(value) => setActiveTab(value as TabKey)}
        tabs={[
          { label: "API keys", value: "keys", content: renderApiKeys() },
          { label: "Logs", value: "logs", content: renderLogs() }
        ]}
      />
    </section>
  );

  function renderApiKeys() {
    return (
      <div className="grid gap-4 xl:grid-cols-[minmax(260px,360px)_1fr]">
        <Card>
          <CardHeader>
            <CardTitle>Create API key</CardTitle>
            <CardDescription>Raw key material is shown once.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <Input label="Name" value={keyForm.name} onChange={(event) => setKeyForm((current) => ({ ...current, name: event.target.value }))} />
            <Input label="Integration key" value={keyForm.integrationKey} onChange={(event) => setKeyForm((current) => ({ ...current, integrationKey: event.target.value }))} />
            <div className="space-y-2">
              <p className="text-sm font-bold text-foreground">Scopes</p>
              {integrationApiKeyScopes.map((scope) => (
                <Checkbox
                  checked={keyForm.scopes.includes(scope)}
                  key={scope}
                  label={scope}
                  onChange={(event) => toggleScope(scope, event.target.checked)}
                />
              ))}
            </div>
            <Checkbox checked={keyForm.isActive} label="Active" onChange={(event) => setKeyForm((current) => ({ ...current, isActive: event.target.checked }))} />
            <Button disabled={savingKey || !keyForm.name.trim() || !keyForm.integrationKey.trim() || keyForm.scopes.length === 0} onClick={() => void handleCreateKey()}>
              <KeyRound className="size-4" />Create key
            </Button>
          </CardContent>
        </Card>

        <div className="space-y-3">
          {loading ? <p className="text-sm font-semibold text-muted-foreground">Loading API keys...</p> : null}
          {!loading && apiKeys.length === 0 ? <EmptyState title="No API keys" description="Create a key to authorize an external integration." /> : null}
          {apiKeys.length > 0 ? (
            <Table
              data={apiKeys}
              columns={[
                { header: "Name", render: (key) => <span className="font-bold">{key.name}</span> },
                { header: "Integration", accessor: "integrationKey" },
                { header: "Prefix", accessor: "keyPrefix" },
                { header: "Scopes", render: (key) => <span className="text-xs">{key.scopes.join(", ")}</span> },
                { header: "Status", render: (key) => key.revokedAt ? <Badge variant="danger">Revoked</Badge> : key.isActive ? <Badge variant="success">Active</Badge> : <Badge>Inactive</Badge> },
                { header: "Last used", render: (key) => formatIntegrationDate(key.lastUsedAt) },
                {
                  header: "Actions",
                  render: (key) => (
                    <div className="flex flex-wrap gap-2">
                      <Button disabled={busyId === key.id || Boolean(key.revokedAt)} onClick={() => void handleRotateKey(key)} size="sm" variant="outline"><RotateCcw className="size-4" />Rotate</Button>
                      <Button disabled={busyId === key.id || Boolean(key.revokedAt)} onClick={() => void handleRevokeKey(key)} size="sm" variant="danger"><ShieldOff className="size-4" />Revoke</Button>
                    </div>
                  )
                }
              ]}
            />
          ) : null}
        </div>
      </div>
    );
  }

  function renderLogs() {
    return (
      <div className="space-y-4">
        <div className="grid gap-3 md:grid-cols-5">
          <Select label="Direction" value={filters.direction ?? ""} onChange={(event) => setFilters((current) => ({ ...current, direction: event.target.value as IntegrationLogFilters["direction"] }))}>
            <option value="">All</option>
            <option value="inbound">Inbound</option>
            <option value="outbound">Outbound</option>
          </Select>
          <Select label="Type" value={filters.type ?? ""} onChange={(event) => setFilters((current) => ({ ...current, type: event.target.value as IntegrationLogFilters["type"] }))}>
            <option value="">All</option>
            <option value="api">API</option>
            <option value="webhook">Webhook</option>
            <option value="import">Import</option>
            <option value="export">Export</option>
          </Select>
          <Select label="Status" value={filters.status ?? ""} onChange={(event) => setFilters((current) => ({ ...current, status: event.target.value as IntegrationLogFilters["status"] }))}>
            <option value="">All</option>
            <option value="pending">Pending</option>
            <option value="running">Running</option>
            <option value="succeeded">Succeeded</option>
            <option value="failed">Failed</option>
            <option value="canceled">Canceled</option>
          </Select>
          <Input label="Source" value={filters.source ?? ""} onChange={(event) => setFilters((current) => ({ ...current, source: event.target.value }))} />
          <Input label="Since" type="datetime-local" value={filters.since ?? ""} onChange={(event) => setFilters((current) => ({ ...current, since: event.target.value }))} />
        </div>

        <div className="grid gap-4 xl:grid-cols-[1fr_360px]">
          <div>
            {!loading && filteredLogs.length === 0 ? <EmptyState title="No integration logs" description="Adjust filters or wait for integration activity." /> : null}
            {filteredLogs.length > 0 ? (
              <Table
                data={filteredLogs}
                columns={[
                  { header: "Time", render: (log) => formatIntegrationDate(log.startedAt) },
                  { header: "Direction", render: (log) => <Badge>{log.direction}</Badge> },
                  { header: "Type", accessor: "integrationType" },
                  { header: "Source", render: (log) => <button className="text-left font-bold text-primary hover:underline" type="button" onClick={() => setSelectedLog(log)}>{log.sourceType}</button> },
                  { header: "Status", render: (log) => renderStatus(log.status) },
                  { header: "Attempts", render: (log) => `${log.attemptCount}/${log.maxAttempts}` },
                  {
                    header: "Retry",
                    render: (log) => (
                      <Button disabled={busyId === log.id || !isIntegrationLogRetryEligible(log)} onClick={() => void handleRetryLog(log)} size="sm" variant="outline">
                        <RotateCcw className="size-4" />Retry
                      </Button>
                    )
                  }
                ]}
              />
            ) : null}
          </div>
          <Card>
            <CardHeader>
              <CardTitle>Log detail</CardTitle>
              <CardDescription>{selectedLog ? selectedLog.id : "Select a log row"}</CardDescription>
            </CardHeader>
            <CardContent className="space-y-3">
              {selectedLog ? (
                <>
                  <div className="grid grid-cols-2 gap-2 text-sm">
                    <span className="text-muted-foreground">Integration</span><span className="font-bold">{selectedLog.integrationKey}</span>
                    <span className="text-muted-foreground">Source</span><span>{selectedLog.sourceType}</span>
                    <span className="text-muted-foreground">Error</span><span>{selectedLog.errorMessage ?? "-"}</span>
                  </div>
                  <Textarea label="Request metadata" readOnly rows={7} value={formatMetadata(selectedLog.requestMetadata)} />
                  <Textarea label="Response metadata" readOnly rows={7} value={formatMetadata(selectedLog.responseMetadata)} />
                </>
              ) : (
                <p className="text-sm text-muted-foreground">No log selected.</p>
              )}
            </CardContent>
          </Card>
        </div>
      </div>
    );
  }
}

function renderStatus(status: IntegrationLogDto["status"]) {
  if (status === "succeeded") return <Badge variant="success">Succeeded</Badge>;
  if (status === "failed") return <Badge variant="danger">Failed</Badge>;
  if (status === "running") return <Badge variant="warning">Running</Badge>;
  return <Badge>{status}</Badge>;
}

function getErrorMessage(error: unknown): string {
  return error instanceof Error ? error.message : "Integration operation failed.";
}

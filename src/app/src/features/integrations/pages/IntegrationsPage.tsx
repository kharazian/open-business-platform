import { useEffect, useMemo, useState } from "react";
import { Copy, Download, KeyRound, RefreshCw, RotateCcw, ShieldOff, Upload, Webhook } from "lucide-react";
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
  createExternalExportJob,
  createIncomingWebhookListener,
  createIntegrationApiKey,
  createRecordImportJob,
  listExternalExportJobs,
  listIncomingWebhookListeners,
  listIntegrationApiKeys,
  listIntegrationLogs,
  listRecordImportJobs,
  requestIntegrationLogRetry,
  revokeIntegrationApiKey,
  rotateIncomingWebhookListenerSecret,
  rotateIntegrationApiKey,
  updateIncomingWebhookListener
} from "../api";
import { filterIntegrationLogs, formatIntegrationDate, formatMetadata, isIntegrationLogRetryEligible } from "../operations";
import {
  integrationApiKeyScopes,
  type ExternalExportJobFormat,
  type ExternalExportJobSourceType,
  type ExternalExportJobSummaryDto,
  type IncomingWebhookListenerAction,
  type IncomingWebhookListenerAuthMode,
  type IncomingWebhookListenerDto,
  type IntegrationApiKeyDto,
  type IntegrationApiKeyScope,
  type IntegrationLogDto,
  type IntegrationLogFilters,
  type RecordImportJobSummaryDto
} from "../types";

type TabKey = "keys" | "webhooks" | "imports" | "exports" | "logs";

const emptyKeyForm = {
  name: "",
  integrationKey: "",
  scopes: ["integrations.authenticate"] as IntegrationApiKeyScope[],
  isActive: true
};

const defaultWebhookMapping = JSON.stringify({
  fieldMappings: [
    { sourcePath: "email", targetFieldId: "email", required: true }
  ]
}, null, 2);

const defaultImportMapping = JSON.stringify({
  fieldMappings: [
    { csvHeader: "email", targetFieldId: "email" }
  ]
}, null, 2);

const emptyWebhookForm = {
  name: "",
  listenerKey: "",
  targetFormId: "",
  action: "create" as IncomingWebhookListenerAction,
  authMode: "listener_secret" as IncomingWebhookListenerAuthMode,
  safeLookupFieldId: "",
  mappingJson: defaultWebhookMapping,
  isActive: true
};

const emptyImportForm = {
  formId: "",
  integrationKey: "",
  fileName: "records.csv",
  csvContent: "email\njane@example.test",
  mappingJson: defaultImportMapping
};

const emptyExportForm = {
  sourceType: "form_records" as ExternalExportJobSourceType,
  format: "json" as ExternalExportJobFormat,
  integrationKey: "",
  formId: "",
  reportId: "",
  search: ""
};

export function IntegrationsPage() {
  const [activeTab, setActiveTab] = useState<TabKey>("keys");
  const [apiKeys, setApiKeys] = useState<IntegrationApiKeyDto[]>([]);
  const [logs, setLogs] = useState<IntegrationLogDto[]>([]);
  const [webhookListeners, setWebhookListeners] = useState<IncomingWebhookListenerDto[]>([]);
  const [importJobs, setImportJobs] = useState<RecordImportJobSummaryDto[]>([]);
  const [exportJobs, setExportJobs] = useState<ExternalExportJobSummaryDto[]>([]);
  const [keyForm, setKeyForm] = useState(emptyKeyForm);
  const [webhookForm, setWebhookForm] = useState(emptyWebhookForm);
  const [importForm, setImportForm] = useState(emptyImportForm);
  const [exportForm, setExportForm] = useState(emptyExportForm);
  const [filters, setFilters] = useState<IntegrationLogFilters>({});
  const [secret, setSecret] = useState<string | null>(null);
  const [secretLabel, setSecretLabel] = useState("Raw secret returned once");
  const [selectedLog, setSelectedLog] = useState<IntegrationLogDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [busyId, setBusyId] = useState<string | null>(null);
  const [savingKey, setSavingKey] = useState(false);
  const [savingWebhook, setSavingWebhook] = useState(false);
  const [savingImport, setSavingImport] = useState(false);
  const [savingExport, setSavingExport] = useState(false);
  const [lastExportContent, setLastExportContent] = useState<string | null>(null);
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
      const [keyItems, logItems, listenerItems, importItems, exportItems] = await Promise.all([
        listIntegrationApiKeys(),
        listIntegrationLogs(),
        listIncomingWebhookListeners(),
        listRecordImportJobs(),
        listExternalExportJobs()
      ]);
      setApiKeys(keyItems);
      setLogs(logItems);
      setWebhookListeners(listenerItems);
      setImportJobs(importItems);
      setExportJobs(exportItems);
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
      setSecretLabel("Raw API key returned once");
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
      setSecretLabel("Raw API key returned once");
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

  async function handleCreateWebhookListener() {
    setSavingWebhook(true);
    setError(null);
    setNotice(null);

    try {
      const mapping = parseJsonObject(webhookForm.mappingJson, "Webhook mapping");
      const created = await createIncomingWebhookListener({
        name: webhookForm.name,
        listenerKey: webhookForm.listenerKey,
        targetFormId: webhookForm.targetFormId,
        action: webhookForm.action,
        authMode: webhookForm.authMode,
        mapping: {
          fieldMappings: Array.isArray(mapping.fieldMappings) ? mapping.fieldMappings : []
        },
        isActive: webhookForm.isActive,
        safeLookupFieldId: webhookForm.safeLookupFieldId.trim() || null
      });
      setWebhookListeners((current) => [created.listener, ...current]);
      setSecret(created.rawSecret);
      setSecretLabel("Raw webhook secret returned once");
      setWebhookForm(emptyWebhookForm);
      setNotice("Webhook listener created. Save the raw listener secret now if listener-secret auth is used.");
    } catch (caught) {
      setError(getErrorMessage(caught));
    } finally {
      setSavingWebhook(false);
    }
  }

  async function handleToggleWebhookListener(listener: IncomingWebhookListenerDto) {
    setBusyId(listener.id);
    setError(null);
    setNotice(null);

    try {
      const updated = await createOrUpdateWebhookListener(listener, !listener.isActive);
      setWebhookListeners((current) => current.map((item) => item.id === updated.id ? updated : item));
      setNotice(updated.isActive ? "Webhook listener enabled." : "Webhook listener disabled.");
    } catch (caught) {
      setError(getErrorMessage(caught));
    } finally {
      setBusyId(null);
    }
  }

  async function handleRotateWebhookSecret(listener: IncomingWebhookListenerDto) {
    setBusyId(listener.id);
    setError(null);
    setNotice(null);

    try {
      const rotated = await rotateIncomingWebhookListenerSecret(listener.id);
      setWebhookListeners((current) => current.map((item) => item.id === rotated.listener.id ? rotated.listener : item));
      setSecret(rotated.rawSecret);
      setSecretLabel("Raw webhook secret returned once");
      setNotice("Webhook listener secret rotated. Save the new raw secret now.");
    } catch (caught) {
      setError(getErrorMessage(caught));
    } finally {
      setBusyId(null);
    }
  }

  async function createOrUpdateWebhookListener(listener: IncomingWebhookListenerDto, isActive: boolean) {
    return updateIncomingWebhookListener(listener.id, {
      name: listener.name,
      listenerKey: listener.listenerKey,
      targetFormId: listener.targetFormId,
      action: listener.action,
      authMode: listener.authMode,
      mapping: listener.mapping,
      isActive,
      safeLookupFieldId: listener.safeLookupFieldId ?? null
    });
  }

  async function handleCreateImportJob() {
    setSavingImport(true);
    setError(null);
    setNotice(null);

    try {
      const mapping = parseJsonObject(importForm.mappingJson, "Import mapping");
      const created = await createRecordImportJob({
        formId: importForm.formId,
        integrationKey: importForm.integrationKey,
        fileName: importForm.fileName.trim() || null,
        csvContent: importForm.csvContent,
        mapping: {
          fieldMappings: Array.isArray(mapping.fieldMappings) ? mapping.fieldMappings : []
        }
      });
      setImportJobs((current) => [created, ...current]);
      setNotice(`Import ${created.status}: ${created.succeededRows}/${created.totalRows} rows succeeded.`);
    } catch (caught) {
      setError(getErrorMessage(caught));
    } finally {
      setSavingImport(false);
    }
  }

  async function handleCreateExportJob() {
    setSavingExport(true);
    setError(null);
    setNotice(null);
    setLastExportContent(null);

    try {
      const created = await createExternalExportJob({
        sourceType: exportForm.sourceType,
        format: exportForm.format,
        integrationKey: exportForm.integrationKey,
        formId: exportForm.formId.trim() || null,
        reportId: exportForm.reportId.trim() || null,
        search: exportForm.search.trim() || null
      });
      setExportJobs((current) => [created, ...current]);
      setLastExportContent(created.artifactContent ?? null);
      setNotice(`Export ${created.status}: ${created.rowCount} rows.`);
    } catch (caught) {
      setError(getErrorMessage(caught));
    } finally {
      setSavingExport(false);
    }
  }

  async function handleCopySecret() {
    if (!secret) return;

    try {
      await navigator.clipboard.writeText(secret);
      setNotice("Copied one-time secret to clipboard.");
    } catch {
      setError("Clipboard copy failed. Select and copy the value manually.");
    }
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
        <Alert title={secretLabel}>
          <div className="space-y-3">
            <code className="block break-all text-xs font-bold">{secret}</code>
            <div className="flex flex-wrap gap-2">
              <Button onClick={() => void handleCopySecret()} size="sm" variant="outline"><Copy className="size-4" />Copy</Button>
              <Button onClick={() => setSecret(null)} size="sm" variant="ghost">Hide</Button>
            </div>
          </div>
        </Alert>
      ) : null}

      <Tabs
        active={activeTab}
        onChange={(value) => setActiveTab(value as TabKey)}
        tabs={[
          { label: "API keys", value: "keys", content: renderApiKeys() },
          { label: "Webhooks", value: "webhooks", content: renderWebhooks() },
          { label: "Imports", value: "imports", content: renderImports() },
          { label: "Exports", value: "exports", content: renderExports() },
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

  function renderWebhooks() {
    return (
      <div className="grid gap-4 xl:grid-cols-[minmax(280px,420px)_1fr]">
        <Card>
          <CardHeader>
            <CardTitle>Create webhook listener</CardTitle>
            <CardDescription>Map inbound JSON payload paths to one target form.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <Input label="Name" value={webhookForm.name} onChange={(event) => setWebhookForm((current) => ({ ...current, name: event.target.value }))} />
            <Input label="Listener key" value={webhookForm.listenerKey} onChange={(event) => setWebhookForm((current) => ({ ...current, listenerKey: event.target.value }))} />
            <Input label="Target form ID" value={webhookForm.targetFormId} onChange={(event) => setWebhookForm((current) => ({ ...current, targetFormId: event.target.value }))} />
            <div className="grid gap-3 sm:grid-cols-2">
              <Select label="Action" value={webhookForm.action} onChange={(event) => setWebhookForm((current) => ({ ...current, action: event.target.value as IncomingWebhookListenerAction }))}>
                <option value="create">Create</option>
                <option value="upsert">Upsert</option>
              </Select>
              <Select label="Auth mode" value={webhookForm.authMode} onChange={(event) => setWebhookForm((current) => ({ ...current, authMode: event.target.value as IncomingWebhookListenerAuthMode }))}>
                <option value="listener_secret">Listener secret</option>
                <option value="api_key">API key</option>
              </Select>
            </div>
            <Input label="Safe lookup field ID" value={webhookForm.safeLookupFieldId} onChange={(event) => setWebhookForm((current) => ({ ...current, safeLookupFieldId: event.target.value }))} />
            <Textarea label="Mapping JSON" rows={7} value={webhookForm.mappingJson} onChange={(event) => setWebhookForm((current) => ({ ...current, mappingJson: event.target.value }))} />
            <Checkbox checked={webhookForm.isActive} label="Active" onChange={(event) => setWebhookForm((current) => ({ ...current, isActive: event.target.checked }))} />
            <Button disabled={savingWebhook || !webhookForm.name.trim() || !webhookForm.listenerKey.trim() || !webhookForm.targetFormId.trim()} onClick={() => void handleCreateWebhookListener()}>
              <Webhook className="size-4" />Create listener
            </Button>
          </CardContent>
        </Card>

        <div className="space-y-3">
          {!loading && webhookListeners.length === 0 ? <EmptyState title="No webhook listeners" description="Create a listener to receive inbound integration payloads." /> : null}
          {webhookListeners.length > 0 ? (
            <Table
              data={webhookListeners}
              columns={[
                { header: "Name", render: (listener) => <span className="font-bold">{listener.name}</span> },
                { header: "Key", accessor: "listenerKey" },
                { header: "Form", accessor: "targetFormId" },
                { header: "Auth", accessor: "authMode" },
                { header: "Status", render: (listener) => listener.isActive ? <Badge variant="success">Active</Badge> : <Badge>Inactive</Badge> },
                {
                  header: "Actions",
                  render: (listener) => (
                    <div className="flex flex-wrap gap-2">
                      <Button disabled={busyId === listener.id} onClick={() => void handleToggleWebhookListener(listener)} size="sm" variant="outline">
                        {listener.isActive ? "Disable" : "Enable"}
                      </Button>
                      <Button disabled={busyId === listener.id} onClick={() => void handleRotateWebhookSecret(listener)} size="sm" variant="outline">
                        <RotateCcw className="size-4" />Secret
                      </Button>
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

  function renderImports() {
    return (
      <div className="grid gap-4 xl:grid-cols-[minmax(280px,460px)_1fr]">
        <Card>
          <CardHeader>
            <CardTitle>Create CSV import</CardTitle>
            <CardDescription>Runs synchronously through existing record validation.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <Input label="Form ID" value={importForm.formId} onChange={(event) => setImportForm((current) => ({ ...current, formId: event.target.value }))} />
            <Input label="Integration key" value={importForm.integrationKey} onChange={(event) => setImportForm((current) => ({ ...current, integrationKey: event.target.value }))} />
            <Input label="File name" value={importForm.fileName} onChange={(event) => setImportForm((current) => ({ ...current, fileName: event.target.value }))} />
            <Textarea label="CSV content" rows={7} value={importForm.csvContent} onChange={(event) => setImportForm((current) => ({ ...current, csvContent: event.target.value }))} />
            <Textarea label="Mapping JSON" rows={7} value={importForm.mappingJson} onChange={(event) => setImportForm((current) => ({ ...current, mappingJson: event.target.value }))} />
            <Button disabled={savingImport || !importForm.formId.trim() || !importForm.integrationKey.trim() || !importForm.csvContent.trim()} onClick={() => void handleCreateImportJob()}>
              <Upload className="size-4" />Run import
            </Button>
          </CardContent>
        </Card>

        <div className="space-y-3">
          {!loading && importJobs.length === 0 ? <EmptyState title="No import jobs" description="Run a CSV import to create record rows through V8." /> : null}
          {importJobs.length > 0 ? (
            <Table
              data={importJobs}
              columns={[
                { header: "File", render: (job) => <span className="font-bold">{job.fileName ?? job.id}</span> },
                { header: "Integration", accessor: "integrationKey" },
                { header: "Status", render: (job) => renderJobStatus(job.status) },
                { header: "Rows", render: (job) => `${job.succeededRows}/${job.totalRows} succeeded` },
                { header: "Failed", accessor: "failedRows" },
                { header: "Completed", render: (job) => formatIntegrationDate(job.completedAt) }
              ]}
            />
          ) : null}
        </div>
      </div>
    );
  }

  function renderExports() {
    return (
      <div className="grid gap-4 xl:grid-cols-[minmax(280px,420px)_1fr]">
        <Card>
          <CardHeader>
            <CardTitle>Create export</CardTitle>
            <CardDescription>Exports permission-filtered form records or list report rows.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="grid gap-3 sm:grid-cols-2">
              <Select label="Source" value={exportForm.sourceType} onChange={(event) => setExportForm((current) => ({ ...current, sourceType: event.target.value as ExternalExportJobSourceType }))}>
                <option value="form_records">Form records</option>
                <option value="list_report">List report</option>
              </Select>
              <Select label="Format" value={exportForm.format} onChange={(event) => setExportForm((current) => ({ ...current, format: event.target.value as ExternalExportJobFormat }))}>
                <option value="json">JSON</option>
                <option value="csv">CSV</option>
              </Select>
            </div>
            <Input label="Integration key" value={exportForm.integrationKey} onChange={(event) => setExportForm((current) => ({ ...current, integrationKey: event.target.value }))} />
            <Input label="Form ID" value={exportForm.formId} onChange={(event) => setExportForm((current) => ({ ...current, formId: event.target.value }))} />
            <Input label="Report ID" value={exportForm.reportId} onChange={(event) => setExportForm((current) => ({ ...current, reportId: event.target.value }))} />
            <Input label="Search" value={exportForm.search} onChange={(event) => setExportForm((current) => ({ ...current, search: event.target.value }))} />
            <Button disabled={savingExport || !exportForm.integrationKey.trim() || !exportForm.formId.trim()} onClick={() => void handleCreateExportJob()}>
              <Download className="size-4" />Run export
            </Button>
          </CardContent>
        </Card>

        <div className="space-y-3">
          {lastExportContent ? <Textarea label="Last export artifact content" readOnly rows={8} value={lastExportContent} /> : null}
          {!loading && exportJobs.length === 0 ? <EmptyState title="No export jobs" description="Run an export to produce a protected artifact." /> : null}
          {exportJobs.length > 0 ? (
            <Table
              data={exportJobs}
              columns={[
                { header: "Artifact", render: (job) => <span className="font-bold">{job.artifactFileName ?? job.id}</span> },
                { header: "Integration", accessor: "integrationKey" },
                { header: "Source", accessor: "sourceType" },
                { header: "Format", accessor: "format" },
                { header: "Status", render: (job) => renderJobStatus(job.status) },
                { header: "Rows", accessor: "rowCount" },
                { header: "Completed", render: (job) => formatIntegrationDate(job.completedAt) }
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

function renderJobStatus(status: string) {
  if (status === "succeeded") return <Badge variant="success">Succeeded</Badge>;
  if (status === "failed" || status === "completed_with_errors") return <Badge variant="danger">{status}</Badge>;
  if (status === "running" || status === "pending") return <Badge variant="warning">{status}</Badge>;
  return <Badge>{status}</Badge>;
}

function parseJsonObject(value: string, label: string): Record<string, unknown> {
  try {
    const parsed = JSON.parse(value) as unknown;
    if (typeof parsed === "object" && parsed !== null && !Array.isArray(parsed)) {
      return parsed as Record<string, unknown>;
    }
  } catch {
    throw new Error(`${label} must be valid JSON.`);
  }

  throw new Error(`${label} must be a JSON object.`);
}

function getErrorMessage(error: unknown): string {
  return error instanceof Error ? error.message : "Integration operation failed.";
}

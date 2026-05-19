import { type FormEvent, useMemo, useState } from "react";
import { FileText, Plus, Search } from "lucide-react";
import { Badge } from "../../../components/ui/Badge";
import { Button } from "../../../components/ui/Button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "../../../components/ui/Card";
import { EmptyState } from "../../../components/ui/EmptyState";
import { Input } from "../../../components/ui/Input";
import { Modal } from "../../../components/ui/Modal";
import { PageHeader } from "../../../components/ui/PageHeader";
import { Select } from "../../../components/ui/Select";
import { Table, type TableColumn } from "../../../components/ui/Table";
import { Textarea } from "../../../components/ui/Textarea";
import {
  createFormDraftSummary,
  filterFormSummaries,
  formStatuses,
  getFormStatusLabel,
  sampleFormSummaries,
  validateCreateFormDraftInput,
  type FormStatus,
  type FormStatusFilter,
  type FormSummary
} from "../drafts";

const statusOptions: Array<{ label: string; value: FormStatusFilter }> = [
  { label: "All statuses", value: "all" },
  ...formStatuses.map((status) => ({ label: getFormStatusLabel(status), value: status }))
];

const statusBadgeVariant: Record<FormStatus, "default" | "success" | "warning"> = {
  draft: "warning",
  published: "success",
  archived: "default"
};

const formColumns: Array<TableColumn<FormSummary>> = [
  {
    header: "Form",
    render: (form) => (
      <div>
        <p className="font-bold text-foreground">{form.name}</p>
        {form.description ? <p className="mt-1 max-w-xl text-sm leading-5 text-muted-foreground">{form.description}</p> : null}
      </div>
    )
  },
  {
    header: "Status",
    render: (form) => <Badge variant={statusBadgeVariant[form.status]}>{getFormStatusLabel(form.status)}</Badge>
  },
  { header: "Fields", accessor: "fieldCount" },
  {
    header: "Current version",
    render: (form) =>
      form.currentVersionId ? (
        <span className="font-semibold">{form.currentVersionId}</span>
      ) : (
        <span className="text-muted-foreground">Draft only</span>
      )
  },
  {
    header: "Updated",
    render: (form) => formatDate(form.updatedAt)
  }
];

export function FormsListPage() {
  const [forms, setForms] = useState<FormSummary[]>(sampleFormSummaries);
  const [query, setQuery] = useState("");
  const [status, setStatus] = useState<FormStatusFilter>("all");
  const [createOpen, setCreateOpen] = useState(false);
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [nameError, setNameError] = useState<string | undefined>();

  const filteredForms = useMemo(() => filterFormSummaries(forms, { query, status }), [forms, query, status]);
  const draftCount = forms.filter((form) => form.status === "draft").length;
  const publishedCount = forms.filter((form) => form.status === "published").length;
  const archivedCount = forms.filter((form) => form.status === "archived").length;

  function openCreateModal() {
    setCreateOpen(true);
    setNameError(undefined);
  }

  function closeCreateModal() {
    setCreateOpen(false);
    setName("");
    setDescription("");
    setNameError(undefined);
  }

  function handleCreateSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const validation = validateCreateFormDraftInput({ name, description });

    if (!validation.valid) {
      setNameError(validation.error);
      return;
    }

    setForms((currentForms) => [createFormDraftSummary({ name, description }), ...currentForms]);
    closeCreateModal();
  }

  function clearFilters() {
    setQuery("");
    setStatus("all");
  }

  return (
    <div className="grid gap-6">
      <PageHeader
        eyebrow="Forms"
        title="Form drafts"
        description="Create and manage the forms that will collect records across the workspace."
        actions={
          <Button onClick={openCreateModal}>
            <Plus className="size-4" />
            Create form
          </Button>
        }
      />

      <section className="grid gap-4 md:grid-cols-3">
        <SummaryTile label="Drafts" value={draftCount} />
        <SummaryTile label="Published" value={publishedCount} />
        <SummaryTile label="Archived" value={archivedCount} />
      </section>

      <Card>
        <CardHeader>
          <CardTitle>Forms</CardTitle>
          <CardDescription>Filter draft, published, and archived form summaries.</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="mb-4 grid gap-3 md:grid-cols-[minmax(0,1fr)_14rem]">
            <Input
              aria-label="Search forms"
              icon={<Search className="size-4" />}
              onChange={(event) => setQuery(event.target.value)}
              placeholder="Search by name or description"
              value={query}
            />
            <Select
              aria-label="Filter forms by status"
              onChange={(event) => setStatus(event.target.value as FormStatusFilter)}
              options={statusOptions}
              value={status}
            />
          </div>

          {filteredForms.length > 0 ? (
            <>
              <div className="hidden md:block">
                <Table columns={formColumns} rows={filteredForms} />
              </div>
              <div className="grid gap-3 md:hidden">
                {filteredForms.map((form) => (
                  <MobileFormSummary key={form.id} form={form} />
                ))}
              </div>
            </>
          ) : (
            <EmptyState
              title="No forms found"
              description="No form summaries match the current search and status filters."
              action={
                <Button onClick={clearFilters} variant="outline">
                  Clear filters
                </Button>
              }
            />
          )}
        </CardContent>
      </Card>

      <Modal
        open={createOpen}
        onClose={closeCreateModal}
        title="Create form"
        description="Start a draft form with a name and optional description."
        footer={
          <>
            <Button onClick={closeCreateModal} variant="outline">
              Cancel
            </Button>
            <Button form="create-form" type="submit">
              Create draft
            </Button>
          </>
        }
      >
        <form className="grid gap-4" id="create-form" onSubmit={handleCreateSubmit}>
          <Input
            autoFocus
            error={nameError}
            label="Form name"
            onChange={(event) => {
              setName(event.target.value);
              if (nameError) setNameError(undefined);
            }}
            placeholder="Expense request"
            value={name}
          />
          <Textarea
            label="Description"
            onChange={(event) => setDescription(event.target.value)}
            placeholder="What this form is used for"
            value={description}
          />
        </form>
      </Modal>
    </div>
  );
}

function SummaryTile({ label, value }: { label: string; value: number }) {
  return (
    <Card className="p-5">
      <p className="text-sm font-bold text-muted-foreground">{label}</p>
      <p className="mt-3 text-3xl font-bold text-foreground">{value}</p>
    </Card>
  );
}

function MobileFormSummary({ form }: { form: FormSummary }) {
  return (
    <div className="rounded-xl border border-border bg-card/80 p-4">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <div className="flex items-center gap-2">
            <FileText className="size-4 shrink-0 text-muted-foreground" />
            <p className="truncate font-bold text-foreground">{form.name}</p>
          </div>
          {form.description ? <p className="mt-2 text-sm leading-5 text-muted-foreground">{form.description}</p> : null}
        </div>
        <Badge className="shrink-0" variant={statusBadgeVariant[form.status]}>
          {getFormStatusLabel(form.status)}
        </Badge>
      </div>
      <dl className="mt-4 grid grid-cols-2 gap-3 text-sm">
        <div>
          <dt className="font-bold text-muted-foreground">Fields</dt>
          <dd className="mt-1 text-foreground">{form.fieldCount}</dd>
        </div>
        <div>
          <dt className="font-bold text-muted-foreground">Updated</dt>
          <dd className="mt-1 text-foreground">{formatDate(form.updatedAt)}</dd>
        </div>
      </dl>
    </div>
  );
}

function formatDate(value: string): string {
  return new Intl.DateTimeFormat("en", {
    month: "short",
    day: "numeric",
    year: "numeric"
  }).format(new Date(value));
}

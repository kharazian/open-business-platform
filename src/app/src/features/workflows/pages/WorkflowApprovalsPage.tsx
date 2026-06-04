import { useEffect, useMemo, useState } from "react";
import { Check, ClipboardCheck, RefreshCw, X } from "lucide-react";
import { Link } from "react-router-dom";
import { Alert } from "../../../components/ui/Alert";
import { Badge } from "../../../components/ui/Badge";
import { Button } from "../../../components/ui/Button";
import { Card, CardContent } from "../../../components/ui/Card";
import { EmptyState } from "../../../components/ui/EmptyState";
import { PageHeader } from "../../../components/ui/PageHeader";
import { approveWorkflowApproval, listWorkflowApprovals, rejectWorkflowApproval } from "../api";
import type { WorkflowApprovalTask } from "../types";

export function WorkflowApprovalsPage() {
  const [approvals, setApprovals] = useState<WorkflowApprovalTask[]>([]);
  const [loading, setLoading] = useState(true);
  const [respondingId, setRespondingId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const pendingCount = useMemo(() => approvals.filter((approval) => approval.status === "pending").length, [approvals]);

  useEffect(() => {
    void loadApprovals();
  }, []);

  async function loadApprovals() {
    setLoading(true);
    setError(null);

    try {
      setApprovals(await listWorkflowApprovals());
    } catch (caught) {
      setError(getErrorMessage(caught));
    } finally {
      setLoading(false);
    }
  }

  async function respond(approval: WorkflowApprovalTask, action: "approve" | "reject") {
    setRespondingId(`${approval.id}:${action}`);
    setError(null);
    setNotice(null);

    try {
      const updated = action === "approve"
        ? await approveWorkflowApproval(approval.id)
        : await rejectWorkflowApproval(approval.id);

      setApprovals((current) => current.map((item) => (item.id === updated.id ? updated : item)));
      setNotice(action === "approve" ? "Approval submitted." : "Approval rejected.");
      await loadApprovals();
    } catch (caught) {
      setError(getErrorMessage(caught));
    } finally {
      setRespondingId(null);
    }
  }

  return (
    <div className="grid gap-6">
      <PageHeader
        eyebrow="Workflow"
        title="Approvals"
        actions={
          <>
            <Badge tone={pendingCount > 0 ? "info" : "default"}>{pendingCount} pending</Badge>
            <Button disabled={loading} onClick={() => void loadApprovals()} size="icon" title="Refresh approvals" variant="outline">
              <RefreshCw className="size-4" />
            </Button>
          </>
        }
      />

      {error ? <Alert title="Approval error">{error}</Alert> : null}
      {notice ? <Alert title="Approval updated">{notice}</Alert> : null}

      {loading && approvals.length === 0 ? (
        <div className="grid gap-3">
          {[0, 1, 2].map((item) => (
            <div className="h-28 animate-pulse rounded-xl border border-border bg-muted/50" key={item} />
          ))}
        </div>
      ) : approvals.length === 0 ? (
        <EmptyState
          title="No approvals"
          description="Workflow approval tasks assigned to you will appear here."
          action={
            <Button onClick={() => void loadApprovals()} variant="outline">
              <RefreshCw className="size-4" />
              Refresh
            </Button>
          }
        />
      ) : (
        <div className="grid gap-3">
          {approvals.map((approval) => (
            <ApprovalRow
              approval={approval}
              key={approval.id}
              onRespond={respond}
              respondingId={respondingId}
            />
          ))}
        </div>
      )}
    </div>
  );
}

function ApprovalRow({
  approval,
  onRespond,
  respondingId
}: {
  approval: WorkflowApprovalTask;
  onRespond: (approval: WorkflowApprovalTask, action: "approve" | "reject") => void;
  respondingId: string | null;
}) {
  const pending = approval.status === "pending";

  return (
    <Card className={pending ? "border-primary/35 bg-primary-soft/30" : undefined}>
      <CardContent className="p-4">
        <div className="flex flex-col gap-4 md:flex-row md:items-start md:justify-between">
          <div className="min-w-0 flex-1">
            <div className="flex flex-wrap items-center gap-2">
              <span className="grid size-9 shrink-0 place-items-center rounded-xl border border-border bg-card text-primary">
                <ClipboardCheck className="size-4" />
              </span>
              <h2 className="min-w-0 break-words text-base font-bold text-foreground">{approval.transitionName}</h2>
              <Badge tone={pending ? "info" : "default"}>{approval.status}</Badge>
              <Badge variant="default">{approval.mode}</Badge>
            </div>
            <p className="mt-3 text-sm leading-6 text-muted-foreground">
              {approval.approvalStepName}: {approval.fromStateKey} to {approval.toStateKey}
            </p>
            <div className="mt-4 flex flex-wrap gap-2 text-xs font-semibold text-muted-foreground">
              <span>{formatDate(approval.createdAt)}</span>
              <span aria-hidden="true">/</span>
              <Link className="text-primary hover:underline" to={`/records/${approval.recordId}`}>
                Record {shortId(approval.recordId)}
              </Link>
            </div>
          </div>

          <div className="flex shrink-0 flex-wrap gap-2 md:justify-end">
            {pending ? (
              <>
                <Button
                  disabled={respondingId !== null}
                  onClick={() => onRespond(approval, "approve")}
                  size="sm"
                >
                  <Check className="size-4" />
                  Approve
                </Button>
                <Button
                  disabled={respondingId !== null}
                  onClick={() => onRespond(approval, "reject")}
                  size="sm"
                  variant="danger"
                >
                  <X className="size-4" />
                  Reject
                </Button>
              </>
            ) : null}
          </div>
        </div>
      </CardContent>
    </Card>
  );
}

function shortId(value: string): string {
  return value.length > 8 ? value.slice(0, 8) : value;
}

function formatDate(value: string): string {
  return new Intl.DateTimeFormat("en", {
    month: "short",
    day: "numeric",
    year: "numeric",
    hour: "numeric",
    minute: "2-digit"
  }).format(new Date(value));
}

function getErrorMessage(error: unknown): string {
  return error instanceof Error ? error.message : "Workflow approval request failed.";
}

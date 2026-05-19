export const formStatuses = ["draft", "published", "archived"] as const;

export type FormStatus = (typeof formStatuses)[number];
export type FormStatusFilter = "all" | FormStatus;

export type FormSummary = {
  id: string;
  name: string;
  description?: string;
  status: FormStatus;
  fieldCount: number;
  currentVersionId?: string | null;
  createdAt: string;
  updatedAt: string | null;
};

export type CreateFormDraftInput = {
  id?: string;
  name: string;
  description?: string;
  now?: string;
};

export type CreateFormDraftValidation =
  | {
      valid: true;
      value: {
        name: string;
        description?: string;
      };
    }
  | {
      valid: false;
      error: string;
      value: {
        name: string;
        description?: string;
      };
    };

export type FormSummaryFilter = {
  query: string;
  status: FormStatusFilter;
};

export const sampleFormSummaries: FormSummary[] = [
  {
    id: "form_employee_onboarding",
    name: "Employee onboarding",
    description: "Collect new hire details before HR review.",
    status: "published",
    fieldCount: 7,
    currentVersionId: "version_employee_onboarding_1",
    createdAt: "2026-05-10T14:00:00.000Z",
    updatedAt: "2026-05-15T17:30:00.000Z"
  },
  {
    id: "form_expense_request",
    name: "Expense request",
    description: "Draft intake for employee reimbursements.",
    status: "draft",
    fieldCount: 5,
    currentVersionId: null,
    createdAt: "2026-05-12T10:15:00.000Z",
    updatedAt: "2026-05-18T19:20:00.000Z"
  },
  {
    id: "form_vendor_access",
    name: "Vendor access",
    description: "Archived access request form for legacy vendor onboarding.",
    status: "archived",
    fieldCount: 6,
    currentVersionId: "version_vendor_access_2",
    createdAt: "2026-04-20T09:00:00.000Z",
    updatedAt: "2026-05-02T13:45:00.000Z"
  }
];

export function validateCreateFormDraftInput(input: Pick<CreateFormDraftInput, "name" | "description">): CreateFormDraftValidation {
  const name = input.name.trim();
  const description = normalizeOptionalText(input.description);
  const value = description ? { name, description } : { name };

  if (!name) {
    return {
      valid: false,
      error: "Form name is required.",
      value
    };
  }

  return {
    valid: true,
    value
  };
}

export function createFormDraftSummary(input: CreateFormDraftInput): FormSummary {
  const validation = validateCreateFormDraftInput(input);

  if (!validation.valid) {
    throw new Error(validation.error);
  }

  const timestamp = input.now ?? new Date().toISOString();

  return {
    id: input.id ?? createFormId(),
    name: validation.value.name,
    description: validation.value.description,
    status: "draft",
    fieldCount: 0,
    currentVersionId: null,
    createdAt: timestamp,
    updatedAt: timestamp
  };
}

export function filterFormSummaries(forms: FormSummary[], filter: FormSummaryFilter): FormSummary[] {
  const query = filter.query.trim().toLowerCase();

  return forms.filter((form) => {
    const matchesStatus = filter.status === "all" || form.status === filter.status;

    if (!matchesStatus) {
      return false;
    }

    if (!query) {
      return true;
    }

    return `${form.name} ${form.description ?? ""}`.toLowerCase().includes(query);
  });
}

export function getFormStatusLabel(status: FormStatus): string {
  return status.charAt(0).toUpperCase() + status.slice(1);
}

function normalizeOptionalText(value: string | undefined): string | undefined {
  const normalized = value?.trim();
  return normalized ? normalized : undefined;
}

function createFormId(): string {
  if (globalThis.crypto?.randomUUID) {
    return `form_${globalThis.crypto.randomUUID()}`;
  }

  return `form_${Date.now().toString(36)}`;
}

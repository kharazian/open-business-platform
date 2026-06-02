export const minimumPasswordLength = 8;

export type UserFormMode = "create" | "edit";

export type UserDraftForValidation = {
  name: string;
  email: string;
  password: string;
  isActive: boolean;
  roleIds: string[];
  departmentIds: string[];
  groupIds: string[];
};

export type RoleDraftForValidation = {
  name: string;
  description: string;
  isActive: boolean;
};

export type UserDraftValidationErrors = {
  name?: string;
  email?: string;
  password?: string;
};

export type RoleDraftValidationErrors = {
  name?: string;
};

type ValidResult<T> = {
  valid: true;
  value: T;
};

type InvalidResult<TError> = {
  valid: false;
  errors: TError;
};

export function validateUserDraft(
  draft: UserDraftForValidation,
  mode: UserFormMode
): ValidResult<UserDraftForValidation> | InvalidResult<UserDraftValidationErrors> {
  const name = draft.name.trim();
  const email = draft.email.trim().toLowerCase();
  const errors: UserDraftValidationErrors = {};

  if (!name) {
    errors.name = "Full name is required.";
  }

  if (mode === "create") {
    if (!email) {
      errors.email = "Email is required.";
    } else if (!isValidEmail(email)) {
      errors.email = "Enter a valid email address.";
    }
  }

  if (mode === "create" && draft.password.length < minimumPasswordLength) {
    errors.password = "Password must be at least 8 characters.";
  }

  if (Object.keys(errors).length > 0) {
    return { valid: false, errors };
  }

  return {
    valid: true,
    value: {
      ...draft,
      name,
      email
    }
  };
}

export function validateRoleDraft(
  draft: RoleDraftForValidation
): ValidResult<RoleDraftForValidation> | InvalidResult<RoleDraftValidationErrors> {
  const name = draft.name.trim();
  const description = draft.description.trim();

  if (!name) {
    return { valid: false, errors: { name: "Role name is required." } };
  }

  return {
    valid: true,
    value: {
      ...draft,
      name,
      description
    }
  };
}

export function validateResetPassword(password: string): ValidResult<string> | { valid: false; error: string } {
  if (password.length < minimumPasswordLength) {
    return { valid: false, error: "Password must be at least 8 characters." };
  }

  return { valid: true, value: password };
}

function isValidEmail(value: string): boolean {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value);
}

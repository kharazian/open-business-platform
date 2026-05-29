import { useState, type FormEvent } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { Lock } from "lucide-react";
import { Alert } from "../components/ui/Alert";
import { Button } from "../components/ui/Button";
import { Card } from "../components/ui/Card";
import { Input } from "../components/ui/Input";
import { appBranding } from "../config/branding";
import { completePasswordReset } from "../features/auth";

export function ResetPassword() {
  const [searchParams] = useSearchParams();
  const token = searchParams.get("token") ?? "";
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [error, setError] = useState<string | null>(token ? null : "Reset token is missing.");
  const [completed, setCompleted] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);

    if (!token) {
      setError("Reset token is missing.");
      return;
    }

    if (newPassword !== confirmPassword) {
      setError("Passwords do not match.");
      return;
    }

    setSubmitting(true);

    try {
      await completePasswordReset({ token, newPassword });
      setCompleted(true);
    } catch (error) {
      setError(error instanceof Error ? error.message : "Password reset failed.");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <main className="grid min-h-screen place-items-center px-4 py-10">
      <Card className="w-full max-w-md p-6">
        <div className="mb-6 text-center">
          <span className="mx-auto grid size-12 place-items-center rounded-xl bg-primary text-sm font-extrabold text-primary-foreground">
            {appBranding.logoText}
          </span>
          <h1 className="mt-4 text-2xl font-bold text-foreground">Choose a new password</h1>
          <p className="mt-2 text-sm text-muted-foreground">Use at least 8 characters.</p>
        </div>

        {completed ? (
          <div className="grid gap-4">
            <Alert title="Password updated">You can now sign in with your new password.</Alert>
            <Link className="rounded-xl bg-primary px-4 py-2 text-center text-sm font-bold text-primary-foreground hover:bg-primary/90" to="/login">
              Back to sign in
            </Link>
          </div>
        ) : (
          <form className="grid gap-4" onSubmit={handleSubmit}>
            <Input
              autoComplete="new-password"
              icon={<Lock size={16} />}
              label="New password"
              onChange={(event) => setNewPassword(event.target.value)}
              placeholder="Enter a new password"
              required
              type="password"
              value={newPassword}
            />
            <Input
              autoComplete="new-password"
              error={error ?? undefined}
              icon={<Lock size={16} />}
              label="Confirm password"
              onChange={(event) => setConfirmPassword(event.target.value)}
              placeholder="Confirm the new password"
              required
              type="password"
              value={confirmPassword}
            />
            <Button type="submit" className="mt-2 w-full" disabled={submitting || !token}>
              {submitting ? "Updating..." : "Update password"}
            </Button>
            <Link className="rounded-xl px-4 py-2 text-center text-sm font-bold text-muted-foreground hover:bg-muted hover:text-foreground" to="/login">
              Back to sign in
            </Link>
          </form>
        )}
      </Card>
    </main>
  );
}

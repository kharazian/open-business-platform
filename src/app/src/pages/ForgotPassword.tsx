import { useState, type FormEvent } from "react";
import { Link } from "react-router-dom";
import { Mail } from "lucide-react";
import { Alert } from "../components/ui/Alert";
import { Button } from "../components/ui/Button";
import { Card } from "../components/ui/Card";
import { Input } from "../components/ui/Input";
import { appBranding } from "../config/branding";
import { requestPasswordReset } from "../features/auth";

const resetRequestedMessage = "If the email belongs to an active user, a password reset link will be sent.";

export function ForgotPassword() {
  const [email, setEmail] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [sent, setSent] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setSent(false);
    setSubmitting(true);

    try {
      await requestPasswordReset(email);
      setSent(true);
    } catch (error) {
      setError(error instanceof Error ? error.message : "Password reset request failed.");
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
          <h1 className="mt-4 text-2xl font-bold text-foreground">Reset password</h1>
          <p className="mt-2 text-sm text-muted-foreground">Enter your workspace email.</p>
        </div>

        <form className="grid gap-4" onSubmit={handleSubmit}>
          {sent ? <Alert title="Check your email">{resetRequestedMessage}</Alert> : null}
          <Input
            autoComplete="email"
            error={error ?? undefined}
            icon={<Mail size={16} />}
            label="Email"
            onChange={(event) => setEmail(event.target.value)}
            placeholder="you@company.test"
            required
            type="email"
            value={email}
          />
          <Button type="submit" className="mt-2 w-full" disabled={submitting}>
            {submitting ? "Sending..." : "Send reset link"}
          </Button>
          <Link className="rounded-xl px-4 py-2 text-center text-sm font-bold text-muted-foreground hover:bg-muted hover:text-foreground" to="/login">
            Back to sign in
          </Link>
        </form>
      </Card>
    </main>
  );
}

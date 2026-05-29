import { useState, type FormEvent } from "react";
import { Link, Navigate, useLocation, useNavigate } from "react-router-dom";
import { Button } from "../components/ui/Button";
import { Card } from "../components/ui/Card";
import { Input } from "../components/ui/Input";
import { appBranding } from "../config/branding";
import { useAuth } from "../context/AuthContext";

type LoginLocationState = {
  from?: {
    pathname?: string;
  };
};

export function Login() {
  const { signIn, status } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();
  const [email, setEmail] = useState("admin@company.test");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const locationState = location.state as LoginLocationState | null;
  const destination = locationState?.from?.pathname ?? "/";

  if (status === "authenticated") {
    return <Navigate replace to={destination} />;
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setSubmitting(true);

    try {
      await signIn({ email, password });
      navigate(destination, { replace: true });
    } catch (error) {
      setError(error instanceof Error ? error.message : "Sign in failed.");
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
          <h1 className="mt-4 text-2xl font-bold text-foreground">Sign in</h1>
          <p className="mt-2 text-sm text-muted-foreground">Access the {appBranding.appName} dashboard.</p>
        </div>

        <form className="grid gap-4" onSubmit={handleSubmit}>
          <Input
            autoComplete="email"
            label="Email"
            onChange={(event) => setEmail(event.target.value)}
            placeholder="admin@company.test"
            required
            type="email"
            value={email}
          />
          <Input
            autoComplete="current-password"
            error={error ?? undefined}
            label="Password"
            onChange={(event) => setPassword(event.target.value)}
            placeholder="Enter your password"
            required
            type="password"
            value={password}
          />
          <Link className="-mt-2 justify-self-end text-sm font-bold text-muted-foreground hover:text-foreground" to="/forgot-password">
            Forgot password?
          </Link>
          <Button type="submit" className="mt-2 w-full" disabled={submitting}>
            {submitting ? "Signing in..." : "Sign in"}
          </Button>
        </form>
      </Card>
    </main>
  );
}

import { useEffect, useState, type FormEvent } from "react";
import { Link, Navigate, useLocation, useNavigate } from "react-router-dom";
import { Button } from "../components/ui/Button";
import { Card } from "../components/ui/Card";
import { Input } from "../components/ui/Input";
import { useAuth } from "../context/AuthContext";
import { useWorkspaceBranding } from "../context/WorkspaceBrandingContext";
import { getSsoProviders, startSso, type SsoProvider } from "../features/auth";

type LoginLocationState = {
  from?: {
    pathname?: string;
  };
};

export function Login() {
  const { signIn, status } = useAuth();
  const { branding, brandingStyle } = useWorkspaceBranding();
  const location = useLocation();
  const navigate = useNavigate();
  const [email, setEmail] = useState("admin@company.test");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [ssoProviders, setSsoProviders] = useState<SsoProvider[]>([]);
  const [startingProvider, setStartingProvider] = useState<string | null>(null);
  const locationState = location.state as LoginLocationState | null;
  const destination = locationState?.from?.pathname ?? "/";
  const query = new URLSearchParams(location.search);
  const tenantSlug = query.get("tenant");
  const workspaceSlug = query.get("workspace");

  useEffect(() => {
    if (!tenantSlug || !workspaceSlug) {
      setSsoProviders([]);
      return;
    }
    let active = true;
    getSsoProviders(tenantSlug, workspaceSlug)
      .then((providers) => active && setSsoProviders(providers))
      .catch(() => active && setSsoProviders([]));
    return () => {
      active = false;
    };
  }, [tenantSlug, workspaceSlug]);

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

  async function handleSso(provider: SsoProvider) {
    if (!tenantSlug || !workspaceSlug) return;
    setError(null);
    setStartingProvider(provider.providerKey);
    try {
      const authorizationUrl = await startSso({ tenantSlug, workspaceSlug, providerKey: provider.providerKey, returnPath: destination });
      window.location.assign(authorizationUrl);
    } catch (error) {
      setError(error instanceof Error ? error.message : "SSO sign in failed.");
      setStartingProvider(null);
    }
  }

  return (
    <main className="grid min-h-screen place-items-center px-4 py-10" style={brandingStyle}>
      <Card className="w-full max-w-md p-6">
        <div className="mb-6 text-center">
          {branding.logoDataUrl ? (
            <img alt="" className="mx-auto size-12 rounded-xl object-contain" src={branding.logoDataUrl} />
          ) : (
            <span className="mx-auto grid size-12 place-items-center rounded-xl bg-primary text-sm font-extrabold text-primary-foreground">
              {branding.logoText}
            </span>
          )}
          <h1 className="mt-4 text-2xl font-bold text-foreground">Sign in</h1>
          <p className="mt-2 text-sm text-muted-foreground">{branding.loginMessage ?? `Access the ${branding.appName} dashboard.`}</p>
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
        {ssoProviders.length > 0 ? (
          <div className="mt-5 grid gap-3 border-t border-border pt-5">
            {ssoProviders.map((provider) => (
              <Button
                key={provider.id}
                type="button"
                variant="outline"
                disabled={startingProvider !== null}
                onClick={() => void handleSso(provider)}
              >
                {startingProvider === provider.providerKey ? "Redirecting..." : `Continue with ${provider.displayName}`}
              </Button>
            ))}
          </div>
        ) : null}
      </Card>
    </main>
  );
}

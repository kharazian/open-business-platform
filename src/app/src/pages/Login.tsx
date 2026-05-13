import { Link } from "react-router-dom";
import { Button } from "../components/ui/Button";
import { Card } from "../components/ui/Card";
import { Input } from "../components/ui/Input";
import { appBranding } from "../config/branding";

export function Login() {
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

        <form className="grid gap-4">
          <Input label="Email" placeholder="admin@company.test" type="email" />
          <Input label="Password" placeholder="Enter your password" type="password" />
          <Button type="submit" className="mt-2 w-full">
            Sign in
          </Button>
        </form>

        <p className="mt-5 text-center text-sm text-muted-foreground">
          Previewing the app?{" "}
          <Link className="font-bold text-primary hover:underline" to="/">
            Open dashboard
          </Link>
        </p>
      </Card>
    </main>
  );
}

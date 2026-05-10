import { Mail } from "lucide-react";
import { AuthCard } from "../components/AuthCard";
import { Button } from "../../components/ui/Button";
import { Input } from "../../components/ui/Input";

export function ThemeForgotPassword() {
  return (
    <AuthCard title="Reset your password" description="Send a secure password reset link to a workspace user email address.">
      <div className="grid gap-4">
        <Input label="Email" type="email" placeholder="you@company.test" icon={<Mail size={16} />} />
        <Button className="w-full justify-center">Send reset link</Button>
        <Button className="w-full justify-center" variant="ghost">Back to sign in</Button>
      </div>
    </AuthCard>
  );
}

import { ShieldCheck } from "lucide-react";
import { AuthCard } from "../components/AuthCard";
import { Button } from "../../components/ui/Button";
import { Input } from "../../components/ui/Input";

export function ThemeMfa() {
  return (
    <AuthCard title="Verify your sign in" description="A multi-factor authentication checkpoint for protected admin workspaces.">
      <div className="grid gap-4">
        <Input label="Verification code" inputMode="numeric" placeholder="123456" icon={<ShieldCheck size={16} />} />
        <Button className="w-full justify-center">Verify code</Button>
        <Button className="w-full justify-center" variant="ghost">Use another method</Button>
      </div>
    </AuthCard>
  );
}

import { Lock } from "lucide-react";
import { AuthCard } from "../components/AuthCard";
import { Button } from "../../components/ui/Button";
import { Input } from "../../components/ui/Input";

export function ThemeResetPassword() {
  return (
    <AuthCard title="Choose a new password" description="A focused reset form with password and confirmation inputs.">
      <div className="grid gap-4">
        <Input label="New password" type="password" placeholder="Enter a new password" icon={<Lock size={16} />} />
        <Input label="Confirm password" type="password" placeholder="Confirm the new password" icon={<Lock size={16} />} />
        <Button className="w-full justify-center">Update password</Button>
      </div>
    </AuthCard>
  );
}
